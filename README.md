# HealthBr

SaaS multi-tenant de gestão de clínicas (MVP): agenda multi-profissional, cadastro de pacientes, prontuário em texto livre e métricas financeiras/operacionais simplificadas.

> Projeto de teste de capacidade de agente de IA (zcode + GLM 5.3). Não é um produto comercial.

## Stack

- **Backend:** .NET 10 com Clean Architecture (Domain / Application / Infrastructure / Api), EF Core 10, FluentValidation, JWT + refresh token.
- **Testes:** xUnit (+ NSubstitute e Testcontainers, introduzidos nas tasks 1.x).
- **Frontend (planejado, task 3.2):** React 19 + TypeScript strict, Vite, TanStack Query, React Hook Form + Zod, Zustand, Tailwind v4 + shadcn/ui.
- **Infra (planejado, milestone 7):** Azure SQL Serverless, App Service Linux, Key Vault, Bicep, GitHub Actions.

## Estrutura

```text
/HealthBr.sln
  /src
    /HealthBr.Domain           # Entidades, enums, interfaces de repositório, value objects
    /HealthBr.Application      # Use cases (commands/queries), DTOs, validators
    /HealthBr.Infrastructure   # EF Core DbContext, mapeamentos, auth, serviços externos
    /HealthBr.Api              # Controllers, Program.cs, middlewares
  /tests
    /HealthBr.UnitTests        # Testes de Application/Domain
    /HealthBr.IntegrationTests # Testcontainers (a partir das tasks 1.x)
  /iac                         # Bicep (task 7.1)
  /docs                        # spec.md e demais documentação
```

Regras de dependência: `Api → Application → Domain`, `Infrastructure → Application → Domain`. `Domain` não referencia nada; `Application` não referencia EF Core nem ASP.NET Core.

## Como rodar

Pré-requisitos: .NET 10 SDK. (Node 22 + pnpm e Docker serão necessários a partir das tasks de frontend e integração.)

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/HealthBr.Api
```

A API sobe nas portas definidas em `src/HealthBr.Api/Properties/launchSettings.json` (perfil `http`: `localhost:5153`). Segredos locais (connection string, chave JWT, CORS) ficam em `dotnet user-secrets` — nunca em `appsettings` commitados (detalhes na task 7.3).

## Documentação

- Especificação técnica completa: [docs/spec.md](docs/spec.md)
- Roadmap de execução (task por task): [ROADMAP.md](ROADMAP.md)
