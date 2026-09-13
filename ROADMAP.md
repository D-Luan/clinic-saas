# Roadmap de Execução — HealthBr

> Cada task deve ser completável em 30-60 min de trabalho do agente.
> Marque `[x]` quando concluir uma task. Não pule tasks.
> Antes de iniciar uma task, leia a seção do `docs/spec.md` referenciada.

## Princípios de execução

- Uma task por vez. Não adiante.
- Sempre rodar testes e lint antes de marcar task como done.
- Em caso de ambiguidade, parar e perguntar ao usuário.
- Toda task que toca backend deve incluir teste de integração.
- Toda task que toca frontend deve incluir teste de unidade (RTL).
- Commits em inglês, conventional commits (`feat:`, `fix:`, `chore:`).
- **Fluxo autônomo:** para cada task, criar branch → implementar → testar → commit → push → abrir PR via `gh pr create` → marcar `[x]` nesta lista → commit → push. **NÃO** fazer merge do PR (o usuário fará).
- **Parada obrigatória:** se testes falharem 3x consecutivas, ou houver ambiguidade no spec, ou decisão de segurança/auth/timezone, PARE e pergunte ao usuário.
- **Escopo rígido:** implementar apenas o que está descrito na task. Features extras vão para `docs/TODO.md`.

## Milestone 0 — Setup do repositório (1 task)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [x] **0.1** Criar solution .NET, 4 projetos (Domain, Application, Infrastructure, Api), solution file, `.gitignore`, `.editorconfig`, `README.md` inicial, estrutura de pastas do spec seção 6.1. Primeiro commit + tag inicial. **Spec ref: 6.1, 13**

## Milestone 1 — Backend base (4 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [x] **1.1** BaseEntity + enums (`AppointmentStatus`, `UserRole`) + interfaces de repositório. **Spec ref: 4**
- [x] **1.2** EF Core DbContext + mappings de todas as entidades + Global Query Filters (`TenantId`, `IsDeleted`) + índice filtrado UNIQUE `(DoctorId, StartTime)` + primeira migration. **Spec ref: 3.2, 4, 9**
- [x] **1.3** Auth: login + JWT (15 min) + refresh token (7 dias, hash no DB, cookie httpOnly `Path=/api/v1/auth`) + `ITenantContext` (Scoped) + endpoints `POST /api/v1/auth/login` e `POST /api/v1/auth/refresh`. **Spec ref: 5.1, 7.1, 9, 15**
- [ ] **1.4** Middleware de erro global + ProblemDetails (RFC 7807 com `errorCode`, `message`, `details?`) + health checks `/health` e `/health/ready` + headers de segurança + Swagger em dev/staging. **Spec ref: 3.2, 9, 10.1, 15.4**

## Milestone 2 — Provisionamento e IAM (2 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **2.1** Endpoint público `POST /api/v1/tenants` (cria Tenant + primeiro User Doctor em transação única; unicidade global de `adminEmail`). **Spec ref: 5.1**
- [ ] **2.2** Endpoint `GET /api/v1/me` + policies de autorização (`DoctorOnly`) + endpoints `GET/POST /api/v1/users` (apenas Doctor) + `POST /api/v1/auth/logout`. **Spec ref: 5.2, 9**

## Milestone 3 — Feature: Patients end-to-end (3 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **3.1** Backend: CRUD completo de Patient + busca paginada (`Name` contains / `Phone` exact E.164) + testes de integração cobrindo CRUD, auth, role, isolamento de tenant. **Spec ref: 4.2, 5.2, 9**
- [ ] **3.2** Frontend setup: Vite + React 19 + TS strict + Tailwind v4 + shadcn/ui + React Router v7 + TanStack Query + Zustand + RHF+Zod + sonner + lucide-react + date-fns + Inter/JetBrains Mono. Estrutura de pastas do spec seção 6.2, tokens CSS da seção 8.3. Auth flow (login + refresh transparente + 401 handling + rotas protegidas via loader). **Spec ref: 3.3, 6.2, 7, 8**
- [ ] **3.3** Frontend: tela de pacientes (DataTable com busca debounced 400ms + paginação + Sheet de create/edit + delete com AlertDialog). Sem anti-padrões da seção 8.2. **Spec ref: 7, 8**

## Milestone 4 — Feature: Agenda e DoctorSchedule (4 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **4.1** Backend: CRUD de DoctorSchedule (`GET/PUT /api/v1/doctors/{id}/schedule`) com validação `EndTime > StartTime`, `TimeOnly` em hora local (sem conversão UTC). **Spec ref: 4.3, 5.3**
- [ ] **4.2** Backend: `POST /api/v1/appointments` com validação de disponibilidade (contida em DoctorSchedule do dia) + checagem de sobreposição em transação + status inicial `Scheduled`. **Spec ref: 4.4, 5.3**
- [ ] **4.3** Backend: `GET /api/v1/appointments?date&doctorId` + `GET /{id}` + `PATCH /status` com máquina de transições (409 `INVALID_STATUS_TRANSITION`) + `PATCH /payment` (só quando `Completed`; estorno só Doctor). **Spec ref: 5.4, 5.6, 9**
- [ ] **4.4** Frontend: tela de agenda (visão de dia, colunas por médico, status badges com cor semântica, dialog de criar agendamento com conversão `America/Sao_Paulo` ↔ UTC). **Spec ref: 5.4, 7, 8**

## Milestone 5 — Feature: ClinicalNotes (2 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **5.1** Backend: `POST/GET /api/v1/appointments/{id}/notes` (apenas Doctor). Notas imutáveis — sem update, sem soft delete. **Spec ref: 4.5, 5.2, 9**
- [ ] **5.2** Frontend: dentro do drawer de appointment, timeline de notas + formulário de nova nota (visível apenas para Doctor). **Spec ref: 4.5, 7, 8**

## Milestone 6 — Dashboard (2 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **6.1** Backend: `GET /api/v1/dashboard/summary` retornando métricas (consultas por status hoje/mês no fuso `America/Sao_Paulo`, receita dia/mês por `PaidAt`, próximos 5 do médico logado se Doctor). **Spec ref: 5.7, 9**
- [ ] **6.2** Frontend: tela de dashboard com cards de KPI + tabela de próximos agendamentos + botão "Atualizar" (refetch manual). **Spec ref: 5.7, 7, 8**

## Milestone 7 — Infraestrutura e CI/CD (3 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **7.1** Bicep: `main.bicep` + modules (app-service, sql-server, key-vault) + `parameters.dev.json` + outputs (`backendUrl`, `sqlServerFqdn`, `keyVaultUri`). **Spec ref: 11.1, 11.4**
- [ ] **7.2** GitHub Actions: `deploy.yml` (OIDC login, build, test, publish, deploy backend+frontend em `wwwroot/`, migrate, smoke test `/health/ready` com retry 5x/10s) + `destroy.yml` (manual). **Spec ref: 11.3**
- [ ] **7.3** Setup local: `dotnet user-secrets` config documentada + seed script (1 tenant, 2 médicos, 1 recep, 10 pacientes, 50 agendamentos, notas em 20 consultas completed) + Makefile. **Spec ref: 12**

## Milestone 8 — Polish e validação final (2 tasks)

<!-- Para cada task desta milestone, siga o fluxo autônomo de Git descrito em AGENTS.md. Abra um PR por task. Não faça merge — aguarde revisão do usuário. -->

- [ ] **8.1** Auditoria de anti-padrões: revisar todas as telas contra a seção 8.2 do spec. Corrigir desvios. **Spec ref: 8**
- [ ] **8.2** Smoke test end-to-end manual: provisionar tenant, criar paciente, agendar, fazer consulta, adicionar nota, marcar pago, ver dashboard. Documentar em `docs/smoke-test.md`. Tag `v1.0.0`.

## Notas

- Tasks em milestone mais baixo não bloqueiam apenas tarefas dependentes dentro do mesmo milestone.
- Se uma task revelar ambiguidade no spec, pare e pergunte antes de continuar.
- Ao final de cada milestone, rode `dotnet test` e `pnpm test` e garanta que passam.
- Tags: `v0.1.0` ao final do Milestone 3 (primeira feature end-to-end), `v1.0.0` ao final do Milestone 8 (MVP completo). **Spec ref: 13.2.1**
