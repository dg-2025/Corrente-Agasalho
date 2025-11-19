' =============================================================
' Importações de bibliotecas usadas no código
' =============================================================

' Permite usar o HttpClient (fazer requisições na internet)
Imports System.Net.Http

' Permite trabalhar com JSON — transformar texto em objeto e vice-versa
Imports Newtonsoft.Json

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PagePontosColeta
' -------------------------------------------------------------
' Essa página é responsável por cadastrar, editar e excluir
' pontos de coleta (locais onde são recebidas doações).
' Também integra com a API pública do ViaCEP, que preenche
' automaticamente o endereço a partir do CEP informado.
' =============================================================
Public Class PagePontosColeta

    ' =============================================================
    ' OBJETOS PRINCIPAIS DA CLASSE
    ' =============================================================

    ' Cria um cliente HTTP que será usado para acessar a API do ViaCEP
    ' O HttpClient é "Shared" (compartilhado) para evitar criar várias conexões
    Private Shared ReadOnly httpClient As New HttpClient()

    ' Lista principal com todos os pontos de coleta trazidos do banco
    Private masterListPontos As New List(Of PontoColeta)()

    ' Guarda o ponto de coleta selecionado na interface
    Private pontoSelecionado As PontoColeta = Nothing


    ' =============================================================
    ' EVENTO PRINCIPAL - executa quando a página é carregada
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Somente administradores podem acessar essa tela
        If Not Sessao.EhAdmin() Then
            MessageBox.Show("Acesso restrito a administradores.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning)
            Me.IsEnabled = False ' desativa os campos da tela
        End If

        ' Chama a função que busca todos os pontos cadastrados no banco
        CarregarPontosDoBanco()
    End Sub


    ' =============================================================
    ' Função que carrega os pontos de coleta existentes no banco
    ' =============================================================
    Private Sub CarregarPontosDoBanco()
        Try
            ' Pede para o módulo DataAccess buscar todos os pontos ativos
            masterListPontos = DataAccess.GetTodosPontosColetaAtivos()

            ' Liga a lista ao componente visual (ListView)
            ListViewPontos.ItemsSource = masterListPontos
            ListViewPontos.Items.Refresh()

            ' Limpa os campos da tela (caso tenha algo de antes)
            LimparFormulario()
        Catch ex As Exception
            ' Se houver erro de banco (ex: conexão perdida), mostra aviso
            MessageBox.Show(String.Format("Erro fatal ao carregar pontos de coleta: {0}", ex.Message),
                            "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' BOTÃO: Buscar CEP — integração com a API ViaCEP
    ' -------------------------------------------------------------
    ' Quando o usuário digita um CEP e clica em "Buscar CEP",
    ' o sistema envia uma requisição HTTP para o site ViaCEP,
    ' recebe uma resposta em formato JSON e preenche os campos
    ' de endereço automaticamente.
    ' =============================================================
    Private Async Sub BtnBuscarCEP_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscarCEP.Click
        ' Captura o texto digitado no campo CEP e remove o traço
        Dim cep As String = txtCEP.Text.Trim().Replace("-", "")

        ' Verifica se o CEP tem 8 números válidos
        If cep.Length <> 8 OrElse Not IsNumeric(cep) Then
            MessageBox.Show("CEP inválido. Digite apenas 8 números.", "Erro de Formato", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        ' Desabilita o botão enquanto busca (para evitar cliques repetidos)
        btnBuscarCEP.IsEnabled = False
        btnBuscarCEP.Content = "Buscando..."

        Try
            ' Monta a URL da API do ViaCEP
            ' Exemplo: https://viacep.com.br/ws/01001000/json/
            Dim url As String = String.Format("https://viacep.com.br/ws/{0}/json/", cep)

            ' Faz a requisição HTTP de forma assíncrona (sem travar a tela)
            Dim response As HttpResponseMessage = Await httpClient.GetAsync(url)

            ' Verifica se a resposta da API foi bem-sucedida (código 200 OK)
            If response.IsSuccessStatusCode Then

                ' Lê o conteúdo da resposta (texto JSON)
                Dim jsonString As String = Await response.Content.ReadAsStringAsync()

                ' Converte o JSON recebido em um objeto do tipo ViaCepResultPontoColeta
                ' (Essa classe está declarada no final do arquivo)
                Dim resultadoCEP As ViaCepResultPontoColeta =
                    JsonConvert.DeserializeObject(Of ViaCepResultPontoColeta)(jsonString)

                ' Verifica se o objeto foi preenchido corretamente
                ' O campo "logradouro" indica se o CEP existe
                If resultadoCEP IsNot Nothing AndAlso resultadoCEP.Logradouro IsNot Nothing Then

                    ' Preenche os campos da tela com as informações do CEP
                    txtRua.Text = resultadoCEP.Logradouro
                    txtBairro.Text = resultadoCEP.Bairro
                    txtCidade.Text = resultadoCEP.Localidade
                    txtUF.Text = resultadoCEP.Uf

                    ' Coloca o foco no campo "Número" para o usuário continuar digitando
                    txtNumero.Focus()
                Else
                    ' Caso o CEP não exista
                    MessageBox.Show("CEP não encontrado ou inexistente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
                    LimparCamposCEP()
                End If
            Else
                ' Caso o servidor retorne erro (ex: 404, 500)
                MessageBox.Show("Não foi possível conectar à API ViaCEP.", "Erro de Rede", MessageBoxButton.OK, MessageBoxImage.Error)
            End If

        Catch ex As Exception
            ' Qualquer outro erro inesperado (internet, JSON, etc.)
            MessageBox.Show(String.Format("Ocorreu um erro inesperado ao buscar o CEP: {0}", ex.Message),
                            "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

        ' Restaura o botão após o término da busca
        btnBuscarCEP.IsEnabled = True
        btnBuscarCEP.Content = "Buscar CEP"
    End Sub


    ' =============================================================
    ' BOTÃO: Salvar ponto de coleta (novo ou editar)
    ' =============================================================
    Private Sub BtnSalvar_Click(sender As Object, e As RoutedEventArgs) Handles btnSalvar.Click
        ' Valida se o nome e a rua foram preenchidos
        If String.IsNullOrWhiteSpace(txtNome.Text) OrElse String.IsNullOrWhiteSpace(txtRua.Text) Then
            MessageBox.Show("O Nome do Ponto e o Endereço (via CEP) são obrigatórios.",
                            "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            ' Se já existe um ponto selecionado, estamos editando
            If pontoSelecionado IsNot Nothing Then
                ' Atualiza os dados do objeto selecionado com o que foi digitado
                PreencherObjetoPontoColeta(pontoSelecionado)

                ' Manda o DataAccess atualizar no banco
                DataAccess.AtualizarPontoColeta(pontoSelecionado)
                MessageBox.Show(String.Format("Ponto de Coleta '{0}' atualizado com sucesso.", pontoSelecionado.Nome),
                                "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                ' Caso contrário, é um novo cadastro
                Dim novoPonto As New PontoColeta()
                PreencherObjetoPontoColeta(novoPonto)
                DataAccess.SalvarNovoPontoColeta(novoPonto)
                MessageBox.Show(String.Format("Novo Ponto de Coleta '{0}' cadastrado com sucesso.", novoPonto.Nome),
                                "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)
            End If

            ' Recarrega a lista para atualizar a tela
            CarregarPontosDoBanco()

        Catch ex As Exception
            MessageBox.Show(String.Format("Erro ao salvar Ponto de Coleta: {0}", ex.Message),
                            "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' BOTÃO: Inativar ponto de coleta
    ' =============================================================
    Private Sub BtnInativar_Click(sender As Object, e As RoutedEventArgs) Handles btnInativar.Click
        ' Só funciona se houver algo selecionado na lista
        If pontoSelecionado IsNot Nothing Then
            ' Pede confirmação antes de excluir
            Dim msg = String.Format("Tem certeza que deseja inativar o ponto '{0}'?", pontoSelecionado.Nome)
            Dim resposta = MessageBox.Show(msg, "Confirmar Inativação", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            If resposta = MessageBoxResult.No Then Return

            Try
                ' Chama o DataAccess para inativar no banco
                DataAccess.InativarPontoColeta(pontoSelecionado.ID_PontoColeta)
                MessageBox.Show(String.Format("Ponto '{0}' inativado com sucesso.", pontoSelecionado.Nome),
                                "Inativação", MessageBoxButton.OK, MessageBoxImage.Information)

                ' Recarrega a lista atualizada
                CarregarPontosDoBanco()

            Catch ex As Exception
                MessageBox.Show(String.Format("Erro ao inativar Ponto de Coleta: {0}", ex.Message),
                                "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        Else
            MessageBox.Show("Selecione um ponto na lista para inativar.",
                            "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If
    End Sub


    ' =============================================================
    ' BOTÃO: Limpar formulário
    ' =============================================================
    Private Sub BtnLimpar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpar.Click
        LimparFormulario()
    End Sub


    ' =============================================================
    ' Quando o usuário seleciona um item da ListView
    ' =============================================================
    Private Sub ListViewPontos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ListViewPontos.SelectionChanged
        pontoSelecionado = CType(ListViewPontos.SelectedItem, PontoColeta)

        ' Se houver item selecionado, preenche os campos para edição
        If pontoSelecionado IsNot Nothing Then
            txtNome.Text = pontoSelecionado.Nome
            txtResponsavel.Text = pontoSelecionado.Responsavel
            txtTelefone.Text = pontoSelecionado.Telefone
            txtCEP.Text = pontoSelecionado.CEP
            txtRua.Text = pontoSelecionado.Rua
            txtNumero.Text = pontoSelecionado.Numero
            txtComplemento.Text = pontoSelecionado.Complemento
            txtBairro.Text = pontoSelecionado.Bairro
            txtCidade.Text = pontoSelecionado.Cidade
            txtUF.Text = pontoSelecionado.UF
        Else
            ' Caso contrário, limpa os campos
            LimparFormulario()
        End If
    End Sub


    ' =============================================================
    ' Limpa todos os campos de texto
    ' =============================================================
    Private Sub LimparFormulario()
        txtNome.Text = ""
        txtResponsavel.Text = ""
        txtTelefone.Text = ""
        txtCEP.Text = ""
        LimparCamposCEP()
        txtNumero.Text = ""
        txtComplemento.Text = ""
        ListViewPontos.SelectedItem = Nothing
        pontoSelecionado = Nothing
    End Sub

    ' Limpa apenas os campos de endereço (usado quando o CEP muda)
    Private Sub LimparCamposCEP()
        txtRua.Text = ""
        txtBairro.Text = ""
        txtCidade.Text = ""
        txtUF.Text = ""
    End Sub


    ' =============================================================
    ' Copia os valores digitados na tela para o objeto PontoColeta
    ' =============================================================
    Private Sub PreencherObjetoPontoColeta(ByRef p As PontoColeta)
        p.Nome = txtNome.Text.Trim()
        p.Responsavel = txtResponsavel.Text.Trim()
        p.Telefone = txtTelefone.Text.Trim()
        p.CEP = txtCEP.Text.Trim()
        p.Rua = txtRua.Text.Trim()
        p.Numero = txtNumero.Text.Trim()
        p.Complemento = txtComplemento.Text.Trim()
        p.Bairro = txtBairro.Text.Trim()
        p.Cidade = txtCidade.Text.Trim()
        p.UF = txtUF.Text.Trim()
    End Sub

End Class


' =============================================================
' CLASSE AUXILIAR: ViaCepResultPontoColeta
' -------------------------------------------------------------
' Essa classe representa o modelo de dados que vem da API ViaCEP.
' O JSON retornado pelo site é convertido (desserializado) em
' um objeto desse tipo para facilitar o uso no código.
' =============================================================
Public Class ViaCepResultPontoColeta

    <JsonProperty("cep")>
    Public Property Cep As String

    <JsonProperty("logradouro")>
    Public Property Logradouro As String

    <JsonProperty("complemento")>
    Public Property Complemento As String

    <JsonProperty("bairro")>
    Public Property Bairro As String

    <JsonProperty("localidade")>
    Public Property Localidade As String ' Cidade

    <JsonProperty("uf")>
    Public Property Uf As String

    <JsonProperty("ibge")>
    Public Property Ibge As String

    <JsonProperty("gia")>
    Public Property Gia As String

    <JsonProperty("ddd")>
    Public Property Ddd As String

    <JsonProperty("siafi")>
    Public Property Siafi As String
End Class
