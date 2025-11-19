' =============================================================
' ARQUIVO: PageAuditoria.vb
' -------------------------------------------------------------
' OBJETIVO: Gerenciar a tela de Auditoria, onde o administrador
' pode visualizar, filtrar e estornar transações (doações, trocas).
' LINGUAGEM: Visual Basic .NET
' =============================================================

Imports System.ComponentModel ' Necessário para usar ICollectionView
Imports System.Windows.Data ' Para o ICollectionView (ferramenta de visualização e filtragem)

' =============================================================
' CLASSE PRINCIPAL DA PÁGINA: PageAuditoria
' -------------------------------------------------------------
' Contém a lógica de controle da auditoria, filtros e estornos.
' =============================================================
Public Class PageAuditoria

    ' Lista principal que guarda todas as transações carregadas do banco
    Private masterListTransacoes As New List(Of TransacaoAuditoria)()
    ' transacoesView: Objeto usado para aplicar filtros e ordenação na grade (DataGrid)
    Private transacoesView As ICollectionView

    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Ação que ocorre automaticamente assim que a página é carregada.
    ' =============================================================
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Checagem de segurança: verifica se o usuário é administrador
        If Not Sessao.EhAdmin() Then
            ' Se não for Admin, nega o acesso
            MessageBox.Show("Acesso restrito a administradores.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning)
            ' Desabilita a página para impedir o uso
            Me.IsEnabled = False ' ou navegue para outra página
        End If
        ' Obtém o objeto de visualização (transacoesView) a partir da lista
        transacoesView = CollectionViewSource.GetDefaultView(masterListTransacoes)
        ' Liga a visualização ao controle DataGrid
        dgAuditoria.ItemsSource = transacoesView
        ' Chama a função para carregar os dados e filtros
        CarregarFiltrosETransacoes()
    End Sub

    ' =============================================================
    ' FUNÇÃO: CarregarFiltrosETransacoes
    ' -------------------------------------------------------------
    ' Busca os dados de filtro (Combobox) e carrega a lista inicial
    ' de transações do DataAccess.
    ' =============================================================
    Private Sub CarregarFiltrosETransacoes()
        Try
            ' 1. Carrega os filtros: Busca todas as pessoas ativas
            Dim pessoas = DataAccess.GetTodasPessoasAtivas().Select(Function(p) p.Nome).ToList()
            ' Adiciona a opção "(Todas)" na primeira posição
            pessoas.Insert(0, "(Todas)")
            ' Liga a lista de pessoas ao Combobox
            cmbFiltroPessoa.ItemsSource = pessoas

            ' Define as opções fixas para o filtro de Tipo de Transação
            cmbFiltroTipo.ItemsSource = New List(Of String) From {"(Todos)", "Entrada", "Troca", "Doação Pura"}
            ' Zera a seleção inicial dos filtros
            LimparFiltros()

            ' 2. Carrega o log de transações (RF16)
            ' Busca todas as transações, passando 'Nothing' para não aplicar filtro ainda
            masterListTransacoes = DataAccess.GetTransacoesAuditoria(Nothing, Nothing, 0, "(Todos)")
            ' Cria/atualiza o objeto de visualização com a lista completa
            transacoesView = CollectionViewSource.GetDefaultView(masterListTransacoes)
            dgAuditoria.ItemsSource = transacoesView

        Catch ex As Exception
            ' Trata erros de conexão ou de busca no banco
            MessageBox.Show(String.Format("Erro fatal ao carregar auditoria: {0}", ex.Message),
              "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnAplicarFiltros_Click
    ' -------------------------------------------------------------
    ' Define e aplica a regra de filtro em memória (no transacoesView).
    ' =============================================================
    Private Sub BtnAplicarFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnAplicarFiltros.Click
        ' Define a função de filtro para a visualização das transações
        transacoesView.Filter = Function(item)
                                    ' Converte o item atual para o tipo TransacaoAuditoria
                                    Dim obj = CType(item, TransacaoAuditoria)

                                    ' 1. Lógica do Filtro Data Início
                                    ' Verifica se a data inicial foi preenchida
                                    If dpFiltroDataInicio.SelectedDate IsNot Nothing Then
                                        ' Se a data da transação for ANTERIOR à data inicial do filtro
                                        If obj.DataTransacao.Date < dpFiltroDataInicio.SelectedDate.Value.Date Then
                                            Return False ' Esconde o item
                                        End If
                                    End If

                                    ' 2. Lógica do Filtro Data Fim
                                    ' Verifica se a data final foi preenchida
                                    If dpFiltroDataFim.SelectedDate IsNot Nothing Then
                                        ' Se a data da transação for POSTERIOR à data final do filtro
                                        If obj.DataTransacao.Date > dpFiltroDataFim.SelectedDate.Value.Date Then
                                            Return False ' Esconde o item
                                        End If
                                    End If

                                    ' 3. Lógica do Filtro Pessoa
                                    Dim filtroPessoa = CType(cmbFiltroPessoa.SelectedItem, String)
                                    ' Verifica se não é "(Todas)" E se a Pessoa do item NÃO é igual ao filtro
                                    If Not String.IsNullOrEmpty(filtroPessoa) AndAlso
                   Not String.Equals(filtroPessoa, "(Todas)", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(obj.Pessoa, filtroPessoa, StringComparison.OrdinalIgnoreCase) Then
                                        Return False ' Esconde o item
                                    End If

                                    ' 4. Lógica do Filtro Tipo
                                    Dim filtroTipo = CType(cmbFiltroTipo.SelectedItem, String)
                                    ' Verifica se não é "(Todos)" E se o Tipo do item NÃO é igual ao filtro
                                    If Not String.IsNullOrEmpty(filtroTipo) AndAlso
                   Not String.Equals(filtroTipo, "(Todos)", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(obj.Tipo, filtroTipo, StringComparison.OrdinalIgnoreCase) Then
                                        Return False ' Esconde o item
                                    End If

                                    ' Se o item passou por todos os filtros, retorna True
                                    Return True
                                End Function
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnLimparFiltros_Click
    ' -------------------------------------------------------------
    ' Ação do botão: limpa a seleção dos filtros e remove a regra de filtro.
    ' =============================================================
    Private Sub BtnLimparFiltros_Click(sender As Object, e As RoutedEventArgs) Handles btnLimparFiltros.Click
        ' Zera a seleção visual dos filtros
        LimparFiltros()
        ' Remove a função de filtro, fazendo com que todos os itens sejam exibidos
        transacoesView.Filter = Nothing
    End Sub

    ' =============================================================
    ' FUNÇÃO: LimparFiltros
    ' -------------------------------------------------------------
    ' Função auxiliar: zera a seleção de todos os controles de filtro.
    ' =============================================================
    Private Sub LimparFiltros()
        ' Limpa as datas
        dpFiltroDataInicio.SelectedDate = Nothing
        dpFiltroDataFim.SelectedDate = Nothing
        ' Zera a seleção dos Combobox para o item "(Todas)" / "(Todos)" (posição 0)
        cmbFiltroPessoa.SelectedIndex = 0
        cmbFiltroTipo.SelectedIndex = 0
    End Sub

    ' =============================================================
    ' FUNÇÃO: BtnEstornar_Click
    ' -------------------------------------------------------------
    ' Processa o estorno de uma transação. Esta é uma ação crítica.
    ' =============================================================
    Private Sub BtnEstornar_Click(sender As Object, e As RoutedEventArgs)
        ' Pega o objeto (TransacaoAuditoria) da linha onde o botão foi clicado
        Dim transacaoParaEstornar = CType(CType(sender, Button).DataContext, TransacaoAuditoria)

        ' Monta a mensagem de confirmação, incluindo os dados da transação
        Dim msg = String.Format("Tem certeza que deseja estornar a transação ID {0}?{1}{1}Ação: {2}{1}Pessoa: {3}{1}{1}Esta ação é irreversível.",
                transacaoParaEstornar.ID_Transacao, vbCrLf, transacaoParaEstornar.Descricao, transacaoParaEstornar.Pessoa)

        ' Exibe a caixa de diálogo de confirmação (Sim ou Não)
        Dim resposta As MessageBoxResult = MessageBox.Show(msg, "Confirmar Estorno (RF16)", MessageBoxButton.YesNo, MessageBoxImage.Warning)

        ' Se o usuário clicar em "Não", para a execução
        If resposta = MessageBoxResult.No Then
            Return
        End If

        Try
            ' Chama o DataAccess (a ponte) para executar a lógica de estorno no banco
            DataAccess.EstornarTransacao(transacaoParaEstornar.ID_Transacao, transacaoParaEstornar.Tipo)

            ' Mensagem de sucesso
            MessageBox.Show(String.Format("Transação ID {0} estornada com sucesso.", transacaoParaEstornar.ID_Transacao), "Estorno Concluído", MessageBoxButton.OK, MessageBoxImage.Information)

            ' Recarrega a grade para refletir a transação estornada/removida
            CarregarFiltrosETransacoes()

        Catch ex As Exception
            ' Trata erros de banco de dados durante o estorno
            MessageBox.Show(String.Format("Erro ao estornar transação: {0}", ex.Message), "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


End Class

' =============================================================
' RESUMO: FLUXO DE AUDITORIA E ESTORNO
' -------------------------------------------------------------
' 1. ACESSO: O acesso à página é restrito, checado via Sessao.EhAdmin().
' 2. CARREGAMENTO: O DataAccess busca todas as TransacoesAuditoria (o log).
' 3. FILTRAGEM: A lista na tela é gerenciada pelo ICollectionView (transacoesView).
'    - Ao clicar em "Aplicar Filtros", a função transacoesView.Filter é definida.
'    - O filtro roda em memória, checando se a transação está DENTRO do período
'      de datas e se o Tipo/Pessoa batem com a seleção.
' 4. ESTORNO (RF16): A função BtnEstornar_Click faz uma checagem de
'    confirmação obrigatória. Se confirmada, chama o DataAccess.EstornarTransacao(),
'    que executa a lógica reversa no banco (devolvendo pontos, repondo estoque, etc.).
' =============================================================