' =============================================================
' IMPORTAÇÕES DE BIBLIOTECAS
' -------------------------------------------------------------
' - ObservableCollection: lista dinâmica que atualiza a interface automaticamente.
' - ComponentModel: usado para propriedades que notificam mudanças na interface.
' =============================================================
Imports System.Collections.ObjectModel
Imports System.ComponentModel

' =============================================================
' CLASSE PRINCIPAL: PageParametros
' -------------------------------------------------------------
' Esta página é responsável por gerenciar as Tabelas de Parâmetros do sistema,
' como Categorias, Tamanhos e Condições (RF03 e RF04).
' Ela busca as informações do banco de dados e permite adicionar, editar e salvar.
' =============================================================
Public Class PageParametros

    ' =============================================================
    ' DECLARAÇÃO DAS LISTAS PRINCIPAIS
    ' -------------------------------------------------------------
    ' São listas dinâmicas (ObservableCollection) que armazenam
    ' os dados mostrados em tela e se atualizam automaticamente.
    ' =============================================================
    Private ListaCategorias As New ObservableCollection(Of ParamCategoria)
    Private ListaTamanhos As New ObservableCollection(Of String)
    Private ListaCondicoes As New ObservableCollection(Of String)


    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Executado automaticamente quando a página é carregada.
    ' Verifica se o usuário é administrador e carrega as informações iniciais.
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Verifica se o usuário logado tem permissão de administrador
        If Not Sessao.EhAdmin() Then
            MessageBox.Show("Acesso restrito a administradores.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning)
            Me.IsEnabled = False ' Desativa os campos da tela
        End If

        ' Liga (conecta) as listas aos componentes visuais da interface
        dgCategorias.ItemsSource = ListaCategorias
        ListViewTamanhos.ItemsSource = ListaTamanhos
        ListViewCondicoes.ItemsSource = ListaCondicoes

        ' Carrega as informações do banco de dados
        CarregarParametrosDoBanco()
    End Sub


    ' =============================================================
    ' MÉTODO: CarregarParametrosDoBanco
    ' -------------------------------------------------------------
    ' Faz a leitura das três abas (Categorias, Tamanhos e Condições)
    ' diretamente do banco de dados via DataAccess.
    ' =============================================================
    Private Sub CarregarParametrosDoBanco()
        Try
            ' --- ABA 1: CATEGORIAS ---
            ' Limpa a lista atual e recarrega do banco
            ListaCategorias.Clear()
            Dim categoriasDB = DataAccess.GetTodosParamCategorias()
            For Each cat In categoriasDB
                ListaCategorias.Add(cat)
            Next

            ' --- ABA 2: TAMANHOS ---
            ListaTamanhos.Clear()
            Dim tamanhosDB = DataAccess.GetTodosParamTamanhos().Select(Function(t) t.Nome)
            For Each tam In tamanhosDB
                ListaTamanhos.Add(tam)
            Next

            ' --- ABA 3: CONDIÇÕES ---
            ListaCondicoes.Clear()
            Dim condicoesDB = DataAccess.GetTodosParamCondicoes().Select(Function(c) c.Nome)
            For Each cond In condicoesDB
                ListaCondicoes.Add(cond)
            Next

        Catch ex As Exception
            ' Caso haja erro na conexão ou leitura do banco
            MessageBox.Show(String.Format("Erro fatal ao carregar parâmetros: {0}", ex.Message),
                            "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' ABA 1: CATEGORIAS
    ' -------------------------------------------------------------
    ' Parte da tela que controla as categorias de doações/retiradas,
    ' incluindo pontos e indicação se é item essencial.
    ' =============================================================

    ' =============================================================
    ' BOTÃO: Adicionar Categoria
    ' -------------------------------------------------------------
    ' Cria uma nova categoria localmente e adiciona na lista exibida.
    ' O salvamento no banco só ocorre ao clicar em "Salvar Categorias".
    ' =============================================================
    Private Sub BtnAdicionarCategoria_Click(sender As Object, e As RoutedEventArgs) Handles btnAdicionarCategoria.Click
        ' Verifica se os campos obrigatórios foram preenchidos corretamente
        If String.IsNullOrWhiteSpace(txtNovaCategoriaNome.Text) OrElse
           Not IsNumeric(txtNovaCategoriaPtsDoacao.Text) OrElse
           Not IsNumeric(txtNovaCategoriaPtsRetirada.Text) Then

            MessageBox.Show("Preencha o Nome e os valores de Pontos (números) corretamente.",
                            "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' Cria o novo objeto de categoria com os dados da tela
        Dim novaCategoria As New ParamCategoria With {
            .Nome = txtNovaCategoriaNome.Text.Trim(),
            .PontosDoacao = CInt(txtNovaCategoriaPtsDoacao.Text),
            .PontosRetirada = CInt(txtNovaCategoriaPtsRetirada.Text),
            .IsEssencial = chkNovaCategoriaEssencial.IsChecked.GetValueOrDefault(False)
        }

        ' Adiciona a categoria na lista da interface
        ListaCategorias.Add(novaCategoria)

        ' Limpa os campos para próxima inclusão
        txtNovaCategoriaNome.Clear()
        txtNovaCategoriaPtsDoacao.Clear()
        txtNovaCategoriaPtsRetirada.Clear()
        chkNovaCategoriaEssencial.IsChecked = False
    End Sub


    ' =============================================================
    ' BOTÃO: Salvar Categorias
    ' -------------------------------------------------------------
    ' Envia todas as categorias (novas e editadas) para o banco de dados.
    ' =============================================================
    Private Sub BtnSalvarCategorias_Click(sender As Object, e As RoutedEventArgs) Handles btnSalvarCategorias.Click
        Try
            ' Envia a lista completa para o DataAccess salvar no banco
            DataAccess.SalvarParamCategorias(ListaCategorias)

            MessageBox.Show("Categorias e Pontos salvos com sucesso!",
                            "Parâmetros Salvos", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro ao salvar categorias: {0}", ex.Message),
                            "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' ABA 2: TAMANHOS
    ' -------------------------------------------------------------
    ' Permite adicionar tamanhos novos e salvar diretamente no banco.
    ' =============================================================
    Private Sub BtnAdicionarTamanho_Click(sender As Object, e As RoutedEventArgs) Handles btnAdicionarTamanho.Click
        ' Verifica se o campo não está vazio
        If Not String.IsNullOrWhiteSpace(txtNovoTamanho.Text) Then
            Dim novoTamanho = txtNovoTamanho.Text.Trim()

            ' Verifica se o tamanho ainda não foi adicionado
            If Not ListaTamanhos.Contains(novoTamanho) Then
                ' Adiciona na lista
                ListaTamanhos.Add(novoTamanho)

                ' Salva no banco usando método genérico
                SalvarListaGenerica(ListaTamanhos.ToList(), "param_tamanhos", "nome", "Tamanhos")

                ' Limpa o campo
                txtNovoTamanho.Clear()
            End If
        End If
    End Sub


    ' =============================================================
    ' ABA 3: CONDIÇÕES
    ' -------------------------------------------------------------
    ' Permite cadastrar condições do item (ex: Novo, Usado, Bom estado).
    ' =============================================================
    Private Sub BtnAdicionarCondicao_Click(sender As Object, e As RoutedEventArgs) Handles btnAdicionarCondicao.Click
        ' Verifica se o campo não está vazio
        If Not String.IsNullOrWhiteSpace(txtNovaCondicao.Text) Then
            Dim novaCondicao = txtNovaCondicao.Text.Trim()

            ' Evita duplicação
            If Not ListaCondicoes.Contains(novaCondicao) Then
                ' Adiciona na lista e salva no banco
                ListaCondicoes.Add(novaCondicao)
                SalvarListaGenerica(ListaCondicoes.ToList(), "param_condicoes", "nome", "Condições")

                ' Limpa o campo
                txtNovaCondicao.Clear()
            End If
        End If
    End Sub


    ' =============================================================
    ' MÉTODO GENÉRICO: SalvarListaGenerica
    ' -------------------------------------------------------------
    ' Reutilizado para salvar tanto tamanhos quanto condições.
    ' Recebe o nome da tabela e o tipo, e chama o método do DataAccess.
    ' =============================================================
    Private Sub SalvarListaGenerica(lista As List(Of String), nomeTabela As String, nomeColuna As String, nomeTipo As String)
        Try
            ' Manda salvar no banco (método genérico do DataAccess)
            DataAccess.SalvarListaParametros(lista, nomeTabela, nomeColuna)

            ' Mostra mensagem de sucesso
            MessageBox.Show(String.Format("{0} salvos com sucesso!", nomeTipo),
                            "Parâmetros Salvos", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            ' Mostra mensagem de erro com o tipo afetado
            MessageBox.Show(String.Format("Erro ao salvar {0}: {1}", nomeTipo, ex.Message),
                            "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

End Class
