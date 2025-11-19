Imports System.ComponentModel
Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.Windows.Shapes

' =============================================================
' ARQUIVO: PageVoluntarios.xaml.vb
' -------------------------------------------------------------
' Essa classe representa a página que mostra informações sobre
' voluntários. Ela exibe listas, filtros e gráficos dinâmicos
' (pizza e barras) de acordo com os dados do banco.
' =============================================================
Public Class PageVoluntarios

    ' Lista principal de pessoas carregadas do banco de dados
    Private masterListPessoas As New List(Of Pessoa)

    ' Objeto usado para filtrar e exibir a lista de forma dinâmica
    Private pessoasView As ICollectionView

    ' Essa função é executada quando a página é aberta
    ' Ela prepara os dados, filtros e gráficos
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        pessoasView = CollectionViewSource.GetDefaultView(masterListPessoas)
        DataGridPessoas.ItemsSource = pessoasView

        CarregarDadosDoBanco()
        CarregarFiltros()
        AtualizarGraficos()
    End Sub

    ' Carrega todas as pessoas ativas do banco de dados
    ' e atualiza o DataGrid com as informações
    Private Sub CarregarDadosDoBanco()
        Try
            masterListPessoas = DataAccess.GetTodasPessoasAtivas()
            pessoasView = CollectionViewSource.GetDefaultView(masterListPessoas)
            DataGridPessoas.ItemsSource = pessoasView
            DataGridPessoas.Items.Refresh()
        Catch ex As Exception
            MessageBox.Show($"Erro ao carregar pessoas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' Preenche os filtros de status e cidade
    ' Permite ao usuário filtrar a lista de voluntários
    Private Sub CarregarFiltros()
        cmbFiltroStatus.ItemsSource = New List(Of String) From {"(Todos)", "Normal", "Vulnerável"}
        cmbFiltroStatus.SelectedIndex = 0

        Dim cidades = masterListPessoas.
            Select(Function(p) p.Cidade).
            Distinct().
            Where(Function(c) Not String.IsNullOrWhiteSpace(c)).
            OrderBy(Function(c) c).
            ToList()

        cidades.Insert(0, "(Todas)")
        cmbFiltroCidade.ItemsSource = cidades
        cmbFiltroCidade.SelectedIndex = 0
    End Sub

    ' Aplica os filtros escolhidos pelo usuário (status, cidade e busca)
    Private Sub BtnAplicarFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnAplicarFiltros.Click
        pessoasView.Filter = Function(item)
                                 Dim p = CType(item, Pessoa)
                                 Dim filtroStatus = CType(cmbFiltroStatus.SelectedItem, String)
                                 Dim filtroCidade = CType(cmbFiltroCidade.SelectedItem, String)
                                 Dim busca = txtBuscaPessoa.Text.Trim()

                                 ' Filtra por status "Vulnerável"
                                 If filtroStatus = "Vulnerável" AndAlso Not p.IsVulneravel Then Return False

                                 ' Filtra por status "Normal"
                                 If filtroStatus = "Normal" AndAlso p.IsVulneravel Then Return False

                                 ' Filtra por cidade específica
                                 If filtroCidade <> "(Todas)" AndAlso filtroCidade <> "" AndAlso
                                    Not String.Equals(p.Cidade, filtroCidade, StringComparison.OrdinalIgnoreCase) Then Return False

                                 ' Filtra pelo nome ou documento digitado
                                 If busca <> "" AndAlso
                                    Not p.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase) AndAlso
                                    Not p.Documento.Contains(busca, StringComparison.OrdinalIgnoreCase) Then Return False

                                 Return True
                             End Function

        AtualizarGraficos()
    End Sub

    ' Limpa todos os filtros e mostra novamente todos os voluntários
    Private Sub BtnLimparFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnLimparFiltros.Click
        cmbFiltroStatus.SelectedIndex = 0
        cmbFiltroCidade.SelectedIndex = 0
        txtBuscaPessoa.Text = ""
        pessoasView.Filter = Nothing
        AtualizarGraficos()
    End Sub

    ' Atualiza todos os gráficos (pizza e barras)
    Private Sub AtualizarGraficos()
        CanvasPizzaVulnerabilidade.Children.Clear()
        StackPessoasPorCidade.Children.Clear()
        StackSaldoAtivoPorCidade.Children.Clear()

        ' Se não houver dados, sai da função
        If masterListPessoas Is Nothing OrElse masterListPessoas.Count = 0 Then Exit Sub

        DesenharGraficoPizzaVulnerabilidade()
        DesenharGraficoBarrasPessoasPorCidade()
        DesenharGraficoBarrasSaldoAtivo()
    End Sub

    ' =============================================================
    ' GRÁFICO DE PIZZA - Mostra número de pessoas vulneráveis por cidade
    ' =============================================================
    Private Sub DesenharGraficoPizzaVulnerabilidade()
        CanvasPizzaVulnerabilidade.Children.Clear()

        ' Agrupa as pessoas vulneráveis por cidade
        Dim dados = masterListPessoas.
            Where(Function(p) p.IsVulneravel AndAlso Not String.IsNullOrWhiteSpace(p.Cidade)).
            GroupBy(Function(p) p.Cidade).
            Select(Function(g) New With {.Cidade = g.Key, .Quantidade = g.Count()}).
            OrderByDescending(Function(x) x.Quantidade).
            Take(6).ToList()

        Dim total = dados.Sum(Function(x) x.Quantidade)
        If total = 0 Then Exit Sub

        Dim cores = New List(Of String) From {"#FF6F61", "#6B5B95", "#88B04B", "#F7CAC9", "#92A8D1", "#955251"}
        Dim cx = 100, cy = 100, r = 80

        ' Texto central com o total
        Dim centroTexto = New TextBlock With {
            .Text = $"Total: {total}",
            .FontWeight = FontWeights.Bold,
            .FontSize = 14,
            .Foreground = Brushes.Black
        }
        Canvas.SetLeft(centroTexto, cx - 30)
        Canvas.SetTop(centroTexto, cy - 10)
        CanvasPizzaVulnerabilidade.Children.Add(centroTexto)

        ' Se houver apenas uma cidade, desenha um círculo único
        If dados.Count = 1 Then
            Dim circulo = New Ellipse With {
                .Width = r * 2,
                .Height = r * 2,
                .Fill = (New BrushConverter()).ConvertFromString(cores(0)),
                .Stroke = Brushes.White,
                .StrokeThickness = 1
            }
            Canvas.SetLeft(circulo, cx - r)
            Canvas.SetTop(circulo, cy - r)
            CanvasPizzaVulnerabilidade.Children.Add(circulo)
        Else
            ' Desenha uma fatia para cada cidade
            Dim startAngle As Double = 0
            For i = 0 To dados.Count - 1
                Dim fatia = dados(i)
                Dim sweep = 360.0 * fatia.Quantidade / total
                Dim path = CriarFatiaPizza(cx, cy, r, startAngle, sweep, cores(i Mod cores.Count))

                If path IsNot Nothing Then
                    ToolTipService.SetToolTip(path, $"{fatia.Cidade}: {fatia.Quantidade} pessoas ({fatia.Quantidade * 100 / total:F1}%)")
                    CanvasPizzaVulnerabilidade.Children.Add(path)
                End If

                startAngle += sweep
            Next
        End If

        ' Cria legendas com nome da cidade e porcentagem
        Dim legendY = 10
        For i = 0 To dados.Count - 1
            Dim item = dados(i)
            Dim percent = item.Quantidade * 100.0 / total
            Dim cor = (New BrushConverter()).ConvertFromString(cores(i Mod cores.Count))

            Dim bullet = New Rectangle With {.Width = 12, .Height = 12, .Fill = cor, .Margin = New Thickness(0, 0, 5, 0)}
            Dim txt = New TextBlock With {.Text = $"{item.Cidade}: {item.Quantidade} ({percent:F1}%)", .FontSize = 12, .VerticalAlignment = VerticalAlignment.Center}
            Dim stack = New StackPanel With {.Orientation = Orientation.Horizontal, .Margin = New Thickness(220, legendY, 0, 0)}

            stack.Children.Add(bullet)
            stack.Children.Add(txt)
            CanvasPizzaVulnerabilidade.Children.Add(stack)
            legendY += 20
        Next
    End Sub

    ' Cria uma fatia (parte do círculo) para o gráfico de pizza
    Private Function CriarFatiaPizza(cx As Double, cy As Double, r As Double, startAngle As Double, sweepAngle As Double, corHex As String) As Path
        Dim sRad = Math.PI * startAngle / 180
        Dim eRad = Math.PI * (startAngle + sweepAngle) / 180

        Dim x1 = cx + r * Math.Cos(sRad)
        Dim y1 = cy + r * Math.Sin(sRad)
        Dim x2 = cx + r * Math.Cos(eRad)
        Dim y2 = cy + r * Math.Sin(eRad)

        Dim largeArc = If(sweepAngle > 180, 1, 0)

        ' Garante o uso correto de ponto/virgula nas coordenadas
        Dim pathData As String = String.Format(CultureInfo.InvariantCulture,
                                               "M{0},{1} L{2},{3} A{4},{4} 0 {5},1 {6},{7} Z",
                                               cx, cy, x1, y1, r, largeArc, x2, y2)
        Try
            Dim geom = Geometry.Parse(pathData)
            Return New Path With {
                .Data = geom,
                .Fill = (New BrushConverter()).ConvertFromString(corHex),
                .Stroke = Brushes.White,
                .StrokeThickness = 1
            }
        Catch ex As Exception
            MessageBox.Show("Erro ao desenhar fatia: " & ex.Message & vbCrLf & pathData)
            Return Nothing
        End Try
    End Function

    ' =============================================================
    ' GRÁFICOS DE BARRAS - Mostram dados por cidade
    ' =============================================================
    ' Essa função desenha as barras de um gráfico genérico (usada por dois tipos diferentes)
    Private Sub DesenharGraficoBarras(StackPanelDestino As StackPanel, dados As List(Of (Cidade As String, Quantidade As Integer)), corHex As String)
        StackPanelDestino.Children.Clear()
        Dim maxVal = If(dados.Count > 0, dados.Max(Function(x) x.Quantidade), 1)

        ' Cria uma barra para cada cidade
        For Each item In dados
            Dim altura = 160 * item.Quantidade / maxVal

            Dim bar = New Rectangle With {
                .Width = 25,
                .Height = altura,
                .Fill = (New BrushConverter()).ConvertFromString(corHex),
                .RadiusX = 4,
                .RadiusY = 4,
                .VerticalAlignment = VerticalAlignment.Bottom
            }

            ToolTipService.SetToolTip(bar, $"{item.Cidade}: {item.Quantidade}")

            ' Texto com valor da barra
            Dim valorTxt = New TextBlock With {
                .Text = item.Quantidade.ToString(),
                .FontSize = 11,
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Margin = New Thickness(0, 0, 0, 3)
            }

            ' Texto com nome da cidade
            Dim cidadeTxt = New TextBlock With {
                .Text = item.Cidade,
                .FontSize = 10,
                .TextAlignment = TextAlignment.Center,
                .TextWrapping = TextWrapping.Wrap,
                .Width = 50
            }

            ' Agrupa os elementos da barra
            Dim stack = New StackPanel With {.Margin = New Thickness(5), .VerticalAlignment = VerticalAlignment.Bottom}
            stack.Children.Add(valorTxt)
            stack.Children.Add(bar)
            stack.Children.Add(cidadeTxt)

            StackPanelDestino.Children.Add(stack)
        Next
    End Sub

    ' Gráfico de barras: mostra número de pessoas por cidade
    Private Sub DesenharGraficoBarrasPessoasPorCidade()
        Dim dados = masterListPessoas.
            GroupBy(Function(p) p.Cidade).
            Select(Function(g) (g.Key, g.Count())).
            OrderByDescending(Function(x) x.Item2).
            Take(10).ToList()

        DesenharGraficoBarras(StackPessoasPorCidade, dados, "#6B5B95")
    End Sub

    ' Gráfico de barras: mostra saldo ativo (pontuação) por cidade
    Private Sub DesenharGraficoBarrasSaldoAtivo()
        Dim dados = masterListPessoas.
            Where(Function(p) p.SaldoPontos > 0).
            GroupBy(Function(p) p.Cidade).
            Select(Function(g) (g.Key, g.Count())).
            OrderByDescending(Function(x) x.Item2).
            Take(10).ToList()

        DesenharGraficoBarras(StackSaldoAtivoPorCidade, dados, "#88B04B")
    End Sub

End Class
