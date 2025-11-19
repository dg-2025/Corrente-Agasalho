<p align="center">
  <img src="https://img.icons8.com/fluency/96/winter.png" width="100" alt="Logo Corrente do Agasalho">
</p>

<h1 align="center">
  Corrente do Agasalho (Sistema de Gestão Social)
</h1>

<p align="center">
  <strong>Um sistema desktop robusto para gestão de doações, estoque e vulnerabilidade social. Desenvolvido em VB.NET com WPF, integrado a APIs de Clima/CEP e banco de dados em nuvem (AWS RDS).</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Language-VB.NET-5C2D91?logo=dotnet" alt="VB.NET">
  <img src="https://img.shields.io/badge/Frontend-WPF_(XAML)-512BD4?logo=windows" alt="WPF">
  <img src="https://img.shields.io/badge/Database-PostgreSQL_(AWS)-336791?logo=postgresql" alt="PostgreSQL">
  <img src="https://img.shields.io/badge/API-OpenWeatherMap-orange?logo=openweathermap" alt="Weather API">
  <img src="https://img.shields.io/badge/API-ViaCEP-yellow?logo=map" alt="ViaCEP">
  <img src="https://img.shields.io/badge/IDE-Visual_Studio_2022-5C2D91?logo=visualstudio" alt="VS 2022">
</p>

<p align="center">
  <a href="https://git.io/typing-svg">
    <img src="https://readme-typing-svg.herokuapp.com?color=%234A2BFF&center=true&vCenter=true&width=600&lines=Sistema+de+Logística+Social+Completo;VB.NET+%2B+WPF+%2B+PostgreSQL+Cloud;Gestão+de+Estoque+e+Pontos;Integração+com+APIs+de+Clima+e+CEP" alt="Typing SVG">
  </a>
</p>

---

## 🎞️ Demonstração Visual

O sistema conta com uma interface moderna, limpa e padronizada, desenvolvida em **WPF (Windows Presentation Foundation)**.

<table>
  <thead>
    <tr>
      <th align="center">Login Seguro</th>
      <th align="center">Dashboard & Clima</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td width="50%">
        <img src="./login.png" alt="Tela de Login" width="100%">
      </td>
      <td width="50%">
        <img src="./dashboard.png" alt="Dashboard com Clima" width="100%">
      </td>
    </tr>
  </tbody>
</table>

### Fluxo de Operação (Logística)
<p align="center">
  <img src="./registrar doação.png" width="48%" alt="Entrada de Doação">
  &nbsp;
  <img src="./registrar entrega.png" width="48%" alt="Saída e Entrega">
</p>

---

## 🎯 Sobre o Projeto

O **Corrente do Agasalho** não é apenas um CRUD. É um sistema de **Gamificação Social** onde doadores acumulam pontos e beneficiários podem retirar peças baseadas em regras de vulnerabilidade.

A infraestrutura foi desenhada para ser resiliente, com o banco de dados hospedado na **AWS (RDS)**, garantindo que os dados estejam seguros e acessíveis de qualquer estação de trabalho conectada.

---

## ✨ Funcionalidades (Features)

<details>
  <summary><strong>🌡️ 1. Inteligência Climática & Alerta de Frio</strong></summary>
  <br>
  <ul>
    <li>Integração com a <strong>OpenWeatherMap API</strong> para monitorar a temperatura de Diadema/SP em tempo real.</li>
    <li><strong>Modo Alerta de Frio:</strong> Se a temperatura cair abaixo de 15°C (ou ativado manualmente), o sistema entra em "Modo de Emergência".</li>
    <li><strong>Regra de Bloqueio:</strong> Durante o alerta, a troca de itens essenciais (casacos, cobertores) é <strong>bloqueada</strong> para usuários comuns, priorizando a doação para pessoas em situação de rua.</li>
  </ul>
  
  <p align="center">
    <img src="./dashboard.png" width="80%" alt="Dashboard Detalhado">
  </p>
</details>

<details>
  <summary><strong>📦 2. Logística, Estoque e Pontos</strong></summary>
  <br>
  <ul>
    <li><strong>Entrada (Doação):</strong> Cálculo automático de pontos baseado na categoria da peça. Uso de <code>Transaction SQL</code> para garantir integridade (só credita pontos se o estoque for atualizado).</li>
    <li><strong>Saída (Checkout):</strong> Carrinho de compras virtual. O sistema valida se o beneficiário tem saldo de pontos suficiente antes de liberar a peça.</li>
    <li><strong>Inventário:</strong> Filtros avançados (ICollectionView) para consultar o estoque por tamanho, categoria e status.</li>
  </ul>
  
  <p align="center">
     <img src="./inventario.png" width="80%" alt="Inventário">
  </p>
</details>

<details>
  <summary><strong>🛡️ 3. Segurança e Auditoria</strong></summary>
  <br>
  <ul>
    <li><strong>Autenticação:</strong> Senhas nunca são salvas em texto puro. Utilizamos <strong>Hash SHA256</strong>.</li>
    <li><strong>Auditoria Completa:</strong> O Admin pode rastrear todas as operações (Quem doou? Quem retirou? Quando?).</li>
    <li><strong>Estorno (Rollback):</strong> Se houver erro, o Admin pode estornar uma transação. O sistema devolve os pontos para o usuário e repõe o item no estoque automaticamente.</li>
  </ul>
  
  <p align="center">
     <img src="./auditoria.png" width="80%" alt="Tela de Auditoria">
  </p>
</details>

<details>
  <summary><strong>👥 4. Gestão de Pessoas e Endereços</strong></summary>
  <br>
  <ul>
    <li>Integração com a <strong>ViaCEP API</strong>: Preenchimento automático de endereço ao digitar o CEP.</li>
    <li>Histórico unificado: Visualize em uma única tela tudo o que a pessoa já doou ou recebeu.</li>
    <li>Monitoramento gráfico de vulnerabilidade por região.</li>
  </ul>
  
  <p align="center">
     <img src="./monitoramento.png" width="45%" alt="Gráficos">
     <img src="./gestão de pessoas.png" width="45%" alt="Cadastro">
  </p>
</details>

---

## 🛠️ Stack Tecnológico

| Área | Tecnologia | Descrição |
| :--- | :--- | :--- |
| **Frontend** | **WPF (XAML)** | Interface Desktop nativa, com Estilos (`Resources`) globais e responsividade. |
| **Backend** | **VB.NET (.NET 8)** | Lógica de negócio robusta, orientada a objetos. |
| **Database** | **PostgreSQL (AWS)** | Banco relacional hospedado na nuvem (Amazon RDS). |
| **Libs** | **Npgsql** | Driver de conexão de alta performance para Postgres. |
| **Libs** | **Newtonsoft.Json** | Serialização e Deserialização de dados das APIs. |
| **Libs** | **Extended.Wpf.Toolkit** | Componentes visuais avançados (Máscaras, Inputs). |

---

## 🏛️ Arquitetura de Dados (DataAccess)

O sistema utiliza uma classe centralizadora `DataAccess.vb` que gerencia todas as conexões.

* **Transações Atômicas:** Para operações financeiras (pontos) e de estoque, usamos `BeginTransaction`, `Commit` e `Rollback` para evitar dados corrompidos.
* **Segurança:** Proteção contra *SQL Injection* utilizando parâmetros tipados (`@Param`) em todas as queries.

---

## 👨‍💻 Desenvolvimento

<p align="center">
  Projeto desenvolvido com foco em Engenharia de Software e Impacto Social.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Status-Concluído-success?style=for-the-badge">
</p>
