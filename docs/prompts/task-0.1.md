# Prompt — Task 0.1

- **Data:** 2026-09-13
- **Task:** 0.1 — Scaffold da solution .NET (Clean Architecture)
- **Branch:** `feat/0-1-dotnet-scaffold`
- **Estado do ROADMAP:** nenhuma task concluída; 0.1 é a primeira pendente

---

## Prompt

```text
Task: 0.1 — Scaffold da solution .NET (Clean Architecture, 4 projetos + testes)

LEIA ANTES DE COMEÇAR:
1. docs/spec.md — seções 3.2 (stack backend), 6.1 (estrutura do projeto: FONTE DA
   VERDADE desta task), 12.1 (pré-requisitos de ambiente), 13.1 a 13.4 (convenções,
   git workflow, lint, DoD) e 13.2.1.1 (workflow autônomo)
2. .zcode/skills/pre-pr-check/SKILL.md
3. ROADMAP.md (veja que 0.1 está pendente)
4. AGENTS.md (fluxo autônomo de Git)
(As skills add-feature e ui-component NÃO se aplicam — task de scaffolding puro,
sem feature e sem UI. Não as siga aqui.)

DEPENDÊNCIAS DE CÓDIGO:
- Nenhuma. Esta é a primeira task do projeto — o repo contém apenas docs, skills,
  .gitignore e README stub.
- O esqueleto criado aqui será a referência de estrutura para todas as tasks 1.x+.
- Já existem tracked no repo: .gitignore (template Visual Studio) e README.md
  (stub "# Clinic SaaS"). COMPLETE/AJUSTE — não recrie do zero.

BRANCH: feat/0-1-dotnet-scaffold

FLUXO AUTÔNOMO:
Siga o fluxo descrito no AGENTS.md — criar branch, implementar,
testar, commitar, pushar, abrir PR via gh, marcar [x] no ROADMAP.

ESCOPO RÍGIDO:
Implemente estritamente o que está descrito na task 0.1:
- HealthBr.sln na raiz do repo
- src/HealthBr.Domain, src/HealthBr.Application, src/HealthBr.Infrastructure
  (classlib) e src/HealthBr.Api (webapi), .NET 10, com referências de projeto
  conforme spec 6.1: Api → Application e Infrastructure; Infrastructure →
  Application; Application → Domain; Domain não referencia ninguém
- tests/HealthBr.UnitTests e tests/HealthBr.IntegrationTests (xUnit) — fazem
  parte da estrutura da seção 6.1
- iac/ apenas como estrutura de pasta (sem arquivos .bicep — isso é task 7.1)
- .gitignore: completar o existente com appsettings.Development.json,
  appsettings.*.Local.json, .env.local e dist/ (spec 13.2.1)
- .editorconfig com regras Microsoft (spec 13.3)
- README.md inicial real (PT-BR): visão do projeto, stack, como rodar,
  links para docs/spec.md e ROADMAP.md
- Commit tipo "chore:" + tag anotada v0.0.1 (detalhes em ATENÇÃO ESPECIAL)
Features extras vão para docs/TODO.md (crie se não existir).

ATENÇÃO ESPECIAL A:
- Regra de dependência (spec 6.1): Application NÃO referencia EF Core nem
  ASP.NET Core — nem packages nem ProjectReference. Domain não referencia
  ninguém. NÃO instale NuGet de stack (EF Core, FluentValidation, JWT,
  NSubstitute, Testcontainers...) nesta task — chegam com as tasks 1.x.
- Fora do escopo desta task: pasta frontend/ (task 3.2), conteúdo de .bicep
  (task 7.1), docs/architecture.md e docs/api-contracts.md (listados na seção
  6.1 mas não pedidos na task), user-secrets (documentado na task 7.3).
- Ambiente: confirme `dotnet --version` = 10.x ANTES de criar os projetos.
  E ANTES de `git checkout -b`, rode `git status` — se houver mudanças não
  commitadas em main que você não criou (há 5 conhecidas: AGENTS.md,
  ROADMAP.md, docs/spec.md e 2 skills), PARE e pergunte ao usuário.
- Tag inicial: após o commit "docs: mark task 0.1 as done", crie a tag anotada
  v0.0.1 no HEAD da branch e faça `git push origin v0.0.1`. Mencione no corpo
  do PR que a tag aponta para o commit da branch (pré-squash-merge) e que o
  usuário pode re-tagar no main após o merge se preferir.
- Qualidade: Nullable e ImplicitUsings enable nos 4 projetos; remova templates
  de exemplo (WeatherForecast, UnitTest1.cs, appsettings com segredos) e
  garanta `dotnet build` sem warnings, `dotnet format --verify-no-changes`
  limpo e `dotnet test` verde com no mínimo 1 teste trivial por projeto de
  teste (Testcontainers entra em tasks futuras).

PARE E PERGUNTE SE:
- Testes falharem 3x consecutivas
- Encontrar ambiguidade não coberta pelo spec
- Precisar decidir algo que afeta segurança, auth, ou timezone
- Identificar necessidade de feature fora do escopo da task
- Faltar configuração de ambiente (auth do git, gh CLI, .NET SDK 10, Docker)

NÃO FAÇA:
- Merge do PR (apenas o usuário)
- Commit em main direto
- Force-push para main
- Commit de segredos (.env, appsettings.Development.json)
- Adicionar features extras além do escopo

QUANDO TERMINAR, REPORTE:
- URL do PR criado
- Resumo das decisões tomadas (3-5 bullets)
- Dívidas técnicas ou dúvidas que ficaram
- Próxima task sugerida do ROADMAP
```

---

## Notas do planejador

### Por que essas seções do spec
- **6.1** é a fonte da verdade da task (estrutura de pastas e regras de dependência).
- **13.1–13.4 + 13.2.1.1** materializam convenções que o scaffold precisa embody: `.editorconfig` (13.3), branches/commits/gitignore (13.2/13.2.1), DoD (13.4) e o fluxo autônomo (13.2.1.1).
- **3.2 e 12.1** dão contexto de stack/SDK apenas para escolha de templates — sem instalar packages (isso é das tasks 1.x).

### Descobertas do estado atual (impactaram o prompt)
- `.gitignore` e `README.md` **já existem e estão tracked** no commit inicial → prompt manda completar/ajustar, não criar.
- O `.gitignore` atual (template VS) **não cobre** `appsettings.Development.json`, `appsettings.*.Local.json`, `.env.local` nem `dist/` — lacunas reais vs spec 13.2.1 que esta task fecha.
- Skills `add-feature`/`ui-component` não se aplicam (scaffolding sem feature/UI); deixei explícito para a executora não perder tempo nem seguir passos de feature.

### Decisões que tomei (podem ser revertidas)
1. **Tag inicial = `v0.0.1`** — o spec só nomeia `v0.1.0` (fim do M3) e `v1.0.0` (fim do M8); "tag inicial" da task não nomeia. Se preferir outro nome ou tag só pós-merge, avisar que eu regenero o prompt.
2. **"4 projetos" = projetos de src; os 2 projetos de teste fazem parte da "estrutura de pastas" da seção 6.1** — sem eles, o `dotnet test` do DoD não teria o que rodar.
3. **Commit tipo `chore:`** (scaffold, não feature).

### Riscos que enxerguei
- Executora criar a solution com defaults e ferir a regra de dependência (ex.: Application referenciando ASP.NET) — coberto no bullet 1.
- Scope creep: instalar pacotes da stack antecipado, criar frontend/, bicep ou docs extras — coberto nos bullets 1–2.
- Tag apontar para commit pré-squash (aceitável/auditável, mas deve ser dito no PR) — coberto no bullet da tag.

### Sugestões de acompanhamento (revisão do PR)
- Gráfico de referências: `dotnet list src/HealthBr.sln reference` deve mostrar Api→Application+Infrastructure, Infrastructure→Application, Application→Domain, Domain→ninguém.
- Diff do `.gitignore` deve conter as 4 entradas novas.
- `.sln` com os 6 projetos; `dotnet test` verde; nenhum package NuGet além dos templates.
- Tag `v0.0.1` pushed e mencionada no PR.

### Antes de despachar a executora
⚠️ Commitar as 5 mudanças pendentes em `main` (AGENTS.md, ROADMAP.md, docs/spec.md, 2 skills) — senão a executora vai parar no checkpoint de `git status` (comportamento correto, mas desperdiça a sessão).
