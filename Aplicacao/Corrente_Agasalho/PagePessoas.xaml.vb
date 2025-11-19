' =============================================================
' ARQUIVO: PagePessoas.vb
' -------------------------------------------------------------
' OBJETIVO: Página de Cadastro de Pessoas. Gerencia a listagem,
' salvamento, inativação e a busca automática de endereços (ViaCEP).
' LINGUAGEM: Visual Basic .NET
' =============================================================

' Declara que o código precisa das ferramentas de rede (para a API)
Imports System.Net.Http
' Declara que o código usará uma biblioteca para trabalhar com JSON (dados da API)
Imports Newtonsoft.Json
Imports System.Globalization

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PagePessoas
' -------------------------------------------------------------
' É a tela principal que contém toda a lógica da interface de cadastro.
' =============================================================
Public Class PagePessoas

    ' HttpClient = ferramenta usada para acessar sites e APIs pela internet
    ' Shared ReadOnly: significa que é uma única instância (compartilhada) e não pode ser alterada.
    Private Shared ReadOnly httpClient As New HttpClient()

    ' Lista principal que guarda todos os objetos (Pessoas) carregados do banco de dados
    Private masterListPessoas As New List(Of Pessoa)()
    ' Variável que guarda a pessoa que está sendo vista ou editada no formulário
    Private pessoaSelecionada As Pessoa = Nothing

    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Ação que ocorre automaticamente assim que a página é carregada.
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Chama a função que busca os dados no banco e preenche a lista
        CarregarPessoasDoBanco()
    End Sub


    ' =============================================================
    ' FUNÇÃO: CarregarPessoasDoBanco
    ' -------------------------------------------------------------
    ' Busca todas as pessoas ativas no "banco de dados" (via DataAccess)
    ' e exibe na lista da tela.
    ' =============================================================
    Private Sub CarregarPessoasDoBanco()
        ' Bloco Try/Catch para tentar executar o código e capturar erros
        Try
            ' Acessa o módulo de dados e pega a lista de todas as pessoas
            masterListPessoas = DataAccess.GetTodasPessoasAtivas()
            ' Conecta a lista de pessoas (masterListPessoas) ao controle visual da lista
            ListViewPessoas.ItemsSource = masterListPessoas
            ' Força a lista visual (na tela) a se atualizar com os novos dados
            ListViewPessoas.Items.Refresh()
            ' Limpa todos os campos do formulário após carregar a lista
            LimparFormulario()
            ' Se der qualquer erro durante a busca no banco:
        Catch ex As Exception
            ' Mostra uma mensagem de erro na tela
            MessageBox.Show(String.Format("Erro fatal ao carregar pessoas: {0}", ex.Message),
              "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnBuscarCEP_Click
    ' -------------------------------------------------------------
    ' Usa a API do ViaCEP para preencher automaticamente os campos
    ' de endereço a partir do CEP digitado.
    ' O termo 'Async' indica que a função irá aguardar uma resposta
    ' da internet ('Await').
    ' =============================================================
    Private Async Sub BtnBuscarCEP_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscarCEP.Click
        ' Pega o CEP digitado, remove espaços no início/fim e retira o traço
        Dim cep As String = txtCEP.Text.Trim().Replace("-", "")

        ' Verifica se o CEP tem exatamente 8 caracteres E se são todos números
        If cep.Length <> 8 OrElse Not IsNumeric(cep) Then
            ' Se for inválido, mostra o erro e para a execução da função
            MessageBox.Show("CEP inválido. Digite apenas 8 números.", "Erro de Formato", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        ' Desabilita o botão para o usuário não clicar novamente enquanto espera
        btnBuscarCEP.IsEnabled = False
        ' Muda o texto do botão para indicar que está em processo
        btnBuscarCEP.Content = "Buscando..."

        ' Bloco Try/Catch para tratar possíveis erros de conexão com a API
        Try
            ' Monta o endereço (URL) completo da API com o CEP que o usuário digitou
            Dim url As String = String.Format("https://viacep.com.br/ws/{0}/json/", cep)
            ' Envia a requisição para a API e 'Await' (aguarda) a resposta
            Dim response As HttpResponseMessage = Await httpClient.GetAsync(url)

            ' Verifica se a resposta da API foi positiva (código 200, sem erros de rede)
            If response.IsSuccessStatusCode Then
                ' Lê o conteúdo da resposta, que é uma string no formato JSON
                Dim jsonString As String = Await response.Content.ReadAsStringAsync()

                ' Desserialização: Transforma a string JSON em um objeto (ViaCepResult)
                Dim resultadoCEP As ViaCepResult = JsonConvert.DeserializeObject(Of ViaCepResult)(jsonString)

                ' Verifica se o objeto foi criado E se o campo 'Logradouro' (rua) veio preenchido
                If resultadoCEP IsNot Nothing AndAlso resultadoCEP.Logradouro IsNot Nothing Then
                    ' Preenche os campos da tela com os dados que vieram do objeto
                    txtRua.Text = resultadoCEP.Logradouro
                    txtBairro.Text = resultadoCEP.Bairro
                    txtCidade.Text = resultadoCEP.Localidade
                    txtUF.Text = resultadoCEP.Uf
                    ' Coloca o foco no campo de número para o usuário continuar digitando
                    txtNumero.Focus()
                Else
                    ' Se não encontrou o CEP ou a rua veio vazia:
                    MessageBox.Show("CEP não encontrado ou inexistente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
                    ' Limpa os campos de endereço
                    LimparCamposCEP()
                End If
            Else
                ' Se a requisição HTTP falhou (ex: sem internet ou erro no servidor da API)
                MessageBox.Show("Não foi possível conectar à API ViaCEP.", "Erro de Rede", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
            ' Captura qualquer outro erro inesperado
        Catch ex As Exception
            MessageBox.Show(String.Format("Ocorreu um erro inesperado ao buscar o CEP: {0}", ex.Message), "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error)
            ' O bloco Finally SEMPRE é executado, dando erro ou não.
        Finally
            ' Volta a habilitar o botão
            btnBuscarCEP.IsEnabled = True
            ' Volta o texto original do botão
            btnBuscarCEP.Content = "Buscar CEP"
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnSalvar_Click
    ' -------------------------------------------------------------
    ' Salva uma nova pessoa ou atualiza a pessoa selecionada no banco.
    ' =============================================================
    Private Sub BtnSalvar_Click(sender As Object, e As RoutedEventArgs) Handles btnSalvar.Click
        ' Verifica se os campos Nome OU Documento estão vazios
        If String.IsNullOrWhiteSpace(txtNome.Text) OrElse String.IsNullOrWhiteSpace(txtDocumento.Text) Then
            ' Se estiverem, mostra aviso e para a execução da função
            MessageBox.Show("Nome e Documento são obrigatórios.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            ' Condição: Verifica se existe uma pessoa já selecionada na lista
            If pessoaSelecionada IsNot Nothing Then
                ' --- LÓGICA DE EDIÇÃO ---
                ' Preenche o objeto (pessoaSelecionada) com os dados novos do formulário
                PreencherObjetoPessoa(pessoaSelecionada)
                ' Chama a função que atualiza esse objeto no banco de dados
                DataAccess.AtualizarPessoa(pessoaSelecionada)
                MessageBox.Show(String.Format("Cadastro de '{0}' atualizado com sucesso.", pessoaSelecionada.Nome), "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                ' --- LÓGICA DE NOVO CADASTRO ---
                ' Cria um novo objeto (Pessoa) vazio
                Dim novaPessoa As New Pessoa()
                ' Preenche o novo objeto com os dados do formulário
                PreencherObjetoPessoa(novaPessoa)
                ' Chama a função que salva o novo objeto no banco de dados
                DataAccess.SalvarNovaPessoa(novaPessoa)
                MessageBox.Show(String.Format("Pessoa '{0}' cadastrada com sucesso.", novaPessoa.Nome), "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)
            End If
            ' Recarrega a lista para mostrar o item novo ou o item editado
            CarregarPessoasDoBanco()
        Catch ex As Exception
            ' Se der erro ao tentar salvar ou atualizar
            MessageBox.Show(String.Format("Erro ao salvar pessoa: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnExcluir_Click
    ' -------------------------------------------------------------
    ' Marca a pessoa selecionada como 'inativa' no banco de dados.
    ' (Não deleta, apenas desabilita).
    ' =============================================================
    Private Sub BtnExcluir_Click(sender As Object, e As RoutedEventArgs) Handles btnExcluir.Click
        ' Verifica se existe uma pessoa selecionada para inativar
        If pessoaSelecionada IsNot Nothing Then
            ' Monta a mensagem de confirmação
            Dim msg = String.Format("Tem certeza que deseja inativar o cadastro de '{0}'?", pessoaSelecionada.Nome)
            ' Pede confirmação ao usuário (sim ou não)
            Dim resposta = MessageBox.Show(msg, "Confirmar Inativação", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            ' Se o usuário clicar em "Não", a função é encerrada aqui
            If resposta = MessageBoxResult.No Then Return

            Try
                ' Chama a função que marca a pessoa como inativa usando o ID dela
                DataAccess.InativarPessoa(pessoaSelecionada.ID_Pessoa)
                MessageBox.Show(String.Format("Cadastro '{0}' inativado com sucesso.", pessoaSelecionada.Nome), "Inativação", MessageBoxButton.OK, MessageBoxImage.Information)
                ' Recarrega a lista, que agora não deve mostrar o item inativado
                CarregarPessoasDoBanco()
            Catch ex As Exception
                ' Trata o erro se houver falha na comunicação com o banco
                MessageBox.Show(String.Format("Erro ao inativar cadastro: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        Else
            ' Mensagem de aviso se não houver seleção
            MessageBox.Show("Selecione uma pessoa na lista para inativar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnLimpar_Click
    ' -------------------------------------------------------------
    ' Chama a função interna para limpar todos os campos do formulário.
    ' =============================================================
    Private Sub BtnLimpar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpar.Click
        ' Executa a limpeza
        LimparFormulario()
    End Sub

    ' =============================================================
    ' EVENTO: ListViewPessoas_SelectionChanged
    ' -------------------------------------------------------------
    ' Ocorre toda vez que o usuário clica ou troca o item selecionado na lista.
    ' Preenche o formulário com os dados da pessoa escolhida.
    ' =============================================================
    Private Sub ListViewPessoas_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ListViewPessoas.SelectionChanged
        ' Tenta converter o item selecionado em um objeto do tipo 'Pessoa'
        pessoaSelecionada = CType(ListViewPessoas.SelectedItem, Pessoa)

        ' Se realmente uma pessoa foi selecionada:
        If pessoaSelecionada IsNot Nothing Then
            ' Copia os dados do objeto Pessoa para os campos de texto da tela
            txtNome.Text = pessoaSelecionada.Nome
            txtDocumento.Text = pessoaSelecionada.Documento
            txtTelefone.Text = pessoaSelecionada.Telefone
            ' Preenche o checkbox com o valor de IsVulneravel
            chkEmVulnerabilidade.IsChecked = pessoaSelecionada.IsVulneravel
            txtCEP.Text = pessoaSelecionada.CEP
            txtRua.Text = pessoaSelecionada.Rua
            txtNumero.Text = pessoaSelecionada.Numero
            txtComplemento.Text = pessoaSelecionada.Complemento
            txtBairro.Text = pessoaSelecionada.Bairro
            txtCidade.Text = pessoaSelecionada.Cidade
            txtUF.Text = pessoaSelecionada.UF

            ' --- INÍCIO DA CORREÇÃO DE CARREGAMENTO DE HISTÓRICO ---
            Try
                ' Busca o histórico de transações desta pessoa específica
                Dim historico = DataAccess.GetTransacoesPorPessoa(pessoaSelecionada.ID_Pessoa)
                ' Preenche a lista de histórico (ListViewHistorico) com esses dados
                ListViewHistorico.ItemsSource = historico
            Catch ex As Exception
                MessageBox.Show("Erro ao carregar histórico: " & ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error)
                ' Se der erro, garante que a lista de histórico esteja vazia
                ListViewHistorico.ItemsSource = Nothing
            End Try
            ' --- FIM DA CORREÇÃO ---

        Else
            ' Se não há seleção (o usuário desmarcou ou o programa limpou)
            LimparFormulario()
        End If
    End Sub

    ' =============================================================
    ' FUNÇÃO: LimparFormulario
    ' -------------------------------------------------------------
    ' Zera todos os campos do formulário de cadastro.
    ' =============================================================
    Private Sub LimparFormulario()
        ' Limpa campos de texto
        txtNome.Text = ""
        txtDocumento.Text = ""
        txtTelefone.Text = ""
        ' Desmarca o checkbox
        chkEmVulnerabilidade.IsChecked = False
        txtCEP.Text = ""
        ' Chama a função para limpar os campos de endereço
        LimparCamposCEP()
        txtNumero.Text = ""
        txtComplemento.Text = ""
        ' Desmarca o item selecionado na lista (visual)
        ListViewPessoas.SelectedItem = Nothing
        ' Zera o objeto (lógico) da pessoa selecionada
        pessoaSelecionada = Nothing

        ' --- INÍCIO DA CORREÇÃO ---
        ' Garante que a lista de histórico também seja limpa
        ListViewHistorico.ItemsSource = Nothing
        ' --- FIM DA CORREÇÃO ---
    End Sub

    ' =============================================================
    ' FUNÇÃO: LimparCamposCEP
    ' -------------------------------------------------------------
    ' Função dedicada para limpar apenas os campos de endereço
    ' preenchidos automaticamente.
    ' =============================================================
    Private Sub LimparCamposCEP()
        txtRua.Text = ""
        txtBairro.Text = ""
        txtCidade.Text = ""
        txtUF.Text = ""
    End Sub

    ' =============================================================
    ' FUNÇÃO: PreencherObjetoPessoa
    ' -------------------------------------------------------------
    ' Pega os valores dos campos do formulário e guarda dentro de
    ' um objeto do tipo 'Pessoa'.
    ' O 'ByRef' significa que está modificando o objeto original.
    ' =============================================================
    Private Sub PreencherObjetoPessoa(ByRef p As Pessoa)
        ' Pega o texto, remove espaços extras e guarda no objeto 'p'
        p.Nome = txtNome.Text.Trim()
        p.Documento = txtDocumento.Text.Trim()
        p.Telefone = txtTelefone.Text.Trim()
        ' Pega o estado do checkbox (marcado ou desmarcado)
        p.IsVulneravel = chkEmVulnerabilidade.IsChecked.GetValueOrDefault(False)
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
' CLASSE: ViaCepResult
' -------------------------------------------------------------
' É o "molde" (estrutura) que define como o código deve receber
' e interpretar os dados JSON que vêm da API ViaCEP.
' =============================================================
Public Class ViaCepResult
    ' [JsonProperty("cep")] diz que este campo (Propriedade) deve
    ' ser preenchido com o valor do campo "cep" que vem no JSON.
    <JsonProperty("cep")>
    Public Property Cep As String

    <JsonProperty("logradouro")>
    Public Property Logradouro As String

    <JsonProperty("complemento")>
    Public Property Complemento As String

    <JsonProperty("bairro")>
    Public Property Bairro As String

    <JsonProperty("localidade")>
    Public Property Localidade As String ' Nome da Cidade

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

' =============================================================
' RESUMO: INTEGRAÇÃO COM A API VIACEP
' -------------------------------------------------------------
' 1. O usuário digita o CEP e clica em "Buscar".
' 2. O código retira o traço (-) e verifica se o CEP tem 8 números.
' 3. O sistema usa o HttpClient (ferramenta de rede) para enviar uma
'    requisição para a API pública ViaCEP (Ex: viacep.com.br/ws/01001000/json/).
' 4. A API responde com um texto JSON (dados estruturados).
' 5. O código usa a biblioteca Newtonsoft.Json para converter (desserializar)
'    esse JSON em um objeto ViaCepResult.
' 6. Os campos da tela (Rua, Bairro, Cidade, UF) são preenchidos
'    automaticamente com os dados desse objeto.
' =============================================================