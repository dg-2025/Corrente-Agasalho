' Importa classes para listas observáveis e controle de alterações
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Collections.Specialized
' Importa recursos visuais, como cores (Brushes)
Imports System.Windows.Media

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PageSaidaEntrega
' -------------------------------------------------------------
' Essa página controla as entregas de roupas/itens para beneficiários.
' Aqui o operador escolhe o beneficiário, adiciona itens no carrinho
' e confirma a retirada (troca ou doação pura).
' =============================================================
Public Class PageSaidaEntrega

    ' Lista de beneficiários carregados do banco
    Private ListaBeneficiarios As New List(Of Pessoa)

    ' Lista de itens que estão disponíveis no estoque
    Private ListaEstoque As New List(Of ItemDisponivel)

    ' Lista dinâmica que representa o carrinho de retirada
    ' ObservableCollection permite atualizar automaticamente a tela
    Private Carrinho As New ObservableCollection(Of ItemCarrinho)()

    ' Guarda o beneficiário que está selecionado no momento
    Private beneficiarioSelecionado As Pessoa = Nothing

    ' Indica se o modo de alerta de frio está ativo (regra especial)
    Private isModoAlertaAtivo As Boolean = False


    ' =============================================================
    ' EVENTO PRINCIPAL - executa quando a página é carregada
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Define que o DataGrid exibirá os itens do carrinho
        dgItensSaida.ItemsSource = Carrinho

        ' Carrega dados do banco (beneficiários e estoque)
        CarregarDadosDoBanco()

        ' Preenche as caixas de seleção
        cmbBeneficiario.ItemsSource = ListaBeneficiarios
        cmbItemEstoque.ItemsSource = ListaEstoque

        ' Exibe ou oculta o alerta de frio
        VerificarModoAlerta()

        ' Adiciona um evento para atualizar os botões quando o carrinho muda
        AddHandler Carrinho.CollectionChanged, AddressOf AtualizarLogicaCheckout

        ' Força a primeira atualização do estado dos botões e totais
        AtualizarLogicaCheckout(Nothing, Nothing)
    End Sub


    ' =============================================================
    ' Função que busca dados reais no banco (beneficiários e estoque)
    ' =============================================================
    Private Sub CarregarDadosDoBanco()
        Try
            ListaBeneficiarios = DataAccess.GetTodasPessoasAtivas()
            ListaEstoque = DataAccess.GetItensDisponiveisParaSaida()
            isModoAlertaAtivo = DataAccess.GetModoAlertaStatus()
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro fatal ao carregar dados: {0}", ex.Message),
                             "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' Verifica se o "Modo de Alerta de Frio" está ativo
    ' =============================================================
    Private Sub VerificarModoAlerta()
        ' Se o alerta está ativo, mostra o cartão de aviso na tela
        If isModoAlertaAtivo Then
            CardAlertaFrioAtivo.Visibility = Visibility.Visible
        Else
            ' Se não estiver ativo, esconde o cartão
            CardAlertaFrioAtivo.Visibility = Visibility.Collapsed
        End If
    End Sub


    ' =============================================================
    ' Quando o beneficiário é trocado no comboBox
    ' =============================================================
    Private Sub CmbBeneficiario_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cmbBeneficiario.SelectionChanged
        ' Guarda o beneficiário selecionado
        beneficiarioSelecionado = CType(cmbBeneficiario.SelectedItem, Pessoa)

        If beneficiarioSelecionado IsNot Nothing Then
            ' Mostra o saldo atual de pontos
            txtSaldoPontos.Text = beneficiarioSelecionado.SaldoPontos.ToString()

            ' Exibe o status de vulnerabilidade com cores diferentes
            If beneficiarioSelecionado.IsVulneravel Then
                txtStatusVulnerabilidade.Text = "SITUAÇÃO VULNERÁVEL"
                txtStatusVulnerabilidade.Foreground = CType(Application.Current.Resources("CorAcaoPerigo"), SolidColorBrush)
            Else
                txtStatusVulnerabilidade.Text = "NORMAL"
                txtStatusVulnerabilidade.Foreground = CType(Application.Current.Resources("CorTextoPrincipal"), SolidColorBrush)
            End If

            panelStatsBeneficiario.Visibility = Visibility.Visible
        Else
            ' Se nenhum beneficiário for selecionado, esconde as informações
            panelStatsBeneficiario.Visibility = Visibility.Collapsed
        End If

        ' Atualiza a lógica de pontos e botões
        AtualizarLogicaCheckout(Nothing, Nothing)
    End Sub


    ' =============================================================
    ' Quando o item do estoque muda, reseta a quantidade para 1
    ' =============================================================
    Private Sub CmbItemEstoque_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cmbItemEstoque.SelectionChanged
        Dim itemSelecionado As ItemDisponivel = CType(cmbItemEstoque.SelectedItem, ItemDisponivel)
        If itemSelecionado IsNot Nothing Then
            txtQuantidadeRetirar.Text = "1"
        End If
    End Sub


    ' =============================================================
    ' Botão "Adicionar ao Carrinho"
    ' =============================================================
    Private Sub BtnAdicionarAoCarrinho_Click(sender As Object, e As RoutedEventArgs) Handles btnAdicionarAoCarrinho.Click
        Dim itemSelecionado As ItemDisponivel = CType(cmbItemEstoque.SelectedItem, ItemDisponivel)
        If itemSelecionado Is Nothing Then Return

        ' --------------------------- VALIDAÇÕES ---------------------------

        ' 1. Verifica se a quantidade é válida (número inteiro positivo)
        Dim qtdRetirar As Integer
        If Not Integer.TryParse(txtQuantidadeRetirar.Text, qtdRetirar) OrElse qtdRetirar <= 0 Then
            MessageBox.Show("Quantidade inválida. Digite um número maior que zero.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        ' 2. Verifica se há estoque suficiente
        If qtdRetirar > itemSelecionado.QuantidadeEmEstoque Then
            MessageBox.Show(String.Format("Estoque insuficiente. Você só pode retirar no máximo {0} peças deste lote.", itemSelecionado.QuantidadeEmEstoque),
                            "Erro de Estoque", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' 3. Diminui a quantidade disponível no item selecionado (memória)
        itemSelecionado.QuantidadeEmEstoque -= qtdRetirar

        ' 4. Verifica se esse lote já está no carrinho
        Dim itemExistenteNoCarrinho = Carrinho.FirstOrDefault(Function(c) c.LoteDeEstoque.ID_Item = itemSelecionado.ID_Item)

        If itemExistenteNoCarrinho IsNot Nothing Then
            ' Se já existe, apenas soma a quantidade
            itemExistenteNoCarrinho.QuantidadeRetirar += qtdRetirar
            dgItensSaida.Items.Refresh() ' Atualiza a tabela
        Else
            ' Se é novo, cria o item e adiciona ao carrinho
            Dim novoItemCarrinho As New ItemCarrinho With {
                .LoteDeEstoque = itemSelecionado,
                .QuantidadeRetirar = qtdRetirar
            }
            Carrinho.Add(novoItemCarrinho)
        End If

        ' 5. Atualiza as listas visuais e limpa o campo de seleção
        cmbItemEstoque.Items.Refresh()
        cmbItemEstoque.SelectedItem = Nothing
        txtQuantidadeRetirar.Text = "1"
    End Sub


    ' =============================================================
    ' Botão "Remover do Carrinho"
    ' =============================================================
    Private Sub BtnRemoverDoCarrinho_Click(sender As Object, e As RoutedEventArgs)
        ' Pega o item que será removido (vem do botão de dentro do DataGrid)
        Dim itemParaRemover As ItemCarrinho = CType(CType(sender, Button).DataContext, ItemCarrinho)

        ' 1. Devolve a quantidade retirada ao estoque
        Dim loteDeEstoque As ItemDisponivel = itemParaRemover.LoteDeEstoque
        loteDeEstoque.QuantidadeEmEstoque += itemParaRemover.QuantidadeRetirar

        ' 2. Remove o item da lista de carrinho
        Carrinho.Remove(itemParaRemover)

        ' 3. Atualiza visualmente o combo de estoque
        cmbItemEstoque.Items.Refresh()
    End Sub


    ' =============================================================
    ' Atualiza botões, pontuação e regras de troca/doação
    ' =============================================================
    Private Sub AtualizarLogicaCheckout(sender As Object, e As NotifyCollectionChangedEventArgs)
        ' Desativa os botões até validar as condições
        btnConfirmarTroca.IsEnabled = False
        btnConfirmarDoacaoPura.IsEnabled = False
        btnConfirmarTroca.ToolTip = "Debita os pontos do saldo do beneficiário."

        ' Se não há beneficiário ou o carrinho está vazio, zera tudo
        If beneficiarioSelecionado Is Nothing OrElse Carrinho.Count = 0 Then
            txtCustoTotalPontos.Text = "0"
            Return
        End If

        ' Soma o custo total dos itens no carrinho
        Dim custoTotal As Integer = Carrinho.Sum(Function(i) i.CustoTotal)

        ' Verifica se há algum item essencial (roupas prioritárias)
        Dim temItemEssencial As Boolean = Carrinho.Any(Function(i) i.IsEssencial)

        txtCustoTotalPontos.Text = custoTotal.ToString()
        Dim saldoBeneficiario As Integer = beneficiarioSelecionado.SaldoPontos

        ' ---------------- LÓGICA DE BLOQUEIO ----------------

        ' 1. Se o saldo é menor que o custo, não pode trocar
        Dim podeTrocar As Boolean = True
        If saldoBeneficiario < custoTotal Then
            podeTrocar = False
            btnConfirmarTroca.ToolTip = "Saldo de pontos insuficiente."
        End If

        ' 2. Se modo alerta está ativo e há itens essenciais, bloqueia troca
        If isModoAlertaAtivo AndAlso temItemEssencial Then
            podeTrocar = False
            btnConfirmarTroca.ToolTip = "Troca de itens essenciais está bloqueada durante um Alerta de Frio."
        End If

        ' Atualiza o estado dos botões
        btnConfirmarTroca.IsEnabled = podeTrocar
        btnConfirmarDoacaoPura.IsEnabled = True
    End Sub


    ' =============================================================
    ' Botão "Confirmar Troca" - grava no banco (com custo)
    ' =============================================================
    Private Sub BtnConfirmarTroca_Click(sender As Object, e As RoutedEventArgs) Handles btnConfirmarTroca.Click
        If beneficiarioSelecionado Is Nothing Then
            MessageBox.Show("Nenhum beneficiário foi selecionado.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' Calcula o total de pontos que serão debitados
        Dim custoTotal As Integer = Carrinho.Sum(Function(i) i.CustoTotal)

        Try
            ' Atenção: ainda não compatível com a função do banco (usa tipo diferente)
            DataAccess.SalvarNovaEntrega(beneficiarioSelecionado, "Troca", custoTotal, Carrinho.ToList())

            MessageBox.Show(String.Format("Troca registrada com sucesso!{0}{1} pontos foram debitados do saldo de {2}.", vbCrLf, custoTotal, beneficiarioSelecionado.Nome),
                            "Troca Efetuada", MessageBoxButton.OK, MessageBoxImage.Information)
            LimparTelaEAtualizarDados()
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro ao salvar troca: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' Botão "Confirmar Doação Pura" - grava no banco (sem custo)
    ' =============================================================
    Private Sub BtnConfirmarDoacaoPura_Click(sender As Object, e As RoutedEventArgs) Handles btnConfirmarDoacaoPura.Click
        If beneficiarioSelecionado Is Nothing Then
            MessageBox.Show("Nenhum beneficiário foi selecionado.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' Se o modo alerta está ativo e há itens essenciais,
        ' mostra um aviso caso o beneficiário não seja vulnerável
        Dim temItemEssencial As Boolean = Carrinho.Any(Function(i) i.IsEssencial)
        If isModoAlertaAtivo AndAlso temItemEssencial AndAlso Not beneficiarioSelecionado.IsVulneravel Then
            Dim msg As String = String.Format(
                "ATENÇÃO: Você está liberando itens essenciais para uma pessoa NÃO prioritária durante um alerta.{0}{0}A prioridade deve ser para pessoas em situação de vulnerabilidade.{0}{0}Deseja continuar mesmo assim?",
                vbCrLf)
            Dim resultado As MessageBoxResult = MessageBox.Show(msg, "Aviso de Prioridade (RF11)", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            If resultado = MessageBoxResult.No Then Return
        End If

        Try
            ' Salva a doação pura (sem custo)
            DataAccess.SalvarNovaEntrega(beneficiarioSelecionado, "Doação Pura", 0, Carrinho.ToList())

            MessageBox.Show(String.Format("Doação Pura registrada com sucesso para {0}.", beneficiarioSelecionado.Nome),
                            "Doação Efetuada", MessageBoxButton.OK, MessageBoxImage.Information)
            LimparTelaEAtualizarDados()
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro ao salvar doação pura: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' Limpa os campos e recarrega os dados após salvar
    ' =============================================================
    Private Sub LimparTelaEAtualizarDados()
        Carrinho.Clear()
        cmbBeneficiario.SelectedItem = Nothing
        cmbItemEstoque.SelectedItem = Nothing
        panelStatsBeneficiario.Visibility = Visibility.Collapsed
        txtCustoTotalPontos.Text = "0"
        AtualizarLogicaCheckout(Nothing, Nothing)

        ' Recarrega dados do banco
        CarregarDadosDoBanco()
        cmbBeneficiario.ItemsSource = ListaBeneficiarios
        cmbBeneficiario.Items.Refresh()
        cmbItemEstoque.ItemsSource = ListaEstoque
        cmbItemEstoque.Items.Refresh()
    End Sub

End Class


' =============================================================
' CLASSE AUXILIAR: ItemCarrinho
' -------------------------------------------------------------
' Representa um item que o operador adicionou no carrinho.
' =============================================================
Public Class ItemCarrinho

    ' Referência ao lote original do estoque
    Public Property LoteDeEstoque As ItemDisponivel

    ' Quantidade retirada desse lote
    Public Property QuantidadeRetirar As Integer

    ' --- PROPRIEDADES AUTOMÁTICAS PARA MOSTRAR NO DATAGRID ---

    Public ReadOnly Property Nome As String
        Get
            Return LoteDeEstoque.Nome
        End Get
    End Property

    Public ReadOnly Property Tamanho As String
        Get
            Return LoteDeEstoque.Tamanho
        End Get
    End Property

    Public ReadOnly Property CustoUnitario As Integer
        Get
            Return LoteDeEstoque.CustoPontos
        End Get
    End Property

    Public ReadOnly Property IsEssencial As Boolean
        Get
            Return LoteDeEstoque.IsEssencial
        End Get
    End Property

    ' Calcula automaticamente o custo total (quantidade x custo por peça)
    Public ReadOnly Property CustoTotal As Integer
        Get
            Return QuantidadeRetirar * LoteDeEstoque.CustoPontos
        End Get
    End Property

End Class
