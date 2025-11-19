' =============================================================
' ARQUIVO: PageEntradaDoacao.vb
' -------------------------------------------------------------
' OBJETIVO: Gerenciar o formulário de registro de uma nova doação.
' Inclui a seleção do Doador/Ponto de Coleta e o registro dos itens
' doados com cálculo automático de pontos.
' LINGUAGEM: Visual Basic .NET
' =============================================================

Imports System.Collections.ObjectModel ' Para ObservableCollection
Imports System.ComponentModel ' Para INotifyPropertyChanged
Imports System.Collections.Specialized ' Para NotifyCollectionChangedEventArgs (usado no ObservableCollection)

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PageEntradaDoacao
' -------------------------------------------------------------
' Contém toda a lógica para a tela de entrada de doações.
' =============================================================
Public Class PageEntradaDoacao

    ' --- ESTE É O CONSTRUTOR ---
    ' O construtor é a primeira coisa que roda quando a classe é criada.
    ' Ele deve conter APENAS a inicialização dos componentes da tela.
    Public Sub New()
        InitializeComponent()
    End Sub

    ' --- DADOS DA PÁGINA ---
    ' Lista que guarda os itens que estão sendo doados na grade da tela (DataGrid)
    Private ReadOnly ItensDaDoacao As New ObservableCollection(Of ItemDoacao)()
    ' Listas para preencher os Combobox e a grade (DataGrid)
    Private ListaCategorias As New List(Of ParamCategoria)
    Private ListaTamanhos As New List(Of ParamTamanho)
    Private ListaCondicoes As New List(Of ParamCondicao)
    Private ListaDestinos As New List(Of ParamStatusTriagem)
    Private ListaPessoas As New List(Of Pessoa)
    Private ListaPontosColeta As New List(Of PontoColeta)

    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Esta lógica SÓ PODE rodar aqui, quando a página é carregada pela primeira vez.
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Liga a lista de itens doados à tabela (DataGrid) da tela
        dgItensDoacao.ItemsSource = ItensDaDoacao
        ' Carrega as listas de opções (parâmetros e cadastros) do banco
        CarregarDadosDoBanco()
        ' Usa as listas carregadas para preencher os Combobox
        PreencherComboBoxes()
        ' Adiciona um "observador" (listener) na lista da doação para recalcular pontos
        AddHandler ItensDaDoacao.CollectionChanged, AddressOf ItensDaDoacao_CollectionChanged
    End Sub

    ' =============================================================
    ' FUNÇÃO: CarregarDadosDoBanco
    ' -------------------------------------------------------------
    ' Busca todas as listas de parâmetros (opções) e cadastros (Doador/Ponto)
    ' no DataAccess.
    ' =============================================================
    Private Sub CarregarDadosDoBanco()
        Try
            ' Busca as listas de parâmetros (opções)
            ListaCategorias = DataAccess.GetTodosParamCategorias()
            ListaTamanhos = DataAccess.GetTodosParamTamanhos()
            ListaCondicoes = DataAccess.GetTodosParamCondicoes()
            ListaDestinos = DataAccess.GetTodosParamStatusTriagem()
            ' Busca as listas de cadastros (Doador e Ponto de Coleta)
            ListaPessoas = DataAccess.GetTodasPessoasAtivas()
            ListaPontosColeta = DataAccess.GetTodosPontosColetaAtivos()
        Catch ex As Exception
            ' Em caso de erro ao buscar dados iniciais
            MessageBox.Show(String.Format("Erro fatal ao carregar parâmetros: {0}", ex.Message),
              "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: PreencherComboBoxes
    ' -------------------------------------------------------------
    ' Conecta as listas carregadas aos controles visuais da tela.
    ' =============================================================
    Private Sub PreencherComboBoxes()
        ' Preenche os Combobox do cabeçalho
        cmbDoador.ItemsSource = ListaPessoas
        cmbPontoColeta.ItemsSource = ListaPontosColeta
        ' Define a data da doação como a data de hoje
        dpDataDoacao.SelectedDate = DateTime.Now

        ' Preenche os Combobox dentro da tabela (DataGrid Columns)
        colCategoria.ItemsSource = ListaCategorias
        colTamanho.ItemsSource = ListaTamanhos
        colCondicao.ItemsSource = ListaCondicoes
        colDestino.ItemsSource = ListaDestinos
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnAdicionarItem_Click
    ' -------------------------------------------------------------
    ' Adiciona uma nova linha (ItemDoacao) na grade.
    ' =============================================================
    Private Sub BtnAdicionarItem_Click(sender As Object, e As RoutedEventArgs) Handles btnAdicionarItem.Click
        ' Cria um novo objeto ItemDoacao e adiciona na lista, que se reflete na grade
        ItensDaDoacao.Add(New ItemDoacao())
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnExcluirItem_Click
    ' -------------------------------------------------------------
    ' Remove a linha de item doado que o usuário clicou.
    ' =============================================================
    Private Sub BtnExcluirItem_Click(sender As Object, e As RoutedEventArgs)
        ' Pega o objeto ItemDoacao que está ligado ao botão (sender) que foi clicado
        Dim itemParaExcluir As ItemDoacao = CType(CType(sender, Button).DataContext, ItemDoacao)
        ' Remove o item da lista
        ItensDaDoacao.Remove(itemParaExcluir)
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnSalvarDoacao_Click
    ' -------------------------------------------------------------
    ' Valida os dados da doação e, se estiverem corretos, salva a
    ' transação completa no banco de dados.
    ' =============================================================
    Private Sub BtnSalvarDoacao_Click(sender As Object, e As RoutedEventArgs) Handles btnSalvarDoacao.Click
        ' 1. Validação do Cabeçalho (Doador, Ponto de Coleta)
        Dim doadorSelecionado = CType(cmbDoador.SelectedItem, Pessoa)
        Dim pontoSelecionado = CType(cmbPontoColeta.SelectedItem, PontoColeta)

        ' Verifica se Doador OU Ponto OU se a lista de itens está vazia
        If doadorSelecionado Is Nothing OrElse
     pontoSelecionado Is Nothing OrElse
     ItensDaDoacao.Count = 0 Then

            MessageBox.Show("Preencha o Doador, Ponto de Coleta e adicione pelo menos 1 item.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return ' Para a execução
        End If

        ' 2. Validação dos Itens na Grade
        For Each item In ItensDaDoacao
            ' Verifica se algum campo essencial da linha está vazio (Nothing)
            If item.Categoria Is Nothing Or item.Tamanho Is Nothing Or item.Condicao Is Nothing Or item.Destino Is Nothing Then
                MessageBox.Show("Todos os itens na grade devem ter Categoria, Tamanho, Condição e Destino preenchidos.", "Itens Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return ' Para a execução
            End If
        Next

        ' 3. Chama a função para calcular o total de pontos da doação
        Dim pontosTotais As Integer = CalcularTotalPontos()

        Try
            ' 4. Chama o DataAccess (a ponte) para salvar a doação completa no banco
            DataAccess.SalvarNovaDoacao(doadorSelecionado, pontoSelecionado, dpDataDoacao.SelectedDate.Value, pontosTotais, ItensDaDoacao)

            ' 5. Sucesso: Mostra a confirmação com o total de pontos
            MessageBox.Show(String.Format("Doação salva com sucesso!{0}{1} pontos foram creditados para {2}.", vbCrLf, pontosTotais, doadorSelecionado.Nome), "Doação Registrada", MessageBoxButton.OK, MessageBoxImage.Information)

            ' 6. Limpa a tela para começar um novo registro
            LimparTela()

        Catch ex As Exception
            ' Em caso de erro no salvamento
            MessageBox.Show(String.Format("Erro ao salvar doação: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: LimparTela
    ' -------------------------------------------------------------
    ' Zera todos os controles e a lista da doação.
    ' =============================================================
    Private Sub LimparTela()
        ' Limpa a seleção dos Combobox do cabeçalho
        cmbDoador.SelectedItem = Nothing
        cmbPontoColeta.SelectedItem = Nothing
        ' Zera a data para a data de hoje
        dpDataDoacao.SelectedDate = DateTime.Now
        ' Limpa a lista de itens da grade
        ItensDaDoacao.Clear()
        ' Zera o texto do total de pontos
        txtTotalPontos.Text = "0"
    End Sub

    ' --- MÉTODOS DE CÁLCULO DE PONTOS ---

    ' =============================================================
    ' FUNÇÃO: CalcularTotalPontos
    ' -------------------------------------------------------------
    ' Soma os pontos de todos os itens na lista ItensDaDoacao.
    ' =============================================================
    Private Function CalcularTotalPontos() As Integer
        Dim total As Integer = 0
        ' Loop: Percorre item por item na lista
        For Each item In ItensDaDoacao
            ' Soma a propriedade Pontos de cada item ao total
            total += item.Pontos
        Next
        Return total
    End Function

    ' =============================================================
    ' EVENTO: ItensDaDoacao_CollectionChanged
    ' -------------------------------------------------------------
    ' Disparado sempre que um item é ADICIONADO ou REMOVIDO da grade.
    ' O principal objetivo é ligar/desligar o monitor de alteração
    ' de propriedade (PropertyChanged) nos itens da grade.
    ' =============================================================
    Private Sub ItensDaDoacao_CollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        ' Recalcula e atualiza o total de pontos na tela
        txtTotalPontos.Text = CalcularTotalPontos().ToString()

        ' Condição: se um item foi ADICIONADO à lista
        If e.Action = NotifyCollectionChangedAction.Add Then
            ' Para cada novo item, adiciona o monitor de mudança de propriedade
            For Each item As ItemDoacao In e.NewItems
                ' Adiciona o EventHandler (o "gatilho" Item_PropertyChanged)
                AddHandler CType(item, INotifyPropertyChanged).PropertyChanged, AddressOf Item_PropertyChanged
            Next
            ' Condição: se um item foi REMOVIDO da lista
        ElseIf e.Action = NotifyCollectionChangedAction.Remove Then
            ' Para cada item removido, DESLIGA o monitor
            For Each item As ItemDoacao In e.OldItems
                ' Remove o EventHandler para evitar erros
                RemoveHandler CType(item, INotifyPropertyChanged).PropertyChanged, AddressOf Item_PropertyChanged
            Next
        End If
    End Sub

    ' =============================================================
    ' EVENTO: Item_PropertyChanged
    ' -------------------------------------------------------------
    ' Disparado quando uma propriedade (característica) de um item na
    ' grade é alterada (ex: o usuário troca a Categoria).
    ' =============================================================
    Private Sub Item_PropertyChanged(sender As Object, e As PropertyChangedEventArgs)
        ' Verifica se a propriedade que mudou foi especificamente a "Pontos"
        If e.PropertyName = "Pontos" Then
            ' Recalcula o total de pontos e atualiza o texto na tela
            txtTotalPontos.Text = CalcularTotalPontos().ToString()
        End If
    End Sub

End Class

' =============================================================
' RESUMO: FLUXO DA ENTRADA DE DOAÇÃO E CÁLCULO DE PONTOS
' -------------------------------------------------------------
' 1. CARREGAMENTO: O Page_Loaded carrega as listas de opções do banco.
' 2. ADIÇÃO/REMOÇÃO DE ITENS:
'    - A lista ItensDaDoacao é uma ObservableCollection.
'    - Quando um item é adicionado/removido, o evento CollectionChanged é
'      disparado para recalcular o total de pontos.
' 3. ALTERAÇÃO NA LINHA:
'    - Cada ItemDoacao implementa o INotifyPropertyChanged.
'    - Se o usuário mudar a Categoria de um item (o que altera seus Pontos),
'      o evento Item_PropertyChanged é disparado.
'    - A lógica de Item_PropertyChanged detecta a mudança na propriedade
'      "Pontos" e força o recalculo do total na tela, garantindo a atualização
'      em tempo real.
' 4. SALVAMENTO: O BtnSalvarDoacao_Click faz as validações e, em seguida,
'    envia todos os objetos (Doador, Ponto e a lista de Itens) para o DataAccess.
' =============================================================