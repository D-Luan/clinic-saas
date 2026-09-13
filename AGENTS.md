# AGENTS.md — Contexto permanente do projeto

> Este arquivo é lido automaticamente pelo agente em toda sessão. Mantenha atualizado.

## Projeto

HealthBr — SaaS multi-tenant de gestão de clínicas (MVP). Projeto de teste de capacidade de agente de IA (zcode + GLM 5.3). Não é produto comercial.

## Stack

- **Backend:** .NET 10, Clean Architecture (Domain / Application / Infrastructure / Api), EF Core 10, FluentValidation, JWT + refresh token, xUnit + NSubstitute + Testcontainers.
- **Frontend:** React 19 + TypeScript (strict) + Vite, TanStack Query, React Hook Form + Zod, Zustand, Tailwind v4 + shadcn/ui, React Router v7, sonner, lucide-react, date-fns, Vitest + RTL.
- **Infra:** Azure SQL Serverless, App Service Linux, Key Vault, Application Insights. Bicep para IaC. GitHub Actions para CI/CD.

## Onde encontrar coisas

- **Spec técnico:** `docs/spec.md` (leia antes de qualquer implementação)
- **Roadmap:** `ROADMAP.md` (siga task por task, marque `[x]` ao concluir)
- **Skills:** `.zcode/skills/` (3 skills: `add-feature`, `pre-pr-check`, `ui-component`)

## Como rodar localmente

- Backend: `cd src/HealthBr.Api && dotnet run` (Swagger em `/swagger`)
- Frontend: `cd frontend && pnpm dev` (porta 5173)
- Testes backend: `dotnet test`
- Testes frontend: `cd frontend && pnpm test`
- Lint: `dotnet format --verify-no-changes && cd frontend && pnpm lint`
- Migration: `dotnet ef database update --project src/HealthBr.Infrastructure --startup-project src/HealthBr.Api`

## Princípios de trabalho

1. **Uma task por vez.** Não adiante. Veja ROADMAP.md.
2. **Siga o spec literalmente.** Desvios precisam de justificativa em PR.
3. **Quando em dúvida, pergunte.** Não invente comportamento.
4. **Toda funcionalidade vem com teste.** Backend → teste de integração. Frontend → teste de unidade.
5. **Mantenha o escopo.** Features fora da seção 2 do spec não devem ser implementadas.
6. **UI sem anti-padrões.** A seção 8 do spec é mandatória.

## O que NÃO fazer

- ❌ Commits direto em `main` — sempre feature branch + PR (squash and merge)
- ❌ DataAnnotations (use FluentValidation)
- ❌ axios (use `fetch` via `shared/lib/api.ts`)
- ❌ Redux (use Zustand)
- ❌ localStorage para tokens
- ❌ `rounded-2xl`, emojis, gradientes roxo-rosa, glassmorphism
- ❌ `console.log`, `debugger`, `TODO` sem issue
- ❌ Commitar segredos em `appsettings.json` ou `.env`
- ❌ Cores Tailwind default (`text-blue-500`) — use tokens CSS da seção 8.3
- ❌ Moq (use NSubstitute) / Serilog / rich-text / Upload de arquivos (fora do MVP, seção 2)

## Convenções

- **Commits:** conventional commits em inglês (`feat:`, `fix:`, `chore:`). Body referencia task: `Refs: task 3.1`.
- **Branches:** `feat/<task-id>-<short-desc>`, `fix/<task-id>-<short-desc>`, `chore/<short-desc>`.
- **C#:** PascalCase público, `_camelCase` privado, interfaces com prefixo `I`.
- **TS:** camelCase variáveis, PascalCase tipos/componentes. Arquivos de componente `.tsx`, demais `.ts`.
- **UI:** PT-BR. **Código:** inglês. **Fuso:** `America/Sao_Paulo` no frontend; UTC no backend.

## Antes de começar qualquer task

1. Leia a seção relevante do `docs/spec.md`.
2. Confirme que entendeu a task no `ROADMAP.md`.
3. Crie uma branch: `git checkout -b feat/<task-id>-<short-desc>`.
4. Implemente seguindo a skill `.zcode/skills/add-feature/SKILL.md` se aplicável.
5. Rode o checklist da skill `.zcode/skills/pre-pr-check/SKILL.md` antes de commitar.
6. Abra PR referenciando a task.
