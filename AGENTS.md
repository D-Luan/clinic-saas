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
7. **Fluxo autônomo com salvaguardas.** Para cada task: crie branch, implemente, teste, commit, push, abra PR via `gh`. Pare e pergunte se: testes falharem 3x consecutivas, houver ambiguidade no spec, decisão de segurança/auth/timezone, ou necessidade de feature fora de escopo. **Nunca faça merge do PR** — merge é manual do usuário.

## O que NÃO fazer

- ❌ Fazer merge de PR (apenas o usuário faz, via GitHub)
- ❌ Force-push para `main`
- ❌ Commit direto em `main` (sempre feature branch + PR)
- ❌ Commitar segredos de qualquer tipo (tokens, senhas, chaves JWT, connection strings — inclusive em `appsettings.json` ou `.env`)
- ❌ Pular testes/hook com `--no-verify`
- ❌ Adicionar features extras além do escopo da task (anote em `docs/TODO.md`)
- ❌ Rebase interativo sem pedir
- ❌ Apagar branches remotas sem permissão
- ❌ Continuar tentando após 3 falhas consecutivas sem perguntar ao usuário
- ❌ DataAnnotations (use FluentValidation)
- ❌ axios (use `fetch` via `shared/lib/api.ts`)
- ❌ Redux (use Zustand)
- ❌ localStorage para tokens
- ❌ `rounded-2xl`, emojis, gradientes roxo-rosa, glassmorphism
- ❌ `console.log`, `debugger`, `TODO` sem issue
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
6. Siga o **Fluxo autônomo de Git** (seção abaixo) para commit, push e PR via `gh`.

## Fluxo autônomo de Git (uma task = uma sessão)

Para cada task do ROADMAP:

1. `git checkout main && git pull` (sincroniza)
2. `git checkout -b feat/<task-id>-<short-desc>` (ex.: `feat/3-1-patients-crud`)
3. Implemente a task seguindo spec + skill `add-feature`
4. Rode testes e lint:
   - `dotnet test` deve passar
   - `cd frontend && pnpm test` deve passar (se tocou frontend)
   - `dotnet format --verify-no-changes` deve estar limpo
   - `cd frontend && pnpm lint` deve estar limpo
5. Rode o checklist da skill `pre-pr-check`
6. Commit: `git commit -m "feat: <description>" -m "Refs: <task-id>"`
7. Push: `git push -u origin feat/<task-id>-<short-desc>`
8. Abra PR: `gh pr create --title "feat: <description>" --body "Closes task <task-id>.\n\n${resumo}" --base main`
9. Marque `[x]` no ROADMAP.md na task correspondente
10. Commit: `git commit -m "docs: mark task <task-id> as done"`
11. Push: `git push`
12. Reporte ao usuário: URL do PR, resumo das decisões, dúvidas e próxima task sugerida

O agente **não faz merge** — o PR aguarda revisão e merge manual do usuário.

### Momentos de parada obrigatória

PARE e pergunte ao usuário se:

- Testes falharem 3 vezes consecutivas na mesma task
- Encontrar ambiguidade não coberta pelo spec
- Precisar decidir algo que afeta segurança (auth, JWT, cookies, headers HTTP)
- Precisar decidir algo que afeta timezone ou multi-tenant isolation
- Identificar necessidade de feature fora do escopo da task atual
- Faltar configuração de ambiente (auth do git, gh CLI, secrets, user-secrets)
- O spec parecer contraditório em alguma parte

### Momentos em que pode continuar sozinho

- Escolha de nomes de variáveis seguindo convenção
- Organização interna de arquivos dentro da feature
- Escolha de qual componente shadcn/ui usar
- Detalhes de implementação dentro do escopo da task
- Pequenos ajustes de layout dentro do design system (spec seção 8)

Detalhes completos: `docs/spec.md` seções 13.2.1.1, 13.5 e 13.6.
