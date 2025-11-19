' Importa funções usadas para criar o hash da senha (criptografia)
Imports System.Security.Cryptography
' Importa funções para manipular textos (como converter em bytes para gerar o hash)
Imports System.Text

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PageUsuarios
' -------------------------------------------------------------
' Essa classe controla a tela onde os administradores gerenciam
' os usuários do sistema (criar, editar, inativar, etc).
' =============================================================
Public Class PageUsuarios

    ' Lista principal que guarda todos os usuários trazidos do banco de dados
    Private masterListUsuarios As New List(Of Usuario)()

    ' Guarda o usuário atualmente selecionado na lista para edição
    Private usuarioSelecionado As Usuario = Nothing

    ' Essa função é executada automaticamente quando a página é carregada
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Verifica se o usuário logado é administrador
        If Not Sessao.EhAdmin() Then
            ' Se não for, mostra aviso e bloqueia o acesso à tela
            MessageBox.Show("Acesso restrito a administradores.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning)
            Me.IsEnabled = False
            ' Sai do método sem executar o restante
        End If

        ' Preenche o ComboBox com os níveis de acesso possíveis
        cmbNivelAcesso.ItemsSource = New List(Of String) From {"Funcionário", "Administrador"}

        ' Chama o método que busca os usuários no banco de dados
        CarregarUsuariosDoBanco()
    End Sub


    ' -------------------------------------------------------------
    ' Essa função busca os usuários diretamente no banco de dados
    ' através da classe DataAccess e atualiza a lista da interface
    ' -------------------------------------------------------------
    Private Sub CarregarUsuariosDoBanco()
        Try
            ' Pega todos os usuários ativos do banco e joga na lista principal
            masterListUsuarios = DataAccess.GetTodosUsuariosAtivos()

            ' Liga essa lista à ListView que mostra os usuários na tela
            ListViewUsuarios.ItemsSource = masterListUsuarios

            ' Atualiza a exibição (importante se a lista já tinha dados antes)
            ListViewUsuarios.Items.Refresh()

            ' Limpa o formulário (remove texto dos campos e reseta seleção)
            LimparFormulario()

        Catch ex As Exception
            ' Caso ocorra erro (como falha de conexão), mostra uma mensagem explicando
            MessageBox.Show(String.Format("Erro fatal ao carregar usuários: {0}{1}{1}Verifique a 'DefaultConnection' no seu App.config e se o PostgreSQL está rodando.", ex.Message, vbCrLf),
                            "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' -------------------------------------------------------------
    ' Essa função salva um novo usuário ou atualiza um existente.
    ' -------------------------------------------------------------
    Private Sub BtnSalvar_Click(sender As Object, e As RoutedEventArgs) Handles btnSalvar.Click
        ' Verifica se o nome e o nível de acesso foram preenchidos
        If String.IsNullOrWhiteSpace(txtNomeLogin.Text) OrElse cmbNivelAcesso.SelectedItem Is Nothing Then
            MessageBox.Show("Nome de usuário e Nível de Acesso são obrigatórios.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            ' Pega o nome digitado e remove espaços desnecessários
            Dim nomeLogin = txtNomeLogin.Text.Trim()

            ' Busca no banco para ver se já existe um usuário com o mesmo nome
            Dim usuarioExistente = DataAccess.GetUsuarioPorLogin(nomeLogin)

            ' Se estamos editando e o usuário selecionado é o mesmo que já existe, ignora a verificação
            If usuarioSelecionado IsNot Nothing AndAlso usuarioExistente IsNot Nothing AndAlso usuarioSelecionado.ID_Usuario = usuarioExistente.ID_Usuario Then
                usuarioExistente = Nothing
            End If

            ' Se encontrou outro usuário com o mesmo login, mostra aviso e cancela o salvamento
            If usuarioExistente IsNot Nothing Then
                MessageBox.Show(String.Format("O nome de login '{0}' já está sendo usado por outro usuário.", nomeLogin), "Login Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If


            ' =============================================================
            ' Se um usuário já estiver selecionado, significa que é uma edição
            ' =============================================================
            If usuarioSelecionado IsNot Nothing Then

                ' Atualiza apenas o nível de acesso
                usuarioSelecionado.NivelAcesso = cmbNivelAcesso.SelectedItem.ToString()

                ' Verifica se foi digitada uma nova senha
                If Not String.IsNullOrWhiteSpace(txtSenha.Password) Then
                    ' Se sim, gera o hash (criptografia) da nova senha
                    usuarioSelecionado.SenhaHash = DataAccess.GerarHash(txtSenha.Password)
                    MessageBox.Show("Senha alterada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information)
                End If

                ' Salva as alterações no banco
                DataAccess.AtualizarUsuario(usuarioSelecionado)
                MessageBox.Show(String.Format("Usuário '{0}' atualizado com sucesso.", usuarioSelecionado.Nome), "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)

            Else
                ' =============================================================
                ' Caso contrário, é um novo cadastro
                ' =============================================================

                ' Verifica se a senha foi preenchida (nova conta precisa de senha)
                If String.IsNullOrWhiteSpace(txtSenha.Password) Then
                    MessageBox.Show("A Senha é obrigatória para criar um novo usuário.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If

                ' Cria o novo usuário com os dados digitados
                Dim novoUsuario As New Usuario With {
                    .Nome = nomeLogin,
                    .NivelAcesso = cmbNivelAcesso.SelectedItem.ToString(),
                    .SenhaHash = DataAccess.GerarHash(txtSenha.Password)
                }

                ' Salva o novo usuário no banco
                DataAccess.SalvarNovoUsuario(novoUsuario)
                MessageBox.Show(String.Format("Novo usuário '{0}' criado com sucesso.", novoUsuario.Nome), "Salvo", MessageBoxButton.OK, MessageBoxImage.Information)
            End If

            ' Recarrega a lista de usuários para refletir as alterações
            CarregarUsuariosDoBanco()

        Catch ex As Exception
            ' Caso ocorra erro (como falha de conexão ou SQL), mostra a mensagem
            MessageBox.Show(String.Format("Erro ao salvar usuário: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' -------------------------------------------------------------
    ' Essa função inativa (desativa) um usuário selecionado na lista
    ' -------------------------------------------------------------
    Private Sub BtnInativar_Click(sender As Object, e As RoutedEventArgs) Handles btnInativar.Click
        ' Só pode inativar se houver um usuário selecionado
        If usuarioSelecionado IsNot Nothing Then

            ' Impede que o usuário "admin" principal seja desativado
            If usuarioSelecionado.Nome.ToLower() = "admin" Then
                MessageBox.Show("Não é possível inativar o usuário 'admin' principal.", "Ação Bloqueada", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            ' Exibe uma confirmação antes de inativar
            Dim msg = String.Format("Tem certeza que deseja inativar o usuário '{0}'?", usuarioSelecionado.Nome)
            Dim resposta = MessageBox.Show(msg, "Confirmar Inativação", MessageBoxButton.YesNo, MessageBoxImage.Warning)

            ' Se o usuário clicar "Não", cancela a ação
            If resposta = MessageBoxResult.No Then Return

            Try
                ' Chama o método do banco que inativa o usuário
                DataAccess.InativarUsuario(usuarioSelecionado.ID_Usuario)

                ' Mostra mensagem de sucesso
                MessageBox.Show(String.Format("Usuário '{0}' inativado com sucesso.", usuarioSelecionado.Nome), "Inativação", MessageBoxButton.OK, MessageBoxImage.Information)

                ' Recarrega a lista atualizada
                CarregarUsuariosDoBanco()

            Catch ex As Exception
                MessageBox.Show(String.Format("Erro ao inativar usuário: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        Else
            ' Caso nenhum usuário tenha sido selecionado na lista
            MessageBox.Show("Selecione um usuário na lista para inativar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If
    End Sub


    ' -------------------------------------------------------------
    ' Botão que apenas limpa o formulário
    ' -------------------------------------------------------------
    Private Sub BtnLimpar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpar.Click
        LimparFormulario()
    End Sub


    ' -------------------------------------------------------------
    ' Limpa os campos de texto e a seleção atual
    ' -------------------------------------------------------------
    Private Sub LimparFormulario()
        txtNomeLogin.Text = ""
        txtSenha.Password = ""
        cmbNivelAcesso.SelectedItem = Nothing
        ListViewUsuarios.SelectedItem = Nothing
        usuarioSelecionado = Nothing
    End Sub


    ' -------------------------------------------------------------
    ' Atualiza o formulário quando o usuário seleciona alguém da lista
    ' -------------------------------------------------------------
    Private Sub ListViewUsuarios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ListViewUsuarios.SelectionChanged
        ' Captura o item selecionado na ListView e converte para o tipo "Usuario"
        usuarioSelecionado = CType(ListViewUsuarios.SelectedItem, Usuario)

        ' Se houver um usuário selecionado, preenche os campos do formulário
        If usuarioSelecionado IsNot Nothing Then
            txtNomeLogin.Text = usuarioSelecionado.Nome
            cmbNivelAcesso.SelectedItem = usuarioSelecionado.NivelAcesso
        Else
            ' Caso contrário, limpa tudo
            LimparFormulario()
        End If
    End Sub

End Class
