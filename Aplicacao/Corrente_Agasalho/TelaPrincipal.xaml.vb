' Imports necessários para lidar com janelas e eventos do mouse
Imports System.Windows
Imports System.Windows.Input

' =============================================================
' ARQUIVO: TelaPrincipal.xaml.vb
' -------------------------------------------------------------
' Essa classe representa a janela principal do sistema.
' Ela controla a navegação entre as páginas, os botões da barra
' superior (minimizar, maximizar, fechar) e o botão de sair.
' =============================================================
Public Class TelaPrincipal

    ' Essa função permite arrastar a janela pela tela
    ' É chamada quando o usuário segura o botão esquerdo do mouse
    Private Sub Window_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        ' Se o botão esquerdo estiver pressionado, a janela pode ser arrastada
        If e.ButtonState = MouseButtonState.Pressed Then
            DragMove()
        End If
    End Sub

    ' Essa função executa o logout do sistema
    ' Quando o usuário clica no botão "Sair", a tela de login é aberta novamente
    Private Sub btnSair_Click(sender As Object, e As RoutedEventArgs)
        Dim login As New TelaLogin()
        login.Show()
        Me.Close() ' Fecha a tela principal
    End Sub

    ' Essa função controla a navegação entre as páginas do sistema
    ' Cada botão no menu lateral abre uma página diferente dentro do Frame principal
    Private Sub btnNavegacao_Click(sender As Object, e As RoutedEventArgs)
        Dim botao = CType(sender, RadioButton)

        ' Tenta abrir a página correspondente ao botão clicado
        Try
            Select Case botao.Name
                Case "btnDashboard"
                    MainFrame.Navigate(New Uri("PageDashboard.xaml", UriKind.Relative))

                Case "btnPessoas"
                    MainFrame.Navigate(New Uri("PagePessoas.xaml", UriKind.Relative))

                Case "btnVoluntarios"
                    MainFrame.Navigate(New Uri("PageVoluntarios.xaml", UriKind.Relative))

                Case "btnEntrada"
                    MainFrame.Navigate(New Uri("PageEntradaDoacao.xaml", UriKind.Relative))

                Case "btnSaida"
                    MainFrame.Navigate(New Uri("PageSaidaEntrega.xaml", UriKind.Relative))

                Case "btnInventario"
                    MainFrame.Navigate(New Uri("PageInventario.xaml", UriKind.Relative))

                Case "btnPontosColeta"
                    MainFrame.Navigate(New Uri("PagePontosColeta.xaml", UriKind.Relative))

                Case "btnUsuarios"
                    MainFrame.Navigate(New Uri("PageUsuarios.xaml", UriKind.Relative))

                Case "btnParametros"
                    MainFrame.Navigate(New Uri("PageParametros.xaml", UriKind.Relative))

                Case "btnAuditoria"
                    MainFrame.Navigate(New Uri("PageAuditoria.xaml", UriKind.Relative))
            End Select

            ' Caso ocorra algum erro (por exemplo, a página não existe), mostra uma mensagem
        Catch ex As Exception
            MessageBox.Show($"Erro ao navegar: Não foi possível encontrar a página '{botao.Name.Replace("btn", "Page")}.xaml'.{vbCrLf}{ex.Message}")
        End Try
    End Sub

    ' Essa função minimiza a janela principal
    Private Sub btnMinimizar_Click(sender As Object, e As RoutedEventArgs)
        WindowState = WindowState.Minimized
    End Sub

    ' Essa função alterna entre modo maximizado e tamanho normal da janela
    Private Sub btnMaximizar_Click(sender As Object, e As RoutedEventArgs)
        ' Se a janela já estiver maximizada, volta ao tamanho normal
        ' Caso contrário, maximiza a janela
        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal
        Else
            WindowState = WindowState.Maximized
        End If
    End Sub

    ' Essa função fecha completamente o sistema
    Private Sub btnFechar_Click(sender As Object, e As RoutedEventArgs)
        Application.Current.Shutdown()
    End Sub

End Class
