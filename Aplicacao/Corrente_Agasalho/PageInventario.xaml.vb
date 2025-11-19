' =============================================================
' ARQUIVO: PageInventario.vb
' -------------------------------------------------------------
' OBJETIVO: Gerenciar a visualização e filtragem dos itens de
' inventário usando dados do banco (DataAccess).
' LINGUAGEM: Visual Basic .NET
' =============================================================

Imports System.ComponentModel ' Necessário para usar ICollectionView
Imports System.Windows.Data ' Para o ICollectionView (ferramenta de visualização e filtragem)

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PageInventario
' -------------------------------------------------------------
' Controla a interface, carrega os dados e aplica filtros na lista.
' =============================================================
Public Class PageInventario

    ' Lista principal que guarda todos os itens carregados do banco
    Private masterListInventario As New List(Of ItemInventario)()
    ' inventarioView: Objeto especial para fazer a filtragem e ordenação na lista visual
    Private inventarioView As ICollectionView

    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Ação que ocorre assim que a página é carregada.
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Primeiro, carrega a lista completa de itens
        CarregarInventarioDoBanco()

        ' Obtém o objeto de visualização (ICollectionView) a partir da lista principal
        inventarioView = CollectionViewSource.GetDefaultView(masterListInventario)
        ' Liga a visualização (que pode ser filtrada) ao DataGrid (tabela) na tela
        dgInventario.ItemsSource = inventarioView

        ' Carrega as opções dos Combobox (filtros)
        CarregarFiltrosDoBanco()
    End Sub

    ' =============================================================
    ' FUNÇÃO: CarregarInventarioDoBanco
    ' -------------------------------------------------------------
    ' Busca a lista de todos os itens de inventário no banco de dados.
    ' =============================================================
    Private Sub CarregarInventarioDoBanco()
        ' Bloco Try/Catch para tentar executar o código e capturar erros
        Try
            ' Chama a função no DataAccess para obter a lista de itens
            masterListInventario = DataAccess.GetItensInventario()
        Catch ex As Exception
            ' Em caso de erro, mostra uma mensagem de falha na conexão
            MessageBox.Show(String.Format("Erro fatal ao carregar inventário: {0}", ex.Message),
             "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: CarregarFiltrosDoBanco
    ' -------------------------------------------------------------
    ' Preenche os Combobox de filtro com as opções (Categorias, Tamanhos, Status)
    ' que estão salvas como parâmetros no banco.
    ' =============================================================
    Private Sub CarregarFiltrosDoBanco()
        Try
            ' Busca as Categorias no banco e cria uma lista apenas com o Nome
            Dim categorias = DataAccess.GetTodosParamCategorias().Select(Function(c) c.Nome).ToList()
            ' Busca os Tamanhos no banco e cria uma lista apenas com o Nome
            Dim tamanhos = DataAccess.GetTodosParamTamanhos().Select(Function(t) t.Nome).ToList()
            ' Busca os Status de triagem no banco e cria uma lista apenas com o Nome
            Dim status = DataAccess.GetTodosParamStatusTriagem().Select(Function(s) s.Nome).ToList()

            ' Adiciona a opção "(Todos)" na primeira posição de cada lista
            categorias.Insert(0, "(Todos)")
            tamanhos.Insert(0, "(Todos)")
            status.Insert(0, "(Todos)")

            ' Liga as listas aos Combobox na tela
            cmbFiltroCategoria.ItemsSource = categorias
            cmbFiltroTamanho.ItemsSource = tamanhos
            cmbFiltroStatus.ItemsSource = status
            ' Preenche o filtro "Essencial" com opções fixas
            cmbFiltroEssencial.ItemsSource = New List(Of String) From {"(Ambos)", "Sim", "Não"}

            ' Zera a seleção inicial dos filtros
            LimparFiltros()

        Catch ex As Exception
            ' Em caso de erro ao buscar os parâmetros, mostra aviso
            MessageBox.Show(String.Format("Erro fatal ao carregar filtros: {0}", ex.Message),
             "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnAplicarFiltros_Click
    ' -------------------------------------------------------------
    ' Define uma regra de filtro para a visualização do inventário.
    ' A função retorna True (mostra o item) ou False (esconde o item).
    ' =============================================================
    Private Sub BtnAplicarFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnAplicarFiltros.Click

        ' Define a função de filtro para a visualização do inventário (inventarioView)
        inventarioView.Filter = Function(item)
                                    ' Converte o item atual da lista para o tipo ItemInventario
                                    Dim obj = CType(item, ItemInventario)

                                    ' 1. Lógica do Filtro Categoria
                                    Dim filtroCat = CType(cmbFiltroCategoria.SelectedItem, String)
                                    ' Verifica se tem um filtro selecionado E se não é "(Todos)" E se a Categoria do item NÃO é igual ao filtro
                                    If Not String.IsNullOrEmpty(filtroCat) AndAlso
                   Not String.Equals(filtroCat, "(Todos)", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(obj.Categoria, filtroCat, StringComparison.OrdinalIgnoreCase) Then
                                        ' Se não passar no filtro, esconde o item
                                        Return False
                                    End If

                                    ' 2. Lógica do Filtro Tamanho
                                    Dim filtroTam = CType(cmbFiltroTamanho.SelectedItem, String)
                                    ' Verifica se tem filtro E não é "(Todos)" E se o Tamanho do item NÃO é igual ao filtro
                                    If Not String.IsNullOrEmpty(filtroTam) AndAlso
                   Not String.Equals(filtroTam, "(Todos)", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(obj.Tamanho, filtroTam, StringComparison.OrdinalIgnoreCase) Then
                                        Return False ' Esconde o item
                                    End If

                                    ' 3. Lógica do Filtro Status
                                    Dim filtroStatus = CType(cmbFiltroStatus.SelectedItem, String)
                                    ' Verifica se tem filtro E não é "(Todos)" E se o Status do item NÃO é igual ao filtro
                                    If Not String.IsNullOrEmpty(filtroStatus) AndAlso
                   Not String.Equals(filtroStatus, "(Todos)", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(obj.Status, filtroStatus, StringComparison.OrdinalIgnoreCase) Then
                                        Return False ' Esconde o item
                                    End If

                                    ' 4. Lógica do Filtro Peça Essencial
                                    Dim filtroEssencial = CType(cmbFiltroEssencial.SelectedItem, String)
                                    ' Verifica se tem filtro E não é "(Ambos)"
                                    If Not String.IsNullOrEmpty(filtroEssencial) AndAlso
                   Not String.Equals(filtroEssencial, "(Ambos)", StringComparison.OrdinalIgnoreCase) Then
                                        ' Define se o valor esperado é True (para "Sim") ou False (para "Não")
                                        Dim éEssencial = String.Equals(filtroEssencial, "Sim", StringComparison.OrdinalIgnoreCase)
                                        ' Se a propriedade IsEssencial do item for diferente do valor esperado
                                        If obj.IsEssencial <> éEssencial Then
                                            Return False ' Esconde o item
                                        End If
                                    End If

                                    ' 5. Lógica do Filtro Data
                                    ' Verifica se alguma data foi selecionada no calendário
                                    If dpFiltroData.SelectedDate IsNot Nothing Then
                                        ' Compara apenas a parte da data, ignorando o horário
                                        If obj.DataEntrada.Date <> dpFiltroData.SelectedDate.Value.Date Then
                                            Return False ' Esconde o item
                                        End If
                                    End If

                                    ' Se o item passou por todos os filtros, retorna True
                                    Return True
                                End Function
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnLimparFiltros_Click
    ' -------------------------------------------------------------
    ' Ação do botão: limpa a seleção dos Combobox e remove o filtro.
    ' =============================================================
    Private Sub BtnLimparFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnLimparFiltros.Click
        ' Zera a seleção visual dos controles de filtro
        LimparFiltros()
        ' Remove a função de filtro, fazendo com que todos os itens sejam exibidos novamente
        inventarioView.Filter = Nothing
    End Sub

    ' =============================================================
    ' FUNÇÃO: LimparFiltros
    ' -------------------------------------------------------------
    ' Função auxiliar: zera a seleção de todos os controles de filtro.
    ' =============================================================
    Private Sub LimparFiltros()
        ' Define o item selecionado como o primeiro da lista, que é "(Todos)" ou "(Ambos)"
        cmbFiltroCategoria.SelectedIndex = 0
        cmbFiltroTamanho.SelectedIndex = 0
        cmbFiltroStatus.SelectedIndex = 0
        cmbFiltroEssencial.SelectedIndex = 0
        ' Limpa a data selecionada no calendário
        dpFiltroData.SelectedDate = Nothing
    End Sub

End Class

' =============================================================
' RESUMO: FILTRAGEM DE INVENTÁRIO
' -------------------------------------------------------------
' 1. ESTRUTURA: A página usa um objeto ICollectionView, que é uma "capa"
'    por cima da lista de dados (masterListInventario), permitindo filtrar
'    e ordenar os dados sem alterar a lista original.
' 2. LÓGICA DO FILTRO: Ao clicar em "Filtrar", o sistema atribui uma função
'    (inventarioView.Filter) que é executada para CADA item do inventário.
' 3. COMPARAÇÃO: Dentro dessa função, ele verifica se o item bate com cada
'    filtro selecionado (Categoria, Tamanho, etc.). Se o item falhar em
'    qualquer uma das verificações, a função retorna False e o item é
'    escondido da visualização.
' 4. CORREÇÃO: Usamos o String.Equals(..., StringComparison.OrdinalIgnoreCase)
'    para garantir que as comparações de texto (como "Sim" ou "SIM")
'    funcionem corretamente, ignorando letras maiúsculas ou minúsculas.
' =============================================================