# Sistema de Compra Programada de Ações

Projeto de exemplo desenvolvido para o desafio técnico da Itaú Corretora. O objetivo é implementar um sistema que permita a compra programada de uma carteira "Top Five" de ações, com distribuição proporcional entre clientes, rebalanceamentos fiscais e integração com Kafka, conforme descrito na documentação do desafio.

## Estrutura

```
/
|-- cotacoes/                  # Arquivos COTAHIST da B3
|-- src/                       # Código-fonte do sistema
|   |-- CompraProgramada.Api   # Web API (.NET Core)
|   |-- CompraProgramada.Application # Application layer (use cases)
|   |-- CompraProgramada.Domain      # Entidades de domínio
|   |-- CompraProgramada.Infrastructure # Persistência e infra
|-- tests/                     # Testes automatizados (xUnit)
|-- docker-compose.yml         # Kafka + MySQL
|-- README.md                  # Esta documentação
+-- ...
```

## Requisitos

- .NET SDK (6.0 ou superior) instalado localmente
- Docker e Docker Compose para subir Kafka e MySQL

> **Observação:** O ambiente de entrega não possui `dotnet` instalado, portanto os projetos são fornecidos como código. Para compilar e executar você deve executar os comandos abaixo em sua máquina.

## Instruções rápidas

1. Clone o repositório em sua máquina e navegue até a raiz.
2. Suba a infraestrutura com Docker Compose:
   ```
   docker-compose up -d
   ```
   Isso iniciará MySQL (porta 3306) e Kafka (porta 9092).

3. Crie os projetos .NET ou restaure os existentes:
   ```powershell
   cd src/CompraProgramada.Api
   dotnet restore
   dotnet build
   ```

4. Execute a API:
   ```
   dotnet run --project src/CompraProgramada.Api/CompraProgramada.Api.csproj
   ```
   A documentação Swagger estará disponível em `http://localhost:5000/swagger`.

5. Para executar testes:
   ```
   dotnet test
   ```

## Como o código está organizado

- **Domain:** contém as entidades e regras de negócio essenciais (Cliente, CestaTopFive, Custodia, etc.).
- **Application:** implementa casos de uso que coordenam ações entre camadas.
- **Infrastructure:** implementações de persistência (MySQL/EF Core), parser de COTAHIST e produtor Kafka.
- **Api:** controllers expõem os endpoints REST descritos nos contratos de API do desafio.

## Principais funcionalidades implementadas

- Adesão, saída e alteração de cliente via API
- Cadastro e histórico de cestas Top Five
- Parser de arquivos COTAHIST com busca por cotação de fechamento
- Serviço de motor de compra programada com cálculo de quantidades, lotes e fracionário
- Emissão de eventos de IR dedo-duro e IR venda para Kafka
- Estrutura de rebalanceamento (mudança de cesta)
- Cobertura de testes para regras de cálculo e parser

## Próximos passos

- Completar os repositórios EF Core e aplicação de migrações
- Implementar lógica de rebalanceamento por desvio de proporção
- Adicionar endpoints administrativos adicionais e validações completas
- Preencher a pasta `cotacoes/` com arquivos COTAHIST reais para testes

---

_As especificações completas das regras e exemplos estão disponíveis nos arquivos de documentação (`*.md`) deste repositório, conforme o enunciado original do desafio._
