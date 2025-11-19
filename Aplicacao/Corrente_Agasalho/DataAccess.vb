' =============================================================
' A "PONTE" (CAMADA DE ACESSO A DADOS)
' ARQUIVO COMPLETO E CORRIGIDO
' =============================================================

Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Configuration
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows
Imports System.Data
Imports Npgsql

' =============================================================
' 1. CLASSES MODELO (A "Linguagem" do nosso App)
' =============================================================
Public Class Usuario
    Public Property ID_Usuario As Integer
    Public Property Nome As String
    Public Property NivelAcesso As String
    Public Property SenhaHash As String
    Public Property IsAtivo As Boolean
    Public ReadOnly Property Icone As String
        Get
            ' --- CORRIGIDO: string.Equals ---
            If Nome IsNot Nothing AndAlso String.Equals(NivelAcesso, "administrador", StringComparison.OrdinalIgnoreCase) Then
                Return ChrW(&HE7EF).ToString() ' Ícone Admin
            Else
                Return ChrW(&HE716).ToString() ' Ícone Funcionário
            End If
        End Get
    End Property
End Class

Public Class Pessoa
    Public Property ID_Pessoa As Integer
    Public Property Nome As String
    Public Property Documento As String
    Public Property Telefone As String
    Public Property SaldoPontos As Integer
    Public Property IsVulneravel As Boolean
    Public Property CEP As String
    Public Property Rua As String
    Public Property Numero As String
    Public Property Complemento As String
    Public Property Bairro As String
    Public Property Cidade As String
    Public Property UF As String
    Public Property IsAtivo As Boolean

    ' --- (NOVO) CAMPO PARA O DASHBOARD DE PESSOAS ---
    Public Property UltimaMovimentacao As String = "-"

    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class

Public Class PontoColeta
    Public Property ID_PontoColeta As Integer
    Public Property Nome As String
    Public Property Responsavel As String
    Public Property Telefone As String
    Public Property CEP As String
    Public Property Rua As String
    Public Property Numero As String
    Public Property Complemento As String
    Public Property Bairro As String
    Public Property Cidade As String
    Public Property UF As String
    Public Property IsAtivo As Boolean
    Public Overrides Function ToString() As String
        Return Nome
    End Function
    Public ReadOnly Property EnderecoResumido As String
        Get
            Return String.Format("{0}, {1} - {2}", Rua, Numero, Bairro)
        End Get
    End Property
End Class

Public Class ParamCategoria
    Public Property ID_Categoria As Integer
    Public Property Nome As String
    Public Property PontosDoacao As Integer
    Public Property PontosRetirada As Integer
    Public Property IsEssencial As Boolean
    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class

Public Class ParamTamanho
    Public Property ID_Tamanho As Integer
    Public Property Nome As String
    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class

Public Class ParamCondicao
    Public Property ID_Condicao As Integer
    Public Property Nome As String
    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class

Public Class ParamStatusTriagem
    Public Property ID_Status As Integer
    Public Property Nome As String
    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class

' (ESTA CLASSE ESTÁ DENTRO DO DataAccess.vb)
Public Class ItemDoacao
    Implements INotifyPropertyChanged

    ' Propriedade Categoria (com gatilho para recalcular pontos)
    Private _categoria As ParamCategoria
    Public Property Categoria As ParamCategoria
        Get
            Return _categoria
        End Get
        Set(value As ParamCategoria)
            _categoria = value
            NotifyPropertyChanged(NameOf(Categoria))
            NotifyPropertyChanged(NameOf(Pontos)) ' Atualiza os pontos
        End Set
    End Property

    Public Property Tamanho As ParamTamanho
    Public Property Condicao As ParamCondicao
    Public Property Destino As ParamStatusTriagem

    ' Propriedade Quantidade (com gatilho para recalcular pontos)
    Private _quantidade As Integer = 1 ' Valor Padrão
    Public Property Quantidade As Integer
        Get
            Return _quantidade
        End Get
        Set(value As Integer)
            _quantidade = value
            NotifyPropertyChanged(NameOf(Quantidade))
            NotifyPropertyChanged(NameOf(Pontos)) ' Atualiza os pontos
        End Set
    End Property

    ' Propriedade Pontos (Agora calcula com base na Quantidade)
    Public ReadOnly Property Pontos As Integer
        Get
            If Categoria IsNot Nothing Then
                ' LÓGICA ATUALIZADA: Pontos = Qtd * Pontos da Categoria
                Return Quantidade * Categoria.PontosDoacao
            Else
                Return 0
            End If
        End Get
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private Sub NotifyPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class

Public Class ItemDisponivel
    Public Property ID_Item As Integer
    Public Property Nome As String
    Public Property Tamanho As String
    Public Property CustoPontos As Integer
    Public Property IsEssencial As Boolean
    Public Property QuantidadeEmEstoque As Integer ' <-- NOVO CAMPO

    Public Overrides Function ToString() As String
        ' Atualizado para mostrar a quantidade
        Return String.Format("{0} (Tam: {1}) - {2} Pts [Qtd: {3}]", Nome, Tamanho, CustoPontos, QuantidadeEmEstoque)
    End Function
End Class

Public Class ItemInventario
    Public Property Categoria As String
    Public Property Tamanho As String
    Public Property Condicao As String
    Public Property Status As String
    Public Property DataEntrada As DateTime
    Public Property Pontos As Integer ' (Custo de Retirada)
    Public Property IsEssencial As Boolean

    ' --- NOVOS CAMPOS ---
    Public Property QuantidadeDoada As Integer
    Public Property QuantidadeEmEstoque As Integer
    ' --- FIM NOVOS CAMPOS ---

    Public ReadOnly Property IsEssencialVisibility As Visibility
        Get
            Return If(IsEssencial, Visibility.Visible, Visibility.Collapsed)
        End Get
    End Property
End Class

Public Class TransacaoAuditoria
    Public Property ID_Transacao As Integer
    Public Property Tipo As String
    Public Property DataTransacao As DateTime
    Public Property Pessoa As String
    Public Property Descricao As String
    Public Property Pontos As Integer
End Class


' =============================================================
' 2. CLASSE DE ACESSO: DataAccess (FINAL)
' =============================================================
Public Class DataAccess

#Region "Conexão (GetConnection)"
    Private Shared Function GetConnectionString() As String
        Dim connString As ConnectionStringSettings = ConfigurationManager.ConnectionStrings("DefaultConnection")
        If connString IsNot Nothing Then
            Return connString.ConnectionString
        Else
            Throw New ConfigurationErrorsException("Não foi possível encontrar a 'DefaultConnection' no arquivo App.config.")
        End If
    End Function

    Public Shared Function GetConnection() As NpgsqlConnection
        Dim conn As New NpgsqlConnection(GetConnectionString())
        conn.Open()
        Return conn
    End Function
#End Region

#Region "Lógica de Autenticação (Hashing) (RF05, RNF05)"
    Public Shared Function GerarHash(senha As String) As String
        Dim bytes As Byte() = SHA256.HashData(Encoding.UTF8.GetBytes(senha))
        Dim builder As New StringBuilder()
        For i As Integer = 0 To bytes.Length - 1
            builder.Append(bytes(i).ToString("x2"))
        Next
        Return builder.ToString()
    End Function
#End Region

#Region "Lógica de Usuários (RF05)"
    Public Shared Function GetTodosUsuariosAtivos() As List(Of Usuario)
        Dim lista As New List(Of Usuario)
        Dim sql As String = "SELECT * FROM usuarios WHERE is_ativo = TRUE ORDER BY nome_login"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New Usuario With {
                            .ID_Usuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome_login")),
                            .SenhaHash = reader.GetString(reader.GetOrdinal("senha_hash")),
                            .NivelAcesso = reader.GetString(reader.GetOrdinal("nivel_acesso")),
                            .IsAtivo = reader.GetBoolean(reader.GetOrdinal("is_ativo"))
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Public Shared Function GetUsuarioPorLogin(nomeLogin As String) As Usuario
        Dim user As Usuario = Nothing
        Dim sql As String = "SELECT * FROM usuarios WHERE nome_login = @NomeLogin"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@NomeLogin", nomeLogin)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        user = New Usuario With {
                            .ID_Usuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome_login")),
                            .SenhaHash = reader.GetString(reader.GetOrdinal("senha_hash")),
                            .NivelAcesso = reader.GetString(reader.GetOrdinal("nivel_acesso")),
                            .IsAtivo = reader.GetBoolean(reader.GetOrdinal("is_ativo"))
                        }
                    End If
                End Using
            End Using
        End Using
        Return user
    End Function
    Public Shared Sub SalvarNovoUsuario(user As Usuario)
        Dim sql As String = "INSERT INTO usuarios (nome_login, senha_hash, nivel_acesso, is_ativo) VALUES (@Nome, @Hash, @Nivel, TRUE)"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@Nome", user.Nome)
                cmd.Parameters.AddWithValue("@Hash", user.SenhaHash)
                cmd.Parameters.AddWithValue("@Nivel", user.NivelAcesso)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub AtualizarUsuario(user As Usuario)
        Dim sql As String = "UPDATE usuarios SET nivel_acesso = @Nivel, senha_hash = @Hash WHERE id_usuario = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@Nivel", user.NivelAcesso)
                cmd.Parameters.AddWithValue("@Hash", user.SenhaHash)
                cmd.Parameters.AddWithValue("@ID", user.ID_Usuario)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub InativarUsuario(idUsuario As Integer)
        Dim sql As String = "UPDATE usuarios SET is_ativo = FALSE WHERE id_usuario = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@ID", idUsuario)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
#End Region

#Region "Lógica de Pessoas (RF06)"
    Private Shared Function ReaderParaPessoa(reader As NpgsqlDataReader) As Pessoa
        Dim p As New Pessoa With {
        .ID_Pessoa = reader.GetInt32(reader.GetOrdinal("id_pessoa")),
        .Nome = reader.GetString(reader.GetOrdinal("nome")),
        .Documento = If(reader.IsDBNull(reader.GetOrdinal("documento")), "", reader.GetString(reader.GetOrdinal("documento"))),
        .Telefone = If(reader.IsDBNull(reader.GetOrdinal("telefone")), "", reader.GetString(reader.GetOrdinal("telefone"))),
        .SaldoPontos = reader.GetInt32(reader.GetOrdinal("saldo_pontos")),
        .IsVulneravel = reader.GetBoolean(reader.GetOrdinal("is_vulneravel")),
        .CEP = If(reader.IsDBNull(reader.GetOrdinal("cep")), "", reader.GetString(reader.GetOrdinal("cep"))),
        .Rua = If(reader.IsDBNull(reader.GetOrdinal("rua")), "", reader.GetString(reader.GetOrdinal("rua"))),
        .Numero = If(reader.IsDBNull(reader.GetOrdinal("numero")), "", reader.GetString(reader.GetOrdinal("numero"))),
        .Complemento = If(reader.IsDBNull(reader.GetOrdinal("complemento")), "", reader.GetString(reader.GetOrdinal("complemento"))),
        .Bairro = If(reader.IsDBNull(reader.GetOrdinal("bairro")), "", reader.GetString(reader.GetOrdinal("bairro"))),
        .Cidade = If(reader.IsDBNull(reader.GetOrdinal("cidade")), "", reader.GetString(reader.GetOrdinal("cidade"))),
        .UF = If(reader.IsDBNull(reader.GetOrdinal("uf")), "", reader.GetString(reader.GetOrdinal("uf"))),
        .IsAtivo = reader.GetBoolean(reader.GetOrdinal("is_ativo"))
    }

        ' (NOVO) Lógica para buscar a data da última movimentação
        If HasColumn(reader, "ultima_data") AndAlso Not reader.IsDBNull(reader.GetOrdinal("ultima_data")) Then
            p.UltimaMovimentacao = reader.GetDateTime(reader.GetOrdinal("ultima_data")).ToString("dd/MM/yyyy")
        End If

        Return p
    End Function

    Public Shared Function GetTodasPessoasAtivas() As List(Of Pessoa)
        Dim lista As New List(Of Pessoa)

        ' SQL ATUALIZADO (V2.0) - Busca a última movimentação
        Dim sql As String =
        "WITH UltimaAtividade AS (" &
        "    SELECT id_pessoa, MAX(data_doacao) AS ultima_data FROM doacoes WHERE status_transacao = 'Ativo' GROUP BY id_pessoa " &
        "    UNION ALL " &
        "    SELECT id_pessoa, MAX(data_entrega) AS ultima_data FROM entregas WHERE status_transacao = 'Ativo' GROUP BY id_pessoa " &
        "), " &
        "PessoaUltimaAtividade AS (" &
        "    SELECT id_pessoa, MAX(ultima_data) AS ultima_data " &
        "    FROM UltimaAtividade " &
        "    GROUP BY id_pessoa " &
        ") " &
        "SELECT p.*, pua.ultima_data " &
        "FROM pessoas p " &
        "LEFT JOIN PessoaUltimaAtividade pua ON p.id_pessoa = pua.id_pessoa " &
        "WHERE p.is_ativo = TRUE " &
        "ORDER BY p.nome"

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(ReaderParaPessoa(reader))
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Private Shared Sub SalvarPessoaParams(cmd As NpgsqlCommand, p As Pessoa)
        cmd.Parameters.AddWithValue("@Nome", p.Nome)
        cmd.Parameters.AddWithValue("@Doc", p.Documento)
        cmd.Parameters.AddWithValue("@Tel", p.Telefone)
        cmd.Parameters.AddWithValue("@Vulneravel", p.IsVulneravel)
        cmd.Parameters.AddWithValue("@CEP", p.CEP)
        cmd.Parameters.AddWithValue("@Rua", p.Rua)
        cmd.Parameters.AddWithValue("@Num", p.Numero)
        cmd.Parameters.AddWithValue("@Comp", p.Complemento)
        cmd.Parameters.AddWithValue("@Bairro", p.Bairro)
        cmd.Parameters.AddWithValue("@Cid", p.Cidade)
        cmd.Parameters.AddWithValue("@UF", p.UF)
    End Sub
    Public Shared Sub SalvarNovaPessoa(p As Pessoa)
        Dim sql As String = "INSERT INTO pessoas (nome, documento, telefone, is_vulneravel, cep, rua, numero, complemento, bairro, cidade, uf, is_ativo, saldo_pontos) " &
                            "VALUES (@Nome, @Doc, @Tel, @Vulneravel, @CEP, @Rua, @Num, @Comp, @Bairro, @Cid, @UF, TRUE, 0)"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                SalvarPessoaParams(cmd, p)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub AtualizarPessoa(p As Pessoa)
        Dim sql As String = "UPDATE pessoas SET " &
                            "nome = @Nome, documento = @Doc, telefone = @Tel, is_vulneravel = @Vulneravel, " &
                            "cep = @CEP, rua = @Rua, numero = @Num, complemento = @Comp, " &
                            "bairro = @Bairro, cidade = @Cid, uf = @UF " &
                            "WHERE id_pessoa = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                SalvarPessoaParams(cmd, p)
                cmd.Parameters.AddWithValue("@ID", p.ID_Pessoa)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub InativarPessoa(idPessoa As Integer)
        Dim sql As String = "UPDATE pessoas SET is_ativo = FALSE WHERE id_pessoa = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@ID", idPessoa)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
#End Region

#Region "Lógica de Pontos de Coleta (RF02)"
    Private Shared Function ReaderParaPontoColeta(reader As NpgsqlDataReader) As PontoColeta
        Return New PontoColeta With {
            .ID_PontoColeta = reader.GetInt32(reader.GetOrdinal("id_ponto_coleta")),
            .Nome = reader.GetString(reader.GetOrdinal("nome")),
            .Responsavel = If(reader.IsDBNull(reader.GetOrdinal("responsavel")), "", reader.GetString(reader.GetOrdinal("responsavel"))),
            .Telefone = If(reader.IsDBNull(reader.GetOrdinal("telefone")), "", reader.GetString(reader.GetOrdinal("telefone"))),
            .CEP = If(reader.IsDBNull(reader.GetOrdinal("cep")), "", reader.GetString(reader.GetOrdinal("cep"))),
            .Rua = If(reader.IsDBNull(reader.GetOrdinal("rua")), "", reader.GetString(reader.GetOrdinal("rua"))),
            .Numero = If(reader.IsDBNull(reader.GetOrdinal("numero")), "", reader.GetString(reader.GetOrdinal("numero"))),
            .Complemento = If(reader.IsDBNull(reader.GetOrdinal("complemento")), "", reader.GetString(reader.GetOrdinal("complemento"))),
            .Bairro = If(reader.IsDBNull(reader.GetOrdinal("bairro")), "", reader.GetString(reader.GetOrdinal("bairro"))),
            .Cidade = If(reader.IsDBNull(reader.GetOrdinal("cidade")), "", reader.GetString(reader.GetOrdinal("cidade"))),
            .UF = If(reader.IsDBNull(reader.GetOrdinal("uf")), "", reader.GetString(reader.GetOrdinal("uf"))),
            .IsAtivo = reader.GetBoolean(reader.GetOrdinal("is_ativo"))
        }
    End Function
    Public Shared Function GetTodosPontosColetaAtivos() As List(Of PontoColeta)
        Dim lista As New List(Of PontoColeta)
        Dim sql As String = "SELECT * FROM pontos_coleta WHERE is_ativo = TRUE ORDER BY nome"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(ReaderParaPontoColeta(reader))
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Private Shared Sub SalvarPontoColetaParams(cmd As NpgsqlCommand, p As PontoColeta)
        cmd.Parameters.AddWithValue("@Nome", p.Nome)
        cmd.Parameters.AddWithValue("@Responsavel", p.Responsavel)
        cmd.Parameters.AddWithValue("@Telefone", p.Telefone)
        cmd.Parameters.AddWithValue("@CEP", p.CEP)
        cmd.Parameters.AddWithValue("@Rua", p.Rua)
        cmd.Parameters.AddWithValue("@Numero", p.Numero)
        cmd.Parameters.AddWithValue("@Complemento", p.Complemento)
        cmd.Parameters.AddWithValue("@Bairro", p.Bairro)
        cmd.Parameters.AddWithValue("@Cidade", p.Cidade)
        cmd.Parameters.AddWithValue("@UF", p.UF)
    End Sub
    Public Shared Sub SalvarNovoPontoColeta(p As PontoColeta)
        Dim sql As String = "INSERT INTO pontos_coleta (nome, responsavel, telefone, cep, rua, numero, complemento, bairro, cidade, uf, is_ativo) " &
                            "VALUES (@Nome, @Responsavel, @Telefone, @CEP, @Rua, @Numero, @Complemento, @Bairro, @Cidade, @UF, TRUE)"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                SalvarPontoColetaParams(cmd, p)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub AtualizarPontoColeta(p As PontoColeta)
        Dim sql As String = "UPDATE pontos_coleta SET " &
                            "nome = @Nome, responsavel = @Responsavel, telefone = @Telefone, " &
                            "cep = @CEP, rua = @Rua, numero = @Numero, complemento = @Complemento, " &
                            "bairro = @Bairro, cidade = @Cid, uf = @UF " &
                            "WHERE id_ponto_coleta = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                SalvarPontoColetaParams(cmd, p)
                cmd.Parameters.AddWithValue("@ID", p.ID_PontoColeta)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub InativarPontoColeta(idPonto As Integer)
        Dim sql As String = "UPDATE pontos_coleta SET is_ativo = FALSE WHERE id_ponto_coleta = @ID"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@ID", idPonto)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
#End Region

#Region "Lógica de Parâmetros (RF03, RF04, RF09)"
    Public Shared Function GetTodosParamCategorias() As List(Of ParamCategoria)
        Dim lista As New List(Of ParamCategoria)
        Dim sql As String = "SELECT * FROM param_categorias ORDER BY nome"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ParamCategoria With {
                            .ID_Categoria = reader.GetInt32(reader.GetOrdinal("id_categoria")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome")),
                            .PontosDoacao = reader.GetInt32(reader.GetOrdinal("pontos_doacao")),
                            .PontosRetirada = reader.GetInt32(reader.GetOrdinal("pontos_retirada")),
                            .IsEssencial = reader.GetBoolean(reader.GetOrdinal("is_essencial"))
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Public Shared Function GetTodosParamTamanhos() As List(Of ParamTamanho)
        Dim lista As New List(Of ParamTamanho)
        Dim sql As String = "SELECT * FROM param_tamanhos ORDER BY nome"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ParamTamanho With {
                            .ID_Tamanho = reader.GetInt32(reader.GetOrdinal("id_tamanho")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome"))
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Public Shared Function GetTodosParamCondicoes() As List(Of ParamCondicao)
        Dim lista As New List(Of ParamCondicao)
        Dim sql As String = "SELECT * FROM param_condicoes ORDER BY nome"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ParamCondicao With {
                            .ID_Condicao = reader.GetInt32(reader.GetOrdinal("id_condicao")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome"))
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Public Shared Function GetTodosParamStatusTriagem() As List(Of ParamStatusTriagem)
        Dim lista As New List(Of ParamStatusTriagem)
        Dim sql As String = "SELECT * FROM param_status_triagem ORDER BY nome"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ParamStatusTriagem With {
                            .ID_Status = reader.GetInt32(reader.GetOrdinal("id_status")),
                            .Nome = reader.GetString(reader.GetOrdinal("nome"))
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    Public Shared Sub SalvarListaParametros(Of T)(lista As List(Of T), nomeTabela As String, nomeColuna As String)
        Using conn = GetConnection()
            Using transaction = conn.BeginTransaction()
                Try
                    Using cmdDelete As New NpgsqlCommand(String.Format("DELETE FROM {0}", nomeTabela), conn, transaction)
                        cmdDelete.ExecuteNonQuery()
                    End Using
                    Dim sql As String = String.Format("INSERT INTO {0} ({1}) VALUES (@Valor)", nomeTabela, nomeColuna)
                    For Each item In lista
                        Using cmdInsert As New NpgsqlCommand(sql, conn, transaction)
                            cmdInsert.Parameters.AddWithValue("@Valor", item.ToString())
                            cmdInsert.ExecuteNonQuery()
                        End Using
                    Next
                    transaction.Commit()
                Catch ex As Exception
                    transaction.Rollback()
                    Throw New Exception(String.Format("Erro ao salvar lista de parâmetros: {0}", ex.Message))
                End Try
            End Using
        End Using
    End Sub
    Public Shared Sub SalvarParamCategorias(listaCategorias As ObservableCollection(Of ParamCategoria))
        Using conn = GetConnection()
            ' Abre a conexão se ela não estiver aberta (garantia)
            If conn.State <> ConnectionState.Open Then conn.Open()

            Using transaction = conn.BeginTransaction()
                Try
                    ' REMOVEMOS O "DELETE FROM param_categorias" QUE CAUSAVA O ERRO

                    For Each item In listaCategorias
                        If item.ID_Categoria > 0 Then
                            ' CASO 1: A categoria JÁ EXISTE (tem ID) -> Fazemos UPDATE
                            Dim sqlUpdate As String = "UPDATE param_categorias SET " &
                                                  "nome = @Nome, " &
                                                  "pontos_doacao = @PtsDoacao, " &
                                                  "pontos_retirada = @PtsRetirada, " &
                                                  "is_essencial = @IsEssencial " &
                                                  "WHERE id_categoria = @ID"

                            Using cmdUpdate As New NpgsqlCommand(sqlUpdate, conn, transaction)
                                cmdUpdate.Parameters.AddWithValue("@Nome", item.Nome)
                                cmdUpdate.Parameters.AddWithValue("@PtsDoacao", item.PontosDoacao)
                                cmdUpdate.Parameters.AddWithValue("@PtsRetirada", item.PontosRetirada)
                                cmdUpdate.Parameters.AddWithValue("@IsEssencial", item.IsEssencial)
                                cmdUpdate.Parameters.AddWithValue("@ID", item.ID_Categoria)
                                cmdUpdate.ExecuteNonQuery()
                            End Using

                        Else
                            ' CASO 2: A categoria é NOVA (ID é 0) -> Fazemos INSERT
                            Dim sqlInsert As String = "INSERT INTO param_categorias (nome, pontos_doacao, pontos_retirada, is_essencial) " &
                                                  "VALUES (@Nome, @PtsDoacao, @PtsRetirada, @IsEssencial)"

                            Using cmdInsert As New NpgsqlCommand(sqlInsert, conn, transaction)
                                cmdInsert.Parameters.AddWithValue("@Nome", item.Nome)
                                cmdInsert.Parameters.AddWithValue("@PtsDoacao", item.PontosDoacao)
                                cmdInsert.Parameters.AddWithValue("@PtsRetirada", item.PontosRetirada)
                                cmdInsert.Parameters.AddWithValue("@IsEssencial", item.IsEssencial)
                                cmdInsert.ExecuteNonQuery()
                            End Using
                        End If
                    Next

                    transaction.Commit()

                Catch ex As Exception
                    transaction.Rollback()
                    Throw New Exception(String.Format("Erro ao salvar categorias: {0}", ex.Message))
                End Try
            End Using
        End Using
    End Sub
#End Region

#Region "Lógica de Transações (Doação / Entrega)"
    Public Shared Sub SalvarNovaDoacao(doador As Pessoa, ponto As PontoColeta, data As DateTime, pontosTotais As Integer, itens As ObservableCollection(Of ItemDoacao))
        Using conn = GetConnection()
            Using transaction = conn.BeginTransaction()
                Try
                    ' 1. INSERE O CABEÇALHO DA DOAÇÃO (Isto não mudou)
                    Dim sqlDoacao As String = "INSERT INTO doacoes (id_pessoa, id_ponto_coleta, data_doacao, pontos_gerados, status_transacao) " &
                                            "VALUES (@IDPessoa, @IDPonto, @Data, @Pontos, 'Ativo') RETURNING id_doacao"
                    Dim novoIdDoacao As Integer
                    Using cmdDoacao As New NpgsqlCommand(sqlDoacao, conn, transaction)
                        cmdDoacao.Parameters.AddWithValue("@IDPessoa", doador.ID_Pessoa)
                        cmdDoacao.Parameters.AddWithValue("@IDPonto", ponto.ID_PontoColeta)
                        cmdDoacao.Parameters.AddWithValue("@Data", data)
                        cmdDoacao.Parameters.AddWithValue("@Pontos", pontosTotais)
                        novoIdDoacao = CInt(cmdDoacao.ExecuteScalar())
                    End Using

                    ' --- INÍCIO DA MUDANÇA (LÓGICA DE QUANTIDADE) ---

                    ' 2. INSERE OS ITENS (LOTES) COM A QUANTIDADE
                    ' (SQL ATUALIZADO para incluir as novas colunas)
                    Dim sqlItem As String = "INSERT INTO itens (id_doacao, id_categoria, id_tamanho, id_condicao, id_status_triagem, quantidade_doada, quantidade_estoque) " &
                                        "VALUES (@IDDoacao, @IDCategoria, @IDTamanho, @IDCondicao, @IDStatus, @QtdDoada, @QtdEstoque)"

                    For Each item In itens
                        ' Validação (agora verifica a quantidade também)
                        If item.Categoria Is Nothing Or item.Tamanho Is Nothing Or item.Condicao Is Nothing Or item.Destino Is Nothing Or item.Quantidade <= 0 Then
                            Throw New Exception(String.Format("Item '{0}' está com dados incompletos ou quantidade inválida.", item.Categoria))
                        End If

                        Using cmdItem As New NpgsqlCommand(sqlItem, conn, transaction)
                            cmdItem.Parameters.AddWithValue("@IDDoacao", novoIdDoacao)
                            cmdItem.Parameters.AddWithValue("@IDCategoria", item.Categoria.ID_Categoria)
                            cmdItem.Parameters.AddWithValue("@IDTamanho", item.Tamanho.ID_Tamanho)
                            cmdItem.Parameters.AddWithValue("@IDCondicao", item.Condicao.ID_Condicao)
                            cmdItem.Parameters.AddWithValue("@IDStatus", item.Destino.ID_Status)

                            ' (NOVAS LINHAS)
                            ' A Qtd. Doada e a Qtd. em Estoque são as mesmas no momento da entrada
                            cmdItem.Parameters.AddWithValue("@QtdDoada", item.Quantidade)
                            cmdItem.Parameters.AddWithValue("@QtdEstoque", item.Quantidade)

                            cmdItem.ExecuteNonQuery()
                        End Using
                    Next

                    ' --- FIM DA MUDANÇA ---

                    ' 3. ATUALIZA OS PONTOS DO DOADOR (Isto não mudou)
                    Dim sqlPontos As String = "UPDATE pessoas SET saldo_pontos = saldo_pontos + @Pontos WHERE id_pessoa = @IDPessoa"
                    Using cmdPontos As New NpgsqlCommand(sqlPontos, conn, transaction)
                        cmdPontos.Parameters.AddWithValue("@Pontos", pontosTotais)
                        cmdPontos.Parameters.AddWithValue("@IDPessoa", doador.ID_Pessoa)
                        cmdPontos.ExecuteNonQuery()
                    End Using

                    transaction.Commit()

                Catch ex As Exception
                    transaction.Rollback()
                    Throw New Exception(String.Format("Erro ao salvar doação: {0}", ex.Message))
                End Try
            End Using
        End Using
    End Sub

    Public Shared Function GetModoAlertaStatus() As Boolean
        Dim sql As String = "SELECT config_valor FROM configuracoes WHERE config_chave = 'MODO_ALERTA_FRIO'"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Dim resultado = cmd.ExecuteScalar()
                ' --- CORRIGIDO: string.Equals ---
                If resultado IsNot Nothing AndAlso String.Equals(resultado.ToString(), "ativo", StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            End Using
        End Using
        Return False
    End Function

    Public Shared Sub SetModoAlertaStatus(isAtivo As Boolean)
        Dim sql As String = "UPDATE configuracoes SET config_valor = @Valor WHERE config_chave = 'MODO_ALERTA_FRIO'"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@Valor", If(isAtivo, "ATIVO", "INATIVO"))
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Sub SalvarNovaEntrega(beneficiario As Pessoa, tipoEntrega As String, pontosDebitar As Integer, carrinho As List(Of ItemCarrinho))
        Using conn = GetConnection()
            Using transaction = conn.BeginTransaction()
                Try
                    ' 1. INSERE O CABEÇALHO DA ENTREGA (Isto não mudou)
                    Dim sqlEntrega As String = "INSERT INTO entregas (id_pessoa, data_entrega, tipo_entrega, pontos_debitados, status_transacao) " &
                                            "VALUES (@IDPessoa, CURRENT_TIMESTAMP, @Tipo, @Pontos, 'Ativo') RETURNING id_entrega"
                    Dim novoIdEntrega As Integer
                    Using cmdEntrega As New NpgsqlCommand(sqlEntrega, conn, transaction)
                        cmdEntrega.Parameters.AddWithValue("@IDPessoa", beneficiario.ID_Pessoa)
                        cmdEntrega.Parameters.AddWithValue("@Tipo", tipoEntrega)
                        cmdEntrega.Parameters.AddWithValue("@Pontos", pontosDebitar)
                        novoIdEntrega = CInt(cmdEntrega.ExecuteScalar())
                    End Using

                    ' --- INÍCIO DA MUDANÇA (LÓGICA DE QUANTIDADE) ---

                    ' 2. SQLs para ATUALIZAR o estoque e REGISTRAR a saída
                    Dim sqlUpdateEstoque As String = "UPDATE itens SET quantidade_estoque = quantidade_estoque - @QtdRetirada " &
                                                "WHERE id_item = @IDItemLote"

                    Dim sqlInsertRecibo As String = "INSERT INTO entrega_itens (id_entrega, id_item, quantidade_retirada) " &
                                                "VALUES (@IDEntrega, @IDItemLote, @QtdRetirada)"

                    For Each itemNoCarrinho In carrinho

                        ' 2a. Atualiza o estoque do lote (Ex: 10 - 2 = 8)
                        Using cmdUpdate As New NpgsqlCommand(sqlUpdateEstoque, conn, transaction)
                            cmdUpdate.Parameters.AddWithValue("@QtdRetirada", itemNoCarrinho.QuantidadeRetirar)
                            cmdUpdate.Parameters.AddWithValue("@IDItemLote", itemNoCarrinho.LoteDeEstoque.ID_Item)
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        ' 2b. Registra o "recibo" na nova tabela (Ex: Entrega 101 levou 2 do Lote 50)
                        Using cmdInsert As New NpgsqlCommand(sqlInsertRecibo, conn, transaction)
                            cmdInsert.Parameters.AddWithValue("@IDEntrega", novoIdEntrega)
                            cmdInsert.Parameters.AddWithValue("@IDItemLote", itemNoCarrinho.LoteDeEstoque.ID_Item)
                            cmdInsert.Parameters.AddWithValue("@QtdRetirada", itemNoCarrinho.QuantidadeRetirar)
                            cmdInsert.ExecuteNonQuery()
                        End Using
                    Next

                    ' --- FIM DA MUDANÇA ---

                    ' 3. DEBITA OS PONTOS (Isto não mudou)
                    If pontosDebitar > 0 Then
                        Dim sqlPontos As String = "UPDATE pessoas SET saldo_pontos = saldo_pontos - @Pontos WHERE id_pessoa = @IDPessoa"
                        Using cmdPontos As New NpgsqlCommand(sqlPontos, conn, transaction)
                            cmdPontos.Parameters.AddWithValue("@Pontos", pontosDebitar)
                            cmdPontos.Parameters.AddWithValue("@IDPessoa", beneficiario.ID_Pessoa)
                            cmdPontos.ExecuteNonQuery()
                        End Using
                    End If

                    transaction.Commit()

                Catch ex As Exception
                    transaction.Rollback()
                    Throw New Exception(String.Format("Erro ao salvar entrega: {0}", ex.Message))
                End Try
            End Using
        End Using
    End Sub

    Public Shared Function GetItensDisponiveisParaSaida() As List(Of ItemDisponivel)
        Dim lista As New List(Of ItemDisponivel)

        ' SQL ATUALIZADO: Busca lotes com estoque > 0
        Dim sql As String = "SELECT " &
                        "    i.id_item, " &
                        "    c.nome AS categoria_nome, " &
                        "    t.nome AS tamanho_nome, " &
                        "    c.pontos_retirada, " &
                        "    c.is_essencial, " &
                        "    i.quantidade_estoque " & ' <-- NOVO CAMPO
                        "FROM itens i " &
                        "JOIN param_categorias c ON i.id_categoria = c.id_categoria " &
                        "JOIN param_tamanhos t ON i.id_tamanho = t.id_tamanho " &
                        "JOIN param_status_triagem s ON i.id_status_triagem = s.id_status " &
                        "WHERE " &
                        "    i.quantidade_estoque > 0 " & ' <-- NOVA LÓGICA DE FILTRO
                        "    AND s.nome IN ('Disponível', 'Disponível para Troca', 'Disponível (Doação Pura)')" ' (Mantemos esta regra)

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ItemDisponivel With {
                        .ID_Item = reader.GetInt32(reader.GetOrdinal("id_item")),
                        .Nome = reader.GetString(reader.GetOrdinal("categoria_nome")),
                        .Tamanho = reader.GetString(reader.GetOrdinal("tamanho_nome")),
                        .CustoPontos = reader.GetInt32(reader.GetOrdinal("pontos_retirada")),
                        .IsEssencial = reader.GetBoolean(reader.GetOrdinal("is_essencial")),
                        .QuantidadeEmEstoque = reader.GetInt32(reader.GetOrdinal("quantidade_estoque")) ' <-- NOVO
                    })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
#End Region

#Region "Lógica de Inventário (RF12)"
    Public Shared Function GetItensInventario() As List(Of ItemInventario)
        Dim lista As New List(Of ItemInventario)

        ' SQL ATUALIZADO (V2.0)
        Dim sql As String = "SELECT " &
                        "    c.nome AS categoria_nome, " &
                        "    t.nome AS tamanho_nome, " &
                        "    co.nome AS condicao_nome, " &
                        "    s.nome AS status_nome, " &
                        "    d.data_doacao, " &
                        "    c.pontos_retirada, " &
                        "    c.is_essencial, " &
                        "    i.quantidade_doada, " &   ' <-- NOVO
                        "    i.quantidade_estoque " &  ' <-- NOVO
                        "FROM itens i " &
                        "JOIN doacoes d ON i.id_doacao = d.id_doacao " &
                        "JOIN param_categorias c ON i.id_categoria = c.id_categoria " &
                        "JOIN param_tamanhos t ON i.id_tamanho = t.id_tamanho " &
                        "JOIN param_condicoes co ON i.id_condicao = co.id_condicao " &
                        "JOIN param_status_triagem s ON i.id_status_triagem = s.id_status " &
                        "ORDER BY d.data_doacao DESC" ' (Não precisamos mais do WHERE i.id_entrega IS NULL)

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New ItemInventario With {
                        .Categoria = reader.GetString(reader.GetOrdinal("categoria_nome")),
                        .Tamanho = reader.GetString(reader.GetOrdinal("tamanho_nome")),
                        .Condicao = reader.GetString(reader.GetOrdinal("condicao_nome")),
                        .Status = reader.GetString(reader.GetOrdinal("status_nome")),
                        .DataEntrada = reader.GetDateTime(reader.GetOrdinal("data_doacao")),
                        .Pontos = reader.GetInt32(reader.GetOrdinal("pontos_retirada")),
                        .IsEssencial = reader.GetBoolean(reader.GetOrdinal("is_essencial")),
                        .QuantidadeDoada = reader.GetInt32(reader.GetOrdinal("quantidade_doada")), ' <-- NOVO
                        .QuantidadeEmEstoque = reader.GetInt32(reader.GetOrdinal("quantidade_estoque")) ' <-- NOVO
                    })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
#End Region

#Region "Lógica de Dashboard (RF18) (NOVA)"
    ''' <summary>
    ''' (RF18.3) Conta o total de pessoas marcadas como vulneráveis
    ''' </summary>
    Public Shared Function GetTotalPessoasVulneraveis() As Integer
        Dim sql As String = "SELECT COUNT(id_pessoa) FROM pessoas WHERE is_ativo = TRUE AND is_vulneravel = TRUE"
        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                ' ExecuteScalar é usado para buscar um único valor (a contagem)
                Return CInt(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' (RF18.2) Conta o total de peças essenciais em estoque
    ''' </summary>
    ''' <summary>
    ''' (RF18.2) Conta o total de peças essenciais em estoque (V2.0)
    ''' </summary>
    Public Shared Function GetTotalPecasEssenciaisEmEstoque() As Integer
        ' SQL ATUALIZADO (V2.0)
        ' Agora somamos a quantidade em estoque, em vez de contar linhas
        Dim sql As String = "SELECT COALESCE(SUM(i.quantidade_estoque), 0) " &
                        "FROM itens i " &
                        "JOIN param_categorias c ON i.id_categoria = c.id_categoria " &
                        "JOIN param_status_triagem s ON i.id_status_triagem = s.id_status " &
                        "WHERE " &
                        "    i.quantidade_estoque > 0 " & ' <-- LÓGICA CORRIGIDA
                        "    AND c.is_essencial = TRUE " &
                        "    AND s.nome IN ('Disponível', 'Disponível para Troca', 'Disponível (Doação Pura)')"

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                ' Usamos COALESCE para garantir que, se a soma for NULL (vazia), retorne 0.
                Return CInt(cmd.ExecuteScalar())
            End Using
        End Using
    End Function
#End Region

#Region "Lógica de Auditoria (RF16)"
    Public Shared Function GetTransacoesAuditoria(dataInicio As DateTime?, dataFim As DateTime?, idPessoa As Integer, tipo As String) As List(Of TransacaoAuditoria)
        Dim lista As New List(Of TransacaoAuditoria)

        ' SQL ATUALIZADO (V2.0)
        Dim sqlBase As String =
        "(SELECT " &
        "    d.id_doacao AS id, 'Entrada' AS tipo, d.data_doacao AS data, p.nome AS pessoa, p.id_pessoa as id_pessoa_ref, " &
        "    (SELECT STRING_AGG(CONCAT(i.quantidade_doada, 'x ', c.nome), ', ') FROM itens i JOIN param_categorias c ON i.id_categoria = c.id_categoria WHERE i.id_doacao = d.id_doacao) AS descricao, " &
        "    d.pontos_gerados AS pontos, d.status_transacao as status " &
        "FROM doacoes d " &
        "JOIN pessoas p ON d.id_pessoa = p.id_pessoa " &
        ") " &
        "UNION ALL " &
        "(SELECT " &
        "    e.id_entrega AS id, e.tipo_entrega AS tipo, e.data_entrega AS data, p.nome AS pessoa, p.id_pessoa as id_pessoa_ref, " &
        "    (SELECT STRING_AGG(CONCAT(ei.quantidade_retirada, 'x ', c.nome), ', ') " &
        "     FROM entrega_itens ei " &
        "     JOIN itens i ON ei.id_item = i.id_item " &
        "     JOIN param_categorias c ON i.id_categoria = c.id_categoria " &
        "     WHERE ei.id_entrega = e.id_entrega) AS descricao, " &
        "    e.pontos_debitados * -1 AS pontos, e.status_transacao as status " &
        "FROM entregas e " &
        "JOIN pessoas p ON e.id_pessoa = p.id_pessoa " &
        ")"

        ' Começa a construir a consulta final com os filtros
        Dim sqlBuilder As New System.Text.StringBuilder()
        sqlBuilder.Append("SELECT * FROM (")
        sqlBuilder.Append(sqlBase)
        sqlBuilder.Append(") AS T_Union WHERE status = 'Ativo' ") ' Filtra apenas os ativos por padrão

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand()

                ' 1. Filtro de Data Início
                If dataInicio IsNot Nothing Then
                    sqlBuilder.Append(" AND data >= @DataInicio ")
                    cmd.Parameters.AddWithValue("@DataInicio", dataInicio.Value.Date)
                End If

                ' 2. Filtro de Data Fim
                If dataFim IsNot Nothing Then
                    sqlBuilder.Append(" AND data <= @DataFim ")
                    cmd.Parameters.AddWithValue("@DataFim", dataFim.Value.Date.AddDays(1).AddSeconds(-1)) ' Pega o dia todo
                End If

                ' 3. Filtro de Pessoa
                If idPessoa > 0 Then ' (Assumindo que 0 = Todas)
                    sqlBuilder.Append(" AND id_pessoa_ref = @IDPessoa ")
                    cmd.Parameters.AddWithValue("@IDPessoa", idPessoa)
                End If

                ' 4. Filtro de Tipo
                If Not String.IsNullOrEmpty(tipo) AndAlso Not String.Equals(tipo, "(Todos)", StringComparison.OrdinalIgnoreCase) Then
                    sqlBuilder.Append(" AND tipo = @Tipo ")
                    cmd.Parameters.AddWithValue("@Tipo", tipo)
                End If

                sqlBuilder.Append(" ORDER BY data DESC")

                cmd.Connection = conn
                cmd.CommandText = sqlBuilder.ToString()

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New TransacaoAuditoria With {
                        .ID_Transacao = reader.GetInt32(reader.GetOrdinal("id")),
                        .Tipo = reader.GetString(reader.GetOrdinal("tipo")),
                        .DataTransacao = reader.GetDateTime(reader.GetOrdinal("data")),
                        .Pessoa = reader.GetString(reader.GetOrdinal("pessoa")),
                        .Descricao = If(reader.IsDBNull(reader.GetOrdinal("descricao")), "-", reader.GetString(reader.GetOrdinal("descricao"))),
                        .Pontos = reader.GetInt32(reader.GetOrdinal("pontos"))
                    })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Public Shared Sub EstornarTransacao(id As Integer, tipo As String)
        Using conn = GetConnection()
            Using transaction = conn.BeginTransaction()
                Try
                    If tipo = "Entrada" Then
                        ' --- ESTORNAR DOAÇÃO (LÓGICA V2.0) ---
                        Dim idDoador As Integer = 0
                        Dim pontosGerados As Integer = 0

                        ' 1. Acha a doação e os pontos
                        Using cmdFind As New NpgsqlCommand("SELECT id_pessoa, pontos_gerados FROM doacoes WHERE id_doacao = @ID", conn, transaction)
                            cmdFind.Parameters.AddWithValue("@ID", id)
                            Using reader = cmdFind.ExecuteReader()
                                If reader.Read() Then
                                    idDoador = reader.GetInt32(0)
                                    pontosGerados = reader.GetInt32(1)
                                Else
                                    Throw New Exception("Doação não encontrada.")
                                End If
                            End Using
                        End Using

                        ' 2. Devolve os pontos
                        Using cmdPontos As New NpgsqlCommand("UPDATE pessoas SET saldo_pontos = saldo_pontos - @Pontos WHERE id_pessoa = @IDPessoa", conn, transaction)
                            cmdPontos.Parameters.AddWithValue("@Pontos", pontosGerados)
                            cmdPontos.Parameters.AddWithValue("@IDPessoa", idDoador)
                            cmdPontos.ExecuteNonQuery()
                        End Using

                        ' 3. ZERA o estoque dos itens ligados a esta doação
                        ' (Impede que itens de uma doação estornada sejam entregues)
                        Using cmdItens As New NpgsqlCommand("UPDATE itens SET quantidade_estoque = 0 WHERE id_doacao = @ID", conn, transaction)
                            cmdItens.Parameters.AddWithValue("@ID", id)
                            cmdItens.ExecuteNonQuery()
                        End Using

                        ' 4. Marca a doação como estornada
                        Using cmdUpdate As New NpgsqlCommand("UPDATE doacoes SET status_transacao = 'Estornado' WHERE id_doacao = @ID", conn, transaction)
                            cmdUpdate.Parameters.AddWithValue("@ID", id)
                            cmdUpdate.ExecuteNonQuery()
                        End Using
                    Else
                        ' --- ESTORNAR ENTREGA (LÓGICA V2.0) ---
                        Dim idBeneficiario As Integer = 0
                        Dim pontosDebitados As Integer = 0

                        ' 1. Acha a entrega e os pontos
                        Using cmdFind As New NpgsqlCommand("SELECT id_pessoa, pontos_debitados FROM entregas WHERE id_entrega = @ID", conn, transaction)
                            cmdFind.Parameters.AddWithValue("@ID", id)
                            Using reader = cmdFind.ExecuteReader()
                                If reader.Read() Then
                                    idBeneficiario = reader.GetInt32(0)
                                    pontosDebitados = reader.GetInt32(1)
                                Else
                                    Throw New Exception("Entrega não encontrada.")
                                End If
                            End Using
                        End Using

                        ' 2. Devolve os pontos (se foi troca)
                        If pontosDebitados > 0 Then
                            Using cmdPontos As New NpgsqlCommand("UPDATE pessoas SET saldo_pontos = saldo_pontos + @Pontos WHERE id_pessoa = @IDPessoa", conn, transaction)
                                cmdPontos.Parameters.AddWithValue("@Pontos", pontosDebitados)
                                cmdPontos.Parameters.AddWithValue("@IDPessoa", idBeneficiario)
                                cmdPontos.ExecuteNonQuery()
                            End Using
                        End If

                        ' 3. Acha TODOS os itens/quantidades que saíram nesta entrega (NOVA LÓGICA)
                        Dim itensParaDevolver As New List(Of Tuple(Of Integer, Integer))()
                        Using cmdRecibo As New NpgsqlCommand("SELECT id_item, quantidade_retirada FROM entrega_itens WHERE id_entrega = @ID", conn, transaction)
                            cmdRecibo.Parameters.AddWithValue("@ID", id)
                            Using reader = cmdRecibo.ExecuteReader()
                                While reader.Read()
                                    itensParaDevolver.Add(Tuple.Create(reader.GetInt32(0), reader.GetInt32(1)))
                                End While
                            End Using
                        End Using

                        ' 4. Devolve os itens ao estoque (NOVA LÓGICA)
                        For Each itemInfo In itensParaDevolver
                            Dim idItemLote = itemInfo.Item1
                            Dim qtdDevolver = itemInfo.Item2
                            Using cmdDevolver As New NpgsqlCommand("UPDATE itens SET quantidade_estoque = quantidade_estoque + @Qtd WHERE id_item = @IDItem", conn, transaction)
                                cmdDevolver.Parameters.AddWithValue("@Qtd", qtdDevolver)
                                cmdDevolver.Parameters.AddWithValue("@IDItem", idItemLote)
                                cmdDevolver.ExecuteNonQuery()
                            End Using
                        Next

                        ' 5. Marca a entrega como estornada
                        Using cmdUpdate As New NpgsqlCommand("UPDATE entregas SET status_transacao = 'Estornado' WHERE id_entrega = @ID", conn, transaction)
                            cmdUpdate.Parameters.AddWithValue("@ID", id)
                            cmdUpdate.ExecuteNonQuery()
                        End Using
                    End If

                    transaction.Commit()
                Catch ex As Exception
                    transaction.Rollback()
                    Throw New Exception(String.Format("Erro ao estornar: {0}", ex.Message))
                End Try
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (NOVO) Busca o histórico de transações para UMA pessoa específica.
    ''' </summary>
    ''' <summary>
    ''' (NOVO) Busca o histórico de transações para UMA pessoa específica. (V2.0)
    ''' </summary>
    Public Shared Function GetTransacoesPorPessoa(idPessoa As Integer) As List(Of TransacaoAuditoria)
        Dim lista As New List(Of TransacaoAuditoria)

        ' SQL ATUALIZADO (V2.0)
        Dim sql As String =
        "(SELECT " &
        "    d.id_doacao AS id, 'Entrada' AS tipo, d.data_doacao AS data, p.nome AS pessoa, " &
        "    (SELECT STRING_AGG(CONCAT(i.quantidade_doada, 'x ', c.nome), ', ') FROM itens i JOIN param_categorias c ON i.id_categoria = c.id_categoria WHERE i.id_doacao = d.id_doacao) AS descricao, " &
        "    d.pontos_gerados AS pontos " &
        "FROM doacoes d " &
        "JOIN pessoas p ON d.id_pessoa = p.id_pessoa " &
        "WHERE d.status_transacao = 'Ativo' AND d.id_pessoa = @IDPessoa " & ' <-- Filtro
        ") " &
        "UNION ALL " &
        "(SELECT " &
        "    e.id_entrega AS id, e.tipo_entrega AS tipo, e.data_entrega AS data, p.nome AS pessoa, " &
        "    (SELECT STRING_AGG(CONCAT(ei.quantidade_retirada, 'x ', c.nome), ', ') " &
        "     FROM entrega_itens ei " &
        "     JOIN itens i ON ei.id_item = i.id_item " &
        "     JOIN param_categorias c ON i.id_categoria = c.id_categoria " &
        "     WHERE ei.id_entrega = e.id_entrega) AS descricao, " &
        "    e.pontos_debitados * -1 AS pontos " &
        "FROM entregas e " &
        "JOIN pessoas p ON e.id_pessoa = p.id_pessoa " &
        "WHERE e.status_transacao = 'Ativo' AND e.id_pessoa = @IDPessoa " & ' <-- Filtro
        ") " &
        "ORDER BY data DESC"

        Using conn = GetConnection()
            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@IDPessoa", idPessoa) ' <-- Parâmetro do filtro
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        lista.Add(New TransacaoAuditoria With {
                        .ID_Transacao = reader.GetInt32(reader.GetOrdinal("id")),
                        .Tipo = reader.GetString(reader.GetOrdinal("tipo")),
                        .DataTransacao = reader.GetDateTime(reader.GetOrdinal("data")),
                        .Pessoa = reader.GetString(reader.GetOrdinal("pessoa")),
                        .Descricao = If(reader.IsDBNull(reader.GetOrdinal("descricao")), "-", reader.GetString(reader.GetOrdinal("descricao"))),
                        .Pontos = reader.GetInt32(reader.GetOrdinal("pontos"))
                    })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function
    ''' <summary>
    ''' (NOVO) Função auxiliar para verificar se uma coluna existe no Reader
    ''' </summary>
    Private Shared Function HasColumn(reader As NpgsqlDataReader, columnName As String) As Boolean
        For i As Integer = 0 To reader.FieldCount - 1
            If reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next
        Return False
    End Function
#End Region

End Class