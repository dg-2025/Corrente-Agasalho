' Importa funções para capturar ações do mouse
Imports System.Windows.Input
' Importa classes de segurança e criptografia para tratar senhas
Imports System.Security.Cryptography
' Importa ferramentas para trabalhar com textos e caracteres
Imports System.Text

' =============================================================
' ARQUIVO: TelaLogin.xaml.vb
' -------------------------------------------------------------
' Essa classe representa a tela de login do sistema.
' Ela valida o usuário e a senha, faz a verificação no banco
' e abre a tela principal quando o login é bem-sucedido.
' =============================================================
Public Class TelaLogin
    Inherits Window

    ' Construtor padrão da janela
    ' Serve para carregar os elementos definidos no arquivo XAML
    Public Sub New()
        InitializeComponent()
    End Sub

    ' Permite que o usuário arraste a janela segurando o botão esquerdo do mouse
    Private Sub Window_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        ' Verifica se o botão esquerdo do mouse está pressionado
        If e.ButtonState = MouseButtonState.Pressed Then
            DragMove() ' Move a janela conforme o movimento do mouse
        End If
    End Sub

    ' Fecha completamente o programa quando o botão "Fechar" é clicado
    Private Sub BtnFechar_Click(sender As Object, e As RoutedEventArgs) Handles btnFechar.Click
        Application.Current.Shutdown()
    End Sub

    ' Essa função é chamada quando o usuário clica no botão "Entrar"
    ' Ela faz a validação do login, verificando se o usuário existe e se a senha está correta
    Private Sub Botao_entrar_Click(sender As Object, e As RoutedEventArgs) Handles botao_entrar.Click
        ' Esconde a mensagem de erro, caso esteja visível
        txtMensagemErro.Visibility = Visibility.Collapsed

        ' Captura o texto digitado nos campos de usuário e senha
        Dim usuario As String = entrada_usuario.Text.Trim()
        Dim senha As String = entrada_senha.Password.Trim()

        ' Verifica se os campos foram preenchidos
        If String.IsNullOrWhiteSpace(usuario) OrElse String.IsNullOrWhiteSpace(senha) Then
            txtMensagemErro.Text = "Usuário e senha são obrigatórios."
            txtMensagemErro.Visibility = Visibility.Visible
            Return ' Sai da função, pois os campos estão vazios
        End If

        Try
            ' Busca o usuário digitado no banco de dados
            Dim usuarioDoBanco As Usuario = DataAccess.GetUsuarioPorLogin(usuario)

            ' Se o usuário não existir no banco, exibe erro de login
            If usuarioDoBanco Is Nothing Then
                ExibirErroLogin()
                Return
            End If

            ' Gera o hash da senha digitada para comparar com o hash salvo no banco
            Dim hashSenhaDigitada As String = DataAccess.GerarHash(senha)

            ' Compara o hash digitado com o hash armazenado (sem diferença de maiúsculas e minúsculas)
            If usuarioDoBanco.SenhaHash.ToLower() = hashSenhaDigitada.ToLower() Then
                ' Login válido — usuário e senha conferem

                ' Salva informações na sessão atual (quem está logado e o tipo de acesso)
                Sessao.UsuarioLogado = usuarioDoBanco.Nome
                Sessao.UsuarioTipo = usuarioDoBanco.NivelAcesso

                ' Abre a tela principal e fecha a tela de login
                Dim telaPrincipal As New TelaPrincipal()
                Application.Current.MainWindow = telaPrincipal
                telaPrincipal.Show()
                Me.Close()
            Else
                ' Se as senhas não conferirem, mostra mensagem de erro
                ExibirErroLogin()
            End If

            ' Caso ocorra algum erro ao acessar o banco, exibe mensagem detalhada
        Catch ex As Exception
            MessageBox.Show($"Erro ao conectar ao banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' Mostra uma mensagem de erro quando o login falha
    ' É usada quando o usuário não existe ou a senha está errada
    Private Sub ExibirErroLogin()
        txtMensagemErro.Text = "Usuário ou senha inválidos."
        txtMensagemErro.Visibility = Visibility.Visible
    End Sub

End Class
