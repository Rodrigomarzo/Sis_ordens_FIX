# Sistema de Ordens com FIX Protocol (OrderGenerator e OrderAccumulator)

Este projeto consiste em duas aplicações .NET, `OrderGenerator` e `OrderAccumulator`, que se comunicam utilizando o protocolo FIX (Financial Information eXchange) na versão 4.4, através da biblioteca QuickFIX/n.

## Funcionalidades

### 1. OrderGenerator

-   **Interface Web:** Uma aplicação Blazor Server que apresenta um formulário para a criação e envio de novas ordens (`NewOrderSingle`).
-   **Criação de Ordens:** O formulário permite ao usuário especificar:
    -   **Símbolo:** `PETR4`, `VALE3` ou `VIIA4`.
    -   **Lado:** Compra ou Venda.
    -   **Quantidade:** Valor inteiro, positivo e menor que 100.000.
    -   **Preço:** Valor decimal, positivo, múltiplo de 0.01 e menor que 1.000.
-   **Comunicação FIX:** Envia a ordem para o `OrderAccumulator` e exibe a resposta (aceita ou rejeitada) na tela.

### 2. OrderAccumulator

-   **Recepção de Ordens:** Uma aplicação de console que atua como um servidor FIX (acceptor), recebendo as ordens enviadas pelo `OrderGenerator`.
-   **Cálculo de Exposição Financeira:** Para cada símbolo, calcula a exposição da seguinte forma:
    -   `Exposição = Σ(preço * quantidade) de compras - Σ(preço * quantidade) de vendas`
-   **Gerenciamento de Limite:** Impõe um limite de exposição financeira de **R$ 100.000.000,00** (cem milhões) por símbolo.
    -   **Aceite:** Se uma nova ordem não viola o limite, ela é aceita (`ExecType=New`) e sua exposição é contabilizada.
    -   **Rejeição:** Se uma nova ordem faz com que a exposição (em valor absoluto) ultrapasse o limite, ela é rejeitada (`ExecType=Rejected`) e não afeta o cálculo.
-   **Logs:** Exibe logs detalhados no console sobre ordens recebidas, status (aceita/rejeitada) e a nova exposição do símbolo.

## Tecnologias Utilizadas

-   **Linguagem:** C#
-   **Plataforma:** .NET 8 / .NET 9
-   **Frontend:** ASP.NET Core Blazor Server
-   **Protocolo:** FIX 4.4 com a biblioteca **QuickFIX/n**

## Como Executar o Projeto

### Pré-requisitos

-   [.NET SDK](https://dotnet.microsoft.com/download) (versão 8.0 ou superior)

### Passos

1.  **Clone o Repositório:**
    ```bash
    git clone <url-do-seu-repositorio>
    cd <pasta-do-projeto>
    ```

2.  **Inicie o OrderAccumulator (Servidor):**
    Abra um terminal e execute o comando abaixo. Este terminal precisa ficar aberto.
    ```bash
    dotnet run --project OrderAccumulator/OrderAccumulator.csproj
    ```
    Você verá logs indicando que o servidor está aguardando conexões.

3.  **Inicie o OrderGenerator (Cliente Web):**
    Abra um **novo** terminal e execute o seguinte comando:
    ```bash
    dotnet run --project OrderGenerator/OrderGenerator.csproj
    ```

4.  **Acesse a Aplicação:**
    Após a compilação, o terminal do `OrderGenerator` exibirá uma URL, geralmente `http://localhost:5046` ou similar. Abra essa URL no seu navegador. A aplicação estará pronta para uso assim que o console do `OrderGenerator` exibir a mensagem "Logon bem-sucedido".

## Estrutura do Projeto

-   `OrderSystem.sln`: O arquivo da solução que agrupa os dois projetos.
-   `/OrderAccumulator`: Projeto de console C# que funciona como o servidor FIX.
    -   `quickfix-server.cfg`: Arquivo de configuração da sessão FIX para o servidor.
-   `/OrderGenerator`: Projeto Blazor Server C# que funciona como o cliente FIX.
    -   `quickfix-client.cfg`: Arquivo de configuração da sessão FIX para o cliente.
-   `FIX44.xml`: Dicionário de dados do FIX 4.4, utilizado por ambas as aplicações. 