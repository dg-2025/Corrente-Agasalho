' =============================================================
' IMPORTAÇÕES DE BIBLIOTECAS
' -------------------------------------------------------------
' - HttpClient: usada para acessar APIs externas (requisições web).
' - Newtonsoft.Json: biblioteca que converte JSON em objetos VB.
' - Media e BitmapImage: exibem ícones e imagens vindas da internet.
' - Linq: usado para filtrar e agrupar dados facilmente.
' =============================================================
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Linq


' =============================================================
' CLASSES AUXILIARES PARA TRATAR A API DE CLIMA (JSON)
' -------------------------------------------------------------
' Estas classes representam a estrutura dos dados recebidos da API.
' Cada classe corresponde a uma parte do JSON retornado pelo site
' openweathermap.org, o serviço que fornece dados meteorológicos.
' =============================================================

' Informações básicas do clima (condição e ícone)
Public Class WeatherInfo
    <JsonProperty("main")>
    Public Property Main As String
    <JsonProperty("description")>
    Public Property Description As String
    <JsonProperty("icon")>
    Public Property Icon As String
End Class

' Dados numéricos principais: temperatura e umidade
Public Class WeatherMain
    <JsonProperty("temp")>
    Public Property Temp As Double
    <JsonProperty("humidity")>
    Public Property Humidity As Integer
End Class

' Dados sobre o vento
Public Class WeatherWind
    <JsonProperty("speed")>
    Public Property Speed As Double
End Class

' Estrutura completa do retorno da API de clima atual
Public Class WeatherResponse
    <JsonProperty("weather")>
    Public Property Weather As List(Of WeatherInfo)
    <JsonProperty("main")>
    Public Property Main As WeatherMain
    <JsonProperty("wind")>
    Public Property Wind As WeatherWind
    <JsonProperty("name")>
    Public Property Name As String
End Class

' Classes auxiliares para a previsão (5 dias)
Public Class ForecastWeatherInfo
    <JsonProperty("icon")>
    Public Property Icon As String
End Class

Public Class ForecastMain
    <JsonProperty("temp_min")>
    Public Property TempMin As Double
    <JsonProperty("temp_max")>
    Public Property TempMax As Double
End Class

Public Class ForecastListItem
    <JsonProperty("dt_txt")>
    Public Property DtTxt As String
    <JsonProperty("main")>
    Public Property Main As ForecastMain
    <JsonProperty("weather")>
    Public Property Weather As List(Of ForecastWeatherInfo)
End Class

Public Class ForecastResponse
    <JsonProperty("list")>
    Public Property List As List(Of ForecastListItem)
End Class

' Classe usada para exibir os dias da previsão no ListView
Public Class PrevisaoDia
    Public Property DiaSemana As String
    Public Property IconeUrl As String
    Public Property MaxTemp As String
    Public Property MinTemp As String
End Class


' =============================================================
' CLASSE PRINCIPAL: PageDashboard
' -------------------------------------------------------------
' Esta página é o "painel inicial" do sistema (Dashboard).
' Mostra indicadores do banco (peças, pessoas) e dados de clima.
' Também controla o "Modo de Alerta de Frio" (RF17 e RF18).
' =============================================================
Public Class PageDashboard

    ' Cliente HTTP que será reutilizado para chamadas da API
    Private Shared ReadOnly httpClient As New HttpClient()

    ' Armazena o status atual do modo de alerta
    Private isModoAlertaAtivo As Boolean = False

    ' =============================================================
    ' CONSTANTES DE CONFIGURAÇÃO DA API
    ' -------------------------------------------------------------
    ' - API_KEY: chave de acesso ao OpenWeatherMap (grátis).
    ' - CIDADE: define a cidade consultada.
    ' - LIMITE_FRIO: define a temperatura em °C que aciona o alerta.
    ' =============================================================
    Private Const API_KEY As String = "1ce0ef314a850236582df0e8ca5045b8"
    Private Const CIDADE As String = "Diadema,BR"
    Private Const LIMITE_FRIO As Double = 15.0


    ' =============================================================
    ' EVENTO: Page_Loaded
    ' -------------------------------------------------------------
    ' Executado automaticamente quando o Dashboard é aberto.
    ' 1. Busca status do modo alerta no banco.
    ' 2. Mostra os indicadores de estoque e vulneráveis.
    ' 3. Atualiza o widget de clima atual e a previsão de 5 dias.
    ' =============================================================
    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Try
            isModoAlertaAtivo = DataAccess.GetModoAlertaStatus()
            CarregarIndicadores()
            Await CarregarClima()
            Await CarregarPrevisaoFutura()
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro fatal ao carregar o Dashboard: {0}", ex.Message),
                            "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    ' =============================================================
    ' MÉTODO: CarregarIndicadores
    ' -------------------------------------------------------------
    ' Busca no banco o total de peças essenciais e de pessoas
    ' vulneráveis, e atualiza os textos dos cards na tela.
    ' =============================================================
    Private Sub CarregarIndicadores()
        Dim totalPecas As Integer = 0
        Dim totalVulneraveis As Integer = 0

        Try
            totalPecas = DataAccess.GetTotalPecasEssenciaisEmEstoque()
            totalVulneraveis = DataAccess.GetTotalPessoasVulneraveis()
        Catch ex As Exception
            MessageBox.Show(String.Format("Não foi possível carregar os indicadores: {0}", ex.Message),
                            "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try

        ' Atualiza a interface
        txtTotalPecasEssenciais.Text = totalPecas.ToString()
        txtTotalPessoasVulneraveis.Text = totalVulneraveis.ToString()

        ' Atualiza o card de status do modo alerta
        AtualizarStatusModoAlerta()
    End Sub


    ' =============================================================
    ' MÉTODO: CarregarClima
    ' -------------------------------------------------------------
    ' Faz a chamada para a API do OpenWeatherMap e atualiza o
    ' widget com os dados de temperatura, vento, umidade e ícone.
    ' =============================================================
    Private Async Function CarregarClima() As Task
        ' Monta o endereço da API com cidade, chave e idioma
        Dim url As String = String.Format(
        "https://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=metric&lang=pt_br",
        CIDADE, API_KEY)

        Try
            ' Faz a requisição e aguarda a resposta da API
            Dim response As HttpResponseMessage = Await httpClient.GetAsync(url)

            ' Se a resposta for OK (código 200)
            If response.IsSuccessStatusCode Then
                ' Lê o texto JSON e converte para o objeto WeatherResponse
                Dim jsonString As String = Await response.Content.ReadAsStringAsync()
                Dim clima As WeatherResponse = JsonConvert.DeserializeObject(Of WeatherResponse)(jsonString)

                ' Atualiza os campos visuais da tela
                txtTemperatura.Text = String.Format("{0}°C", Math.Round(clima.Main.Temp))
                txtCondicao.Text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clima.Weather(0).Description)
                txtCidadeClima.Text = clima.Name
                txtUmidade.Text = String.Format("{0}%", clima.Main.Humidity)

                ' Conversão da velocidade do vento (m/s → km/h)
                Dim ventoKmH = Math.Round(clima.Wind.Speed * 3.6)
                txtVento.Text = String.Format("{0} km/h", ventoKmH)

                ' Carrega o ícone do clima (ex: sol, nuvem, chuva)
                Dim iconUrl As String = String.Format("https://openweathermap.org/img/wn/{0}@2x.png", clima.Weather(0).Icon)
                imgIconeClima.Source = New BitmapImage(New Uri(iconUrl))

                ' =========================================================
                ' NOVA LÓGICA DOS BOTÕES (ATIVAR / DESATIVAR)
                ' =========================================================
                If isModoAlertaAtivo Then
                    ' Se o alerta JÁ está ativo:
                    ' Mostra o botão "Desativar" e esconde o "Ativar"
                    btnDesativarModoAlerta.Visibility = Visibility.Visible
                    btnAtivarModoAlerta.Visibility = Visibility.Collapsed

                ElseIf clima.Main.Temp < LIMITE_FRIO Then
                    ' Se NÃO está ativo, mas está FRIO (abaixo de 15°C):
                    ' Mostra o botão "Ativar" e esconde o "Desativar"
                    btnAtivarModoAlerta.Visibility = Visibility.Visible
                    btnDesativarModoAlerta.Visibility = Visibility.Collapsed

                Else
                    ' Se NÃO está ativo e NÃO está frio:
                    ' Esconde os dois botões
                    btnAtivarModoAlerta.Visibility = Visibility.Collapsed
                    btnDesativarModoAlerta.Visibility = Visibility.Collapsed
                End If
                ' =========================================================

            Else
                ' Caso a chave da API seja inválida ou a API esteja inativa
                txtCondicao.Text = "API Key inválida ou aguardando ativação."
                txtTemperatura.Text = "--"
            End If

        Catch ex As Exception
            txtCondicao.Text = "Falha na API. Verifique o Firewall ou Internet."
        End Try
    End Function


    ' =============================================================
    ' MÉTODO: CarregarPrevisaoFutura
    ' -------------------------------------------------------------
    ' Busca a previsão de 5 dias futuros (API diferente) e
    ' agrupa por data, exibindo apenas 1 previsão por dia.
    ' =============================================================
    Private Async Function CarregarPrevisaoFutura() As Task
        Dim url As String = String.Format(
            "https://api.openweathermap.org/data/2.5/forecast?q={0}&appid={1}&units=metric&lang=pt_br",
            CIDADE, API_KEY)

        Try
            Dim response As HttpResponseMessage = Await httpClient.GetAsync(url)

            If response.IsSuccessStatusCode Then
                Dim jsonString As String = Await response.Content.ReadAsStringAsync()
                Dim previsao As ForecastResponse = JsonConvert.DeserializeObject(Of ForecastResponse)(jsonString)

                ' Agrupa previsões por dia (a API traz de 3 em 3 horas)
                Dim previsaoDiaria = previsao.List _
                    .GroupBy(Function(f) DateTime.Parse(f.DtTxt).Date) _
                    .Select(Function(dia)
                                ' Pega um horário fixo (15h) para representar o dia
                                Dim itemIcone = dia.FirstOrDefault(Function(d) DateTime.Parse(d.DtTxt).Hour = 15)
                                If itemIcone Is Nothing Then itemIcone = dia.First()

                                ' Cria o objeto PrevisaoDia com dados de temperatura e ícone
                                Return New PrevisaoDia With {
                                    .DiaSemana = dia.Key.ToString("ddd", New System.Globalization.CultureInfo("pt-BR")),
                                    .MaxTemp = String.Format("{0}°", Math.Round(dia.Max(Function(d) d.Main.TempMax))),
                                    .MinTemp = String.Format("{0}°", Math.Round(dia.Min(Function(d) d.Main.TempMin))),
                                    .IconeUrl = String.Format("https://openweathermap.org/img/wn/{0}@2x.png", itemIcone.Weather(0).Icon)
                                }
                            End Function) _
                    .ToList()

                ' Exibe apenas os próximos 5 dias
                ListViewPrevisao.ItemsSource = previsaoDiaria.Skip(1).Take(5)
            End If

        Catch ex As Exception
            ' Silencia o erro — não impede o resto do sistema
        End Try
    End Function


    ' =============================================================
    ' BOTÃO: Ativar Modo Alerta
    ' -------------------------------------------------------------
    ' Ativa manualmente o modo de alerta de frio e salva o status
    ' no banco de dados (RF17). Depois atualiza o painel.
    ' =============================================================
    Private Async Sub BtnAtivarModoAlerta_Click(sender As Object, e As RoutedEventArgs) Handles btnAtivarModoAlerta.Click
        Try
            ' Atualiza o status no banco
            DataAccess.SetModoAlertaStatus(True)
            isModoAlertaAtivo = True

            ' Atualiza a cor e texto do card
            AtualizarStatusModoAlerta()

            ' Oculta o botão e atualiza os dados do clima
            btnAtivarModoAlerta.Visibility = Visibility.Collapsed
            Await CarregarClima()

            ' Mensagem explicando a ativação
            MessageBox.Show("Modo de Alerta de Frio ATIVADO." & vbCrLf &
                            "A retirada de peças essenciais por pontos está bloqueada.",
                            "Sistema Ativado",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
        Catch ex As Exception
            MessageBox.Show(String.Format("Erro ao ativar o Modo de Alerta: {0}", ex.Message),
                            "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' =============================================================
    ' BOTÃO: Desativar Modo Alerta
    ' =============================================================
    Private Async Sub BtnDesativarModoAlerta_Click(sender As Object, e As RoutedEventArgs) Handles btnDesativarModoAlerta.Click
        Try
            ' 1. Atualiza no banco para "INATIVO" (False)
            DataAccess.SetModoAlertaStatus(False)
            isModoAlertaAtivo = False

            ' 2. Atualiza o visual (Texto e Cores)
            AtualizarStatusModoAlerta()

            ' 3. Esconde este botão e recarrega o clima para ver se o botão de ativar deve voltar
            btnDesativarModoAlerta.Visibility = Visibility.Collapsed
            Await CarregarClima()

            MessageBox.Show("Modo de Alerta DESATIVADO. O sistema voltou ao normal.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            MessageBox.Show("Erro ao desativar: " & ex.Message)
        End Try
    End Sub


    ' =============================================================
    ' MÉTODO: AtualizarStatusModoAlerta
    ' -------------------------------------------------------------
    ' Muda o texto e a cor do indicador de status do alerta
    ' conforme o valor atual armazenado em isModoAlertaAtivo.
    ' =============================================================
    Private Sub AtualizarStatusModoAlerta()
        If isModoAlertaAtivo Then
            txtStatusModoAlerta.Text = "ATIVO"
            txtStatusModoAlerta.Foreground = CType(Application.Current.Resources("CorAcaoPrimaria"), SolidColorBrush)
        Else
            txtStatusModoAlerta.Text = "INATIVO"
            txtStatusModoAlerta.Foreground = CType(Application.Current.Resources("CorAcaoPerigo"), SolidColorBrush)
        End If
    End Sub

End Class
