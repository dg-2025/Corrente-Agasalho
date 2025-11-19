' =============================================================
' ARQUIVO: Sessao.vb
' -------------------------------------------------------------
' Este módulo guarda as informações do usuário que está logado.
' Ele mantém o nome e o tipo de acesso (ex: administrador ou comum)
' enquanto o sistema estiver aberto.
' =============================================================
Public Module Sessao

    ' Armazena o nome do usuário que fez login
    Public Property UsuarioLogado As String

    ' Armazena o tipo de acesso do usuário (ex: "Administrador", "Comum", etc.)
    Public Property UsuarioTipo As String

    ' Essa função verifica se o usuário logado é um administrador
    ' Retorna "True" se for administrador e "False" se não for
    Public Function EhAdmin() As Boolean
        ' Só retorna verdadeiro se o tipo de usuário não for nulo e for igual a "administrador"
        Return UsuarioTipo IsNot Nothing AndAlso UsuarioTipo.ToLower() = "administrador"
    End Function

End Module
