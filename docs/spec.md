# Especificação Técnica: HealthBr — SaaS Gestão de Clínicas (MVP)

> **Status:** Pronto para desenvolvimento
> **Linguagem padrão:** Português (Brasil) em toda a UI e documentação
> **Fuso horário de referência:** `America/Sao_Paulo` (fixo para o MVP)

---

## 1. Visão Geral

Sistema multi-tenant projetado para digitalizar e otimizar a operação de consultórios e clínicas de pequeno e médio porte. O foco do MVP está na gestão de agenda, cadastro demográfico básico, registro clínico evolutivo e métricas financeiras/operacionais simplificadas.

Este projeto serve simultaneamente a dois propósitos: (a) entregar um MVP funcional de gestão clínica e (b) servir como benchmark para avaliar a capacidade de um agente de desenvolvimento assistido por IA (zcode + GLM 5.3) em um cenário fullstack realista. Por isso, decisões técnicas foram tomadas privilegiando clareza de especificação, convenções explícitas e rastreabilidade — facilitando a auditoria do que o agente produz.

---

## 2. Restrições e Limites de Escopo (O Corte)

### Incluso
- Gestão de pacientes (CRUD com busca)
- Agenda multi-profissional com validação de disponibilidade
- Prontuário em linha do tempo (texto livre, sem rich-text)
- Faturamento acoplado à consulta (marcação simples de "pago")
- Dashboard gerencial simplificado (atualização sob demanda)
- Provisionamento de tenant via endpoint público
- Autenticação com JWT + refresh token

### Fora do MVP
- Faturamento TISS/TUSS (planos de saúde)
- Prescrição eletrônica / ICP-Brasil
- Upload de arquivos ou anexos
- Agendamento online por pacientes (self-booking)
- Integrações de mensageria (WhatsApp/SMS/email)
- Atualização em tempo real via WebSockets/SignalR
- Recuperação de senha por email
- Confirmação de email no signup
- Auditoria/log de alterações por usuário
- Notificações in-app
- Internacionalização (i18n) — UI em PT-BR fixo
- Testes E2E (Playwright/Cypress)
- PWA / modo offline
- Métricas avançadas (lifetime value, no-show rate por médico, cohort)
- Rate limiting / throttling de API (pode ser adicionado via Azure API Management em futuro; fora do escopo do MVP)
- LGPD formal (anonimização automatizada, direito ao esquecimento por API, DPO, registro de operações) — o sistema manipula dados de pacientes mas é projeto de teste sem dados reais
- Autenticação multifator (MFA / 2FA)
- Auditoria completa de segurança (SAST/DAST automatizado, testes de penetração)
- Rotação automática de chaves JWT e connection strings
- WAF / Azure Front Door
- Audit log detalhado por entidade (quem alterou o quê, quando)

### Ambiguidades resolvidas explicitamente
- **Moeda:** BRL, armazenada como `decimal(10,2)`. Sem arredondamento além do padrão SQL Server.
- **Recuperação de senha:** fora do MVP. Recriar tenant/usuário em dev via script.
- **Confirmação de email:** fora do MVP. Após `POST /api/v1/tenants`, o usuário já pode logar.
- **Notas clínicas pós-cancelamento:** permanecem acessíveis ao médico vinculado à consulta.

---

## 3. Arquitetura e Stack Tecnológico

### 3.1 Padrão Arquitetural
Monolito Modular baseado em Clean Architecture no backend e arquitetura *feature-based* no frontend. Multi-tenant por estratégia de *shared database, shared schema* com discriminador `TenantId` em todas as entidades.

### 3.2 Backend (.NET 10 / C#)
- **Web API:** ASP.NET Core 10 minimal controllers.
- **ORM:** Entity Framework Core 10 com Global Query Filters (`TenantId`, `IsDeleted`) e `EnableRetryOnFailure` para falhas transitórias do Azure SQL.
- **Validação:** FluentValidation (um validator por command/query). Proibido usar DataAnnotations.
- **Segurança:** ASP.NET Core Identity para hash de senha + JWT próprio. `TenantId` extraído das claims do token em toda requisição autenticada.
- **Tratamento de erros:** Middleware global padronizado retornando `{ errorCode, message, details? }` sem expor stack trace.
- **Versionamento de API:** `/api/v1/...` explícito via `Asp.Versioning.Mvc`.
- **OpenAPI:** Swashbuckle habilitado em `Development` e `Staging`. Desabilitado em `Production` (ou atrás de auth admin).
- **Health checks:** `/health` (self) e `/health/ready` (inclui ping no SQL). Usados pelo Azure App Service.
- **Logging:** `ILogger` com `System.Text.Json` formatter + Application Insights. Serilog **não** será usado (mantém stack mínima).
- **CORS:** configurado por ambiente — `localhost:5173` em dev, URL do frontend em Staging/Prod. Origens definidas via variável `CORS__AllowedOrigins` (separadas por `;`).

### 3.3 Frontend (React + TypeScript)
- **Build:** Vite 6+ com React 19.
- **Linguagem:** TypeScript 5.6+ em modo `strict`.
- **Roteamento:** React Router v7 (data router / `createBrowserRouter`).
- **Gerenciamento de dados server-side:** TanStack Query v5 (cache, retry, invalidação por query key).
- **Gerenciamento de estado UI:** Zustand apenas para estado global mínimo (sessão, drawer ativo, tema). Proibido Redux.
- **Forms:** React Hook Form + Zod (schemas compartilhados com o backend quando aplicável via `zod-from-openapi` ou validação manual espelhada).
- **Estilização:** Tailwind CSS v4 + shadcn/ui (Radix primitives).
- **Datas:** `date-fns` com `tz` support. Conversão UTC ↔ `America/Sao_Paulo` centralizada em `lib/datetime.ts`.
- **Ícones:** `lucide-react` (única biblioteca permitida).
- **Notificações:** `sonner` para toasts.
- **HTTP:** `fetch` nativo com wrapper thin em `lib/api.ts`. Proibido `axios` (reduz dependências).
- **Testes:** Vitest + React Testing Library.

### 3.4 Banco de Dados
- **Engine:** Azure SQL Database, camada Serverless Gen5, auto-pause 1h, armazenamento 32GB.
- **Collation:** `SQL_Latin1_General_CP1_CI_AS` (default Azure).
- **Timezone:** todas as colunas `DateTime` armazenadas em **UTC** com tipo `datetime2(0)`. Conversão para `America/Sao_Paulo` ocorre exclusivamente no frontend. O backend nunca formata para exibição.
- **Horários recorrentes (`DoctorSchedule`):** armazenados como `TimeOnly` + `DayOfWeek` em horário local do tenant (`America/Sao_Paulo`). Não há conversão UTC para esses campos.

### 3.5 Testes
- **Backend unitários:** xUnit + NSubstitute (proibido Moq).
- **Backend integração:** xUnit + Testcontainers (SQL Server real em Docker). Cada teste roda em transação revertida.
- **Frontend:** Vitest + React Testing Library. Componentes de UI isolados em `*.test.tsx`; hooks em `*.test.ts`.
- **Cobertura alvo:** ≥ 70% no `Application`, 100% nos use cases críticos (criação de agendamento, transições de status, checkout). Coletor: Coverlet + `reportgenerator`.

### 3.6 Infraestrutura e CI/CD
- **IaC:** Azure Bicep para provisionamento e destruição de ambientes `dev` sob demanda.
- **CI/CD:** GitHub Actions com `azure/login` (OIDC, sem segredos longos) + `azure/webapps-deploy`.
- **Containerização:** backend empacotado como imagem Linux no Azure Container Apps ou App Service Linux B1 (decidir na implementação; preferência por App Service Linux por simplicidade).

---

## 4. Modelagem de Dados e Domínios

Todas as entidades principais herdam de `BaseEntity` com `Id (Guid)`, `TenantId (Guid)`, `CreatedAt (UTC)`, `UpdatedAt (UTC)`, `IsDeleted (bool)`. Soft delete via Global Query Filter.

### 4.1 IAM
- **`Tenant`** — `Id`, `Name`, `CreatedAt`.
- **`User`** — `Id`, `TenantId`, `Name`, `Email (unique por tenant)`, `PasswordHash`, `Role`, `CreatedAt`.
  - **Roles (enum):** `Doctor` (administrador do tenant), `Receptionist` (acesso restrito, sem acesso a prontuário clínico).
- **`RefreshToken`** — `Id`, `UserId`, `Token (hash)`, `ExpiresAt (UTC)`, `RevokedAt?`.

### 4.2 Patient
- **`Patient`** — `Name`, `DateOfBirth (DateOnly)`, `Phone (string, E.164 normalizado)`, `Email?`, `CreatedAt`.
- Busca por `Name` (contains, case-insensitive) ou `Phone` (exact match após normalização).

### 4.3 DoctorSchedule
- **`DoctorSchedule`** — `DoctorId`, `DayOfWeek (enum System.DayOfWeek)`, `StartTime (TimeOnly)`, `EndTime (TimeOnly)`.
- Horários em **hora local do tenant** (`America/Sao_Paulo`), não UTC.
- Validação: `EndTime > StartTime`. Não há suporte a intervalos (lunch break) no MVP — médico define múltiplos registros por dia se necessário.

### 4.4 Appointment
- **`Appointment`** — `PatientId`, `DoctorId`, `StartTime (UTC datetime2)`, `EndTime (UTC datetime2)`, `Status (enum)`, `Price (decimal(10,2))`, `IsPaid (bool)`, `PaidAt? (UTC)`.
- **Status enum:** `Scheduled`, `Confirmed`, `InProgress`, `Completed`, `Canceled`, `NoShow`.
- **Unicidade:** índice filtrado `UNIQUE (DoctorId, StartTime) WHERE IsDeleted = 0` para evitar duplicata e permitir recriação após soft delete.
- Validação de sobreposição: executada em transação no Application layer (`ISession`/`IDbContextTransaction`) antes do insert.

### 4.5 ClinicalNote
- **`ClinicalNote`** — `AppointmentId`, `NoteText (nvarchar(max))`, `DateRecorded (UTC)`.
- Pertence exclusivamente a uma `Appointment`. `PatientId` e `DoctorId` derivam do relacionamento.
- Soft delete **não se aplica** a notas clínicas (imutabilidade para fins de prontuário). Uma vez registrada, não é apagada — apenas o `Appointment` pode ser cancelado.

---

## 5. Regras de Negócio Detalhadas

### 5.1 Provisionamento e Autenticação
- Endpoint público `POST /api/v1/tenants` recebe `{ clinicName, adminName, adminEmail, adminPassword }`.
- Backend valida unicidade de `adminEmail` globalmente (não por tenant) para evitar conflitos de login.
- Cria `Tenant` + primeiro `User` com role `Doctor` e `TenantId` vinculado, em transação única.
- **Sem confirmação de email.** Após `POST /api/tenants`, o usuário já pode autenticar via `POST /api/v1/auth/login`.
- Login retorna `{ accessToken (15 min), refreshToken (7 dias) }`. Refresh token armazenado hash no DB.
- Token JWT contém claims: `sub` (UserId), `email`, `role`, `tenant_id`, `exp`.
- Em toda requisição autenticada, o `TenantId` é extraído da claim e injetado no `ITenantContext` (Scoped) que o EF Core usa no Global Query Filter.
- `POST /api/v1/auth/refresh` recebe `{ refreshToken }`, valida, revoga o anterior e emite novo par.

### 5.2 Papéis e Permissões

| Ação                              | Doctor | Receptionist |
|-----------------------------------|:------:|:------------:|
| Gerenciar pacientes (CRUD)        | ✔️     | ✔️           |
| Visualizar/criar agendamentos     | ✔️     | ✔️           |
| Alterar status da consulta        | ✔️     | ✔️           |
| Visualizar notas clínicas         | ✔️     | ❌           |
| Criar/editar notas clínicas       | ✔️     | ❌           |
| Marcar consulta como paga         | ✔️     | ✔️           |
| Gerenciar agenda do médico        | ✔️     | ❌           |
| Gerenciar usuários do tenant      | ✔️     | ❌           |

Autorização aplicada no backend via policies `[Authorize(Policy = "DoctorOnly")]`. O frontend oculta ações não permitidas por role, mas o backend é a fonte de verdade.

### 5.3 Agendamento e Disponibilidade
- `DoctorSchedule` define dias da semana e horários de atendimento em hora local.
- Ao criar agendamento, o sistema:
  1. Converte `StartTime`/`EndTime` (recebidos em UTC ou com offset) para hora local do tenant.
  2. Verifica se o intervalo está contido em um `DoctorSchedule` daquele `DayOfWeek`.
  3. Verifica sobreposição com `Appointment` existente do mesmo médico dentro de transação serializable.
  4. Insere. O índice filtrado `UNIQUE (DoctorId, StartTime) WHERE IsDeleted = 0` atua como rede de segurança.
- Status inicial: `Scheduled`.
- `Price` é informado no momento do agendamento (default configurável por médico, mas MVP aceita no payload).

### 5.4 Transições de Status

```
Scheduled  → Confirmed | Canceled | InProgress
Confirmed  → InProgress | Canceled | NoShow
InProgress → Completed | Canceled
Completed  → (sem transição; apenas IsPaid pode ser alterado)
NoShow     → (sem transição)
Canceled   → (sem transição)
```

Transições inválidas retornam `409 Conflict` com `{ errorCode: "INVALID_STATUS_TRANSITION", message: "..." }`.

### 5.5 Cancelamento
- `Doctor` e `Receptionist` podem cancelar quando status ∈ `{ Scheduled, Confirmed, InProgress }`.
- Não há restrição de prazo no MVP.
- Cancelamento não remove `ClinicalNote`s já registradas.

### 5.6 Checkout Financeiro
- `PATCH /api/v1/appointments/{id}/payment` com `{ isPaid: true }` marca `IsPaid = true` e `PaidAt = now UTC`.
- Só permitido quando `Status = Completed`.
- Estorno (voltar para `IsPaid = false`) permitido apenas para `Doctor`.

### 5.7 Dashboard
- Atualização sob demanda (refetch manual via botão "Atualizar" ou invalidação de query após mutação).
- Dados exibidos (todos no fuso `America/Sao_Paulo`):
  - Contagem de consultas por status (hoje)
  - Contagem de consultas por status (mês atual)
  - Receita do dia (soma de `Price` onde `IsPaid = true` e `PaidAt` no dia)
  - Receita do mês (soma de `Price` onde `IsPaid = true` e `PaidAt` no mês)
  - Próximos 5 agendamentos do médico logado (se Doctor)

---

## 6. Estrutura do Projeto

### 6.1 Backend (Clean Architecture)

```text
/HealthBr.sln
  /src
    /HealthBr.Domain              # Entidades, Enums, Interfaces de Repositório, ValueObjects
    /HealthBr.Application         # Use Cases (Commands/Queries), DTOs, Validators, Interfaces de Serviço
    /HealthBr.Infrastructure      # EF Core DbContext, Mapeamentos, Auth Services, External Services
    /HealthBr.Api                 # Controllers, Program.cs, Middlewares, ProblemDetails
  /tests
    /HealthBr.UnitTests           # Testes de Application/Domain
    /HealthBr.IntegrationTests    # Testcontainers (SQL Server real)
  /iac
    main.bicep
    modules/app-service.bicep
    modules/sql-server.bicep
    modules/key-vault.bicep
  /docs
    spec.md
    architecture.md
    api-contracts.md
```

**Regras de dependência:** `Api → Application → Domain`. `Infrastructure → Application → Domain`. `Domain` não referencia ninguém. `Application` não referencia `EF Core` nem `ASP.NET Core`.

### 6.2 Frontend (feature-based)

```text
/frontend
  /src
    /app                  # Setup global: router, query client, providers, error boundary
    /routes               # Definição de rotas (React Router v7 data router)
      /(auth)/login.tsx
      /(auth)/signup.tsx
      /(app)/layout.tsx
      /(app)/dashboard.tsx
      /(app)/patients/index.tsx
      /(app)/patients/$id.tsx
      /(app)/appointments/index.tsx
      /(app)/appointments/$id.tsx
      /(app)/settings/users.tsx
      /(app)/settings/schedule.tsx
    /features             # Cada feature agrupa seus components/hooks/queries
      /auth
      /patients
      /appointments
      /clinical-notes
      /dashboard
      /settings
    /shared               # Componentes de UI reutilizáveis (shadcn/ui base)
      /components
      /hooks
      /lib                # api.ts, datetime.ts, query-keys.ts, zod-schemas.ts
    /styles               # globals.css (Tailwind v4), tokens.css
  /public
  /tests
  index.html
  vite.config.ts
  tailwind.config.ts      # Não usado no v4 (config no CSS), mas mantido para tooling
  tsconfig.json
```

**Regras:**
- Componentes de `shared/components` são "burros" — sem lógica de negócio.
- Cada pasta em `features/` exporta um *barrel* `index.ts` com sua API pública.
- Hooks de query/mutation ficam em `features/<x>/api/use-<resource>.ts`. Query keys centralizadas em `shared/lib/query-keys.ts`.
- Proibido importar de uma feature dentro de outra — extraia para `shared` se precisar.

---

## 7. Frontend — Detalhamento Técnico

### 7.1 Autenticação no cliente
- Tokens guardados em **memória** (`access token` em estado React + refresh automático) e **refresh token em cookie httpOnly** setado pelo backend (`Secure`, `SameSite=Strict`, `Path=/api/v1/auth`).
- Proibido `localStorage` para tokens.
- Wrapper `fetch` em `shared/lib/api.ts`:
  - Anexa `Authorization: Bearer <accessToken>` automaticamente.
  - Em `401`, dispara refresh transparente uma vez e retenta.
  - Em `401` após refresh falhar, redireciona para `/login` limpando sessão.
- Rotas protegidas via `loader` do React Router que valida sessão; redireciona para `/login?from=...` se não autenticado.

### 7.2 Estado e cache
- **Server state:** TanStack Query com `staleTime: 30s`, `retry: 1` (não retentar em 4xx). Invalidação por query key após mutações.
- **Client state:** Zustand stores pequenas e focadas. Stores atuais: `useAuthStore` (sessão/user), `useUiStore` (drawer/sidebar aberto). Não criar stores genéricas "global".
- **Form state:** React Hook Form. Não usar `useState` para campos de formulário.

### 7.3 Convenções de componentes
- Páginas em `routes/` são *thin* — apenas orquestram features.
- Lógica de tela fica em `features/<x>/<ScreenName>.tsx`.
- Sub-componentes em `features/<x>/components/`.
- Toda ação de mutação usa `useMutation` com `onSuccess` invalidando queries relevantes e `onError` chamando `toast.error()` com `error.message`.
- Loading states via `Skeleton` (shadcn/ui), nunca via `Loading...` em texto.
- Empty states via componente `<EmptyState title description action? />` padronizado em `shared/components`.

### 7.4 Padrões de erro no cliente
- Erros de rede → `toast.error("Falha de conexão. Tente novamente.")`.
- Erros 4xx com `errorCode` → mapear para mensagens humanas em `shared/lib/error-messages.ts`.
- Erros 5xx → `toast.error("Erro interno. Equipe já foi notificada.")`.
- Erros de validação (400) → exibir inline nos campos via `react-hook-form` `setError`.

### 7.5 Paginação e busca
- Contrato padrão de lista: `{ items: T[], page: number, pageSize: number, total: number, totalPages: number }`.
- Hook padrão `usePaginated<T>(resource, params)` em `shared/hooks`.
- Busca com debounce de 400ms via `useDebouncedValue`.

### 7.6 Datas e timezone
- `shared/lib/datetime.ts` expõe:
  - `toLocal(utcIso: string): string` — converte UTC para `America/Sao_Paulo` formatado.
  - `toLocalDate(utcIso: string): Date` — converte para objeto Date local.
  - `toUtcInput(localIso: string): string` — para enviar ao backend.
  - `formatShort(utcIso: string): string` — `dd/MM/yyyy HH:mm`.
- Inputs `<input type="datetime-local">` produzem string sem timezone; o wrapper converte assumindo `America/Sao_Paulo` antes de enviar.
- Exibição de `DoctorSchedule` não converte (já está em local).

---

## 8. Design System & Diretrizes de UI

> Esta seção é **mandatória**. O frontend deve segui-la literalmente. Desvios devem ser justificados em PR.

### 8.1 Referência estética
Inspirado em **Linear**, **Notion** e **Stripe Dashboard**: denso, tipográfico, baixa saturação, sem ornamentos. Sensação geral: ferramenta de trabalho séria, não produto de marketing.

### 8.2 Anti-padrões proibidos (cheiro de template de IA)
- ❌ Gradientes roxo→rosa ou azul→verde em fundos e botões.
- ❌ Glassmorphism (`backdrop-blur` em cards sobre fundos coloridos).
- ❌ `rounded-2xl` ou `rounded-3xl` em tudo. Raio padrão é `rounded-md` (6px). `rounded-lg` (8px) só em modais grandes.
- ❌ Emojis na UI (🚀 ✨ 🎯). Apenas ícones `lucide-react`.
- ❌ Ilustrações SVG decorativas (Undraw, Storyset) em empty states.
- ❌ Cards grandes com sombra pesada para listas de dados — usar tabelas densas.
- ❌ Headlines com gradient text (`bg-clip-text text-transparent`).
- ❌ Hero sections com CTA gigante — não é landing page.
- ❌ Animações excessivas (framer-motion em tudo). Animações só em interações reais (drawer open, dialog).
- ❌ Uso de `text-blue-500` ou qualquer cor default do Tailwind diretamente. Sempre passar pela paleta de tokens.

### 8.3 Paleta de cores (tokens CSS)

```css
:root {
  /* Base neutra (zinc) */
  --bg-app: #fafafa;            /* fundo da aplicação */
  --bg-surface: #ffffff;        /* cards, tables */
  --bg-subtle: #f4f4f5;         /* hover, background de inputs */
  --border: #e4e4e7;
  --border-strong: #d4d4d8;

  --text-primary: #18181b;      /* zinc-900 */
  --text-secondary: #52525b;    /* zinc-600 */
  --text-muted: #a1a1aa;        /* zinc-400 */

  /* Marca — azul-petróleo institucional (não é o blue-500 do Tailwind) */
  --brand-50: #f0f9f9;
  --brand-100: #ccfbf1;
  --brand-500: #0d9488;         /* teal-600 — primária */
  --brand-600: #0f766e;         /* hover */
  --brand-700: #115e59;

  /* Semânticas */
  --success: #16a34a;
  --success-bg: #f0fdf4;
  --warning: #d97706;
  --warning-bg: #fffbeb;
  --danger: #dc2626;
  --danger-bg: #fef2f2;
  --info: #2563eb;
  --info-bg: #eff6ff;
}
```

A cor primária é **teal-petróleo `#0f766e`** — escolhida por evocar seriedade clínica sem cair no azul genérico. Implementada via tokens CSS, não via classes Tailwind cruas.

### 8.4 Tipografia
- **Fonte:** Inter (variable) via `@fontsource/inter`. Fallback `system-ui, -apple-system, sans-serif`.
- **Mono (código/IDs):** `JetBrains Mono` via `@fontsource/jetbrains-mono`.
- **Tamanho base:** `14px` (não 16). Dashboards admin são densos.
- **Escala:**
  - `text-xs` 12px / 16px line-height — labels, metadados
  - `text-sm` 14px / 20px — corpo padrão
  - `text-base` 16px / 24px — títulos de seção
  - `text-lg` 18px / 28px — títulos de página
  - `text-xl` 20px / 28px — hero mínimo (raramente usado)
- **Pesos:** `font-normal` (400) corpo, `font-medium` (500) ênfase, `font-semibold` (600) títulos. Proibido `font-bold` em corpo de texto.

### 8.5 Espaçamento e layout
- Grid de 4px. Tudo em múltiplos (`gap-2`, `gap-3`, `gap-4`, `gap-6`).
- Densidade: padding de célula de tabela `py-2 px-3`, não `py-4`.
- Largura máxima de conteúdo: `max-w-7xl` em telas de gestão. Formulários em `max-w-xl` centralizado.
- Sidebar fixa `w-60`, colapsável para `w-16` (ícones apenas) em telas < 1280px.

### 8.6 Componentes padrão (shadcn/ui base)
- **Listas de dados:** `<DataTable>` (wrapper sobre `@tanstack/react-table`) — sempre tabelas, nunca grid de cards.
- **Forms:** `<Form>` do shadcn (RHF integrado), inputs `text-sm`, labels `text-xs text-secondary`.
- **Ações primárias:** `<Button variant="default">` com cor brand. Tamanho padrão `default` (h-9).
- **Ações destrutivas:** `<Button variant="destructive">` ou `<AlertDialog>`.
- **Confirmações:** `<AlertDialog>` (não `window.confirm`).
- **Detalhes rápidos:** `<Sheet>` (drawer lateral direito) para visualizar/editar registros sem trocar de rota.
- **Feedback:** `sonner` toasters no canto inferior direito. Máximo 3 visíveis.
- **Badges de status:** `<Badge>` com cor semântica mapeada do enum de status.

### 8.7 Acessibilidade
- Contraste mínimo AA (4.5:1) em todo texto.
- Foco visível: `outline-2 outline-offset-2 outline-brand-500`. Proibido `outline: none` sem fallback.
- Tab order respeita ordem visual.
- Todos os `onClick` em elementos não-button têm `<button>` ou `role="button"` + `onKeyDown`.
- `aria-label` em ícones isolados.

### 8.8 Ícones
- Biblioteca única: `lucide-react`.
- Tamanho padrão: 16px em UI inline, 20px em navegação, 24px em empty states.
- Sempre com `aria-hidden` se decorativos, `aria-label` se funcionais.

---

## 9. Contratos de API Principais

Todas as rotas sob `/api/v1`. Respostas de erro seguem RFC 7807 (`application/problem+json`) com campos `errorCode`, `message`, `details?`.

| Método | Rota                                   | Descrição                                  | Auth       | Role         |
|--------|----------------------------------------|--------------------------------------------|------------|--------------|
| POST   | `/api/v1/tenants`                      | Criação de tenant e admin                  | Público    | —            |
| POST   | `/api/v1/auth/login`                   | Login, retorna access + refresh            | Público    | —            |
| POST   | `/api/v1/auth/refresh`                 | Renova access token                        | Refresh cookie | —        |
| POST   | `/api/v1/auth/logout`                  | Revoga refresh token                       | JWT        | Qualquer     |
| GET    | `/api/v1/me`                           | Dados do usuário logado                    | JWT        | Qualquer     |
| GET    | `/api/v1/patients?page&pageSize&search`| Lista paginada com busca                   | JWT        | Doctor, Rec. |
| POST   | `/api/v1/patients`                     | Cria paciente                              | JWT        | Doctor, Rec. |
| GET    | `/api/v1/patients/{id}`                | Detalhe do paciente                        | JWT        | Doctor, Rec. |
| PUT    | `/api/v1/patients/{id}`                | Atualiza paciente                          | JWT        | Doctor, Rec. |
| DELETE | `/api/v1/patients/{id}`                | Soft delete                                | JWT        | Doctor, Rec. |
| GET    | `/api/v1/doctors`                      | Lista médicos do tenant                    | JWT        | Doctor, Rec. |
| GET    | `/api/v1/doctors/{id}/schedule`        | Agenda semanal do médico                   | JWT        | Doctor, Rec. |
| PUT    | `/api/v1/doctors/{id}/schedule`        | Atualiza agenda (lista de slots)           | JWT        | Doctor       |
| POST   | `/api/v1/appointments`                 | Cria agendamento                           | JWT        | Doctor, Rec. |
| GET    | `/api/v1/appointments?date&doctorId?`  | Lista por dia (e médico)                   | JWT        | Doctor, Rec. |
| GET    | `/api/v1/appointments/{id}`            | Detalhe                                    | JWT        | Doctor, Rec. |
| PATCH  | `/api/v1/appointments/{id}/status`     | Transição de status                        | JWT        | Doctor, Rec. |
| PATCH  | `/api/v1/appointments/{id}/payment`    | Marca paga                                 | JWT        | Doctor, Rec. |
| POST   | `/api/v1/appointments/{id}/notes`      | Adiciona nota clínica                      | JWT        | Doctor       |
| GET    | `/api/v1/appointments/{id}/notes`      | Lista notas                                | JWT        | Doctor       |
| GET    | `/api/v1/dashboard/summary`            | Métricas                                   | JWT        | Doctor, Rec. |
| GET    | `/api/v1/users`                        | Lista usuários do tenant                   | JWT        | Doctor       |
| POST   | `/api/v1/users`                        | Cria usuário (Receptionist)                | JWT        | Doctor       |

**Padrões:**
- Paginação: query params `page` (default 1) e `pageSize` (default 20, max 100).
- Busca: query param `search`. Para pacientes, busca em `Name` (contains) ou `Phone` (exact).
- Filtros de data: query param `date` em formato ISO `YYYY-MM-DD` (interpretado em `America/Sao_Paulo`).
- Sucesso: `200 OK`, `201 Created` (com `Location` header), `204 No Content`.
- Erros: `400` (validação), `401` (não autenticado), `403` (sem permissão), `404` (não encontrado), `409` (conflito de domínio), `500` (erro interno).

---

## 10. Tratamento de Falhas e Resiliência

### 10.1 Backend
- EF Core com `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)`.
- Middleware global captura:
  - `ValidationException` → 400 com lista de erros por campo.
  - `DomainException` → 409 com `errorCode`.
  - `UnauthorizedAccessException` → 401 ou 403 conforme contexto.
  - `EntityNotFoundException` → 404.
  - Demais exceções → 500 com `errorCode: "INTERNAL_ERROR"` e loga stack trace no Application Insights.
- Padrão de resposta de erro (`application/problem+json`):
  ```json
  {
    "type": "https://errors.healthbr.dev/INVALID_STATUS_TRANSITION",
    "title": "Transição de status inválida",
    "status": 409,
    "errorCode": "INVALID_STATUS_TRANSITION",
    "message": "Não é possível mover de Completed para InProgress.",
    "details": { "from": "Completed", "to": "InProgress" },
    "traceId": "00-abc...-01"
  }
  ```

### 10.2 Frontend
- React Error Boundary no nível de rota — captura erros de render e mostra tela de fallback com botão "Recarregar".
- TanStack Query com `retry: 1` (não retentar 4xx). `retryDelay` exponencial.
- `fetch` wrapper trata 401 com refresh transparente único. Falha no refresh → logout + redirect.
- Toda mutação tem `onError` chamando `toast.error()` com mensagem humanizada.

### 10.3 Azure
- App Service com health check configurado (`/health/ready`) — Azure reinicia automaticamente se falhar 5x consecutivas.
- Application Insights com sampling 100% em dev, 10% em prod.
- SQL Server com `EnableRetryOnFailure` no EF Core (já citado) + connection resiliency do ADO.NET.
- Auto-pause do SQL Server pode causar latência no primeiro request após idle — aceitável para MVP.

---

## 11. Azure — Infraestrutura e Deploy

### 11.1 Topologia de recursos

| Recurso                       | Tipo                                    | SKU / Config                                  | Ambiente |
|-------------------------------|-----------------------------------------|-----------------------------------------------|----------|
| Resource Group                | `Microsoft.Resources/resourceGroups`    | `rg-healthbr-dev`                             | Dev      |
| App Service Plan              | `Microsoft.Web/serverfarms`             | Linux, B1 (1 vCPU, 1.75GB)                    | Dev      |
| App Service (Backend)         | `Microsoft.Web/sites`                   | Linux, .NET 10, HTTPS only                    | Dev      |
| Azure SQL Server (logical)    | `Microsoft.Sql/servers`                 | `sql-healthbr-dev`                            | Dev      |
| Azure SQL Database            | `Microsoft.Sql/servers/databases`       | Serverless Gen5, 2 vCores, auto-pause 1h      | Dev      |
| Key Vault                     | `Microsoft.KeyVault/vaults`             | Standard, RBAC                                | Dev      |
| Application Insights          | `Microsoft.Insights/components`         | workspace-based                               | Dev      |
| Log Analytics Workspace       | `Microsoft.OperationalInsights/workspaces` | PerGB2018                                   | Dev      |

> Para o propósito de "testar o agente", apenas o ambiente **Dev** é provisionado. Há um pipeline `destroy.yml` que remove tudo ao final da avaliação.

### 11.2 Secrets
- **Proibido** commitar segredos em `appsettings.json` ou `.env`.
- Local dev: `dotnet user-secrets` (já configurado no template). Chaves: `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey`, `Cors:AllowedOrigins`.
- Azure: Key Vault + Managed Identity (System-assigned no App Service). Acesso via `DefaultAzureCredential` no `Program.cs`.
- Variáveis de ambiente não-secretas (`ASPNETCORE_ENVIRONMENT`, etc.) ficam direto no App Service Configuration.

### 11.3 Deploy (GitHub Actions)
Pipeline `deploy.yml` em `/.github/workflows/`:
1. **Build & Test (matrix):** `dotnet test` + `pnpm test` (frontend). Falha em teste quebra o pipeline.
2. **Publish backend:** `dotnet publish -c Release -o ./publish`. Empacota como ZIP.
3. **Build frontend:** `pnpm build` (gera `dist/`).
4. **Deploy backend:** `azure/webapps-deploy@v3` com o ZIP.
5. **Deploy frontend:** static files servidos pelo backend em `wwwroot/` OU Azure Static Web App separado (decidir: para MVP, **servir pelo backend** em `wwwroot/` simplifica CORS e auth).
6. **Migrations:** step `dotnet ef database update` rodando contra a connection string do Key Vault. Roda **após** deploy, **antes** do warmup.
7. **Smoke test:** HTTP GET em `/health/ready` com retry 5x a cada 10s. Falha marca deploy como falho.

### 11.4 Provisionamento Bicep
- `main.bicep` aceita parâmetros: `environment` (dev/staging/prod), `location` (default `brazilsouth`), `sqlAdminPassword` (via Key Vault reference ou parameter file).
- Outputs: `backendUrl`, `sqlServerFqdn`, `keyVaultUri`.
- Comando: `az deployment group create -g rg-healthbr-dev -f iac/main.bicep -p iac/parameters.dev.json`.
- Destruição: `az group delete -n rg-healthbr-dev --yes` (pipeline manual `destroy.yml`).

### 11.5 Observabilidade
- Application Insights ligado via `AzureMonitor.OpenTelemetry.AspNetCore`.
- Logs estruturados em JSON com campos: `timestamp`, `level`, `message`, `traceId`, `userId?`, `tenantId?`, `exception?`.
- Queries úteis (Kusto) documentadas em `/docs/observability.md` (geradas depois).

---

## 12. Configuração de Dev Local

### 12.1 Pré-requisitos
- .NET 10 SDK
- Node.js 22 LTS + `pnpm` (não usar `npm` diretamente)
- Docker (para Testcontainers e SQL Server local opcional)
- Azure CLI (para deploy/destruir infra)
- `dotnet-ef` tool global instalado

### 12.2 Subir o backend
```bash
cd src/HealthBr.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=HealthBr_Dev;User=sa;Password=Your_password123;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<32+ chars random>"
dotnet user-secrets set "Cors:AllowedOrigins" "http://localhost:5173"
dotnet ef database update --project ../HealthBr.Infrastructure --startup-project .
dotnet run
```
Backend disponível em `https://localhost:5001`. Swagger em `/swagger`.

### 12.3 Subir o frontend
```bash
cd frontend
pnpm install
pnpm dev
```
Frontend disponível em `http://localhost:5173`. Variável `VITE_API_URL=http://localhost:5001/api/v1` em `.env.local`.

### 12.4 SQL Server local via Docker (alternativa ao Azure)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name healthbr-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

### 12.5 Seed de dev
Script `scripts/seed-dev.sql` (ou `dotnet run --project src/HealthBr.Api -- seed`) cria:
- 1 tenant: "Clínica Demo"
- 2 médicos: `doctor@demo.com` / `Doctor@123`
- 1 recepcionista: `recep@demo.com` / `Recep@123`
- 10 pacientes, 50 agendamentos distribuídos nos últimos 30 dias
- Notas clínicas em 20 consultas completed

### 12.6 Comandos úteis (Makefile / package.json scripts)
- `make test-backend` — roda testes do backend
- `make test-frontend` — roda testes do frontend
- `make lint` — roda lint de ambos
- `make infra-up` — `az deployment group create ...`
- `make infra-down` — `az group delete ...`
- `make migrate` — `dotnet ef database update`
- `make migrate-add` — `dotnet ef migrations add <name>`

---

## 13. Convenções de Código e Definition of Done

### 13.1 Convenções de nome
- **C#:**
  - Público: `PascalCase` (classes, métodos, propriedades, enums).
  - Privado: `_camelCase` com underscore para campos.
  - Arquivos: `PascalCase.cs` com nome igual ao da classe principal.
  - Interfaces: prefixo `I`.
- **TypeScript/React:**
  - Variáveis e funções: `camelCase`.
  - Tipos, interfaces, componentes: `PascalCase`.
  - Arquivos de componente: `PascalCase.tsx` (`PatientForm.tsx`).
  - Arquivos não-componente: `camelCase.ts` (`usePatients.ts`, `api.ts`).
  - Constantes: `SCREAMING_SNAKE_CASE` para valores imutáveis globais.

### 13.2 Padrões de commit
- **Conventional Commits** (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`).
- Branches: `feat/<short-desc>`, `fix/<short-desc>`, `chore/<short-desc>`.
- Commits atômicos, em português ou inglês (decidir: **inglês** para padronizar com tooling).

### 13.2.1 Git workflow
- **Branch base:** `main` (protegida, sem push direto). Toda alteração via Pull Request.
- **Branch padrão:** `feat/<task-id>-<short-desc>` (ex.: `feat/3-1-patients-crud`), `fix/<task-id>-<short-desc>` para correções.
- **Commits:** conventional commits em inglês. Body referencia a task: `Refs: 3.1`.
- **PR obrigatório** para todo código que vai para `main`. PR deve referenciar a task do `ROADMAP.md` na descrição.
- **Estratégia de merge:** *squash and merge* como padrão — histórico linear facilita auditoria do que o agente produziu.
- **Tags:** `v0.1.0` ao final do Milestone 3 (primeira feature end-to-end), `v1.0.0` ao final do Milestone 8 (MVP completo).
- **`.gitignore`** cobrindo obrigatoriamente: `bin/`, `obj/`, `node_modules/`, `dist/`, `.env`, `.env.local`, `*.user`, `.vs/`, `.idea/`, `appsettings.Development.json`, `appsettings.*.Local.json`. Manter versionado: `appsettings.json` (template sem segredos), `.env.example`.
- **Proibido commitar:** segredos de qualquer tipo (connection strings com senha, chaves JWT, senhas, tokens), arquivos `.env` (apenas `.env.example`), arquivos de configuração local com segredos (`appsettings.Development.json`).
- **Pre-commit hook (opcional, recomendado):** via Husky + lint-staged no frontend e `dotnet format --verify-no-changes` no backend. Bloqueia commit se lint falhar.
- **Branch protection rules (configurar no GitHub/Azure DevOps):**
  - Exigir PR antes de merge em `main`.
  - Exigir CI verde (build + testes + lint) antes de merge.
  - Exigir no mínimo 1 approval (pode ser o próprio usuário revisando o PR do agente).
  - Proibir force-push em `main`.

### 13.3 Linting e formatação
- **C#:** `.editorconfig` com regras da Microsoft. `dotnet format` no CI.
- **TypeScript:** ESLint + Prettier. Configuração `@typescript-eslint/recommended` + `eslint-plugin-react-hooks`. Prettier com `singleQuote: true`, `semi: true`, `printWidth: 100`.
- Falha de lint quebra o pipeline.

### 13.4 Definition of Done (DoD)
Uma task está "done" quando:
- [ ] Código passa em `dotnet test` e `pnpm test` localmente.
- [ ] Lint limpo (`dotnet format --verify-no-changes` e `pnpm lint`).
- [ ] Build limpo (`dotnet build` e `pnpm build` sem warnings).
- [ ] Endpoints novos cobertos por teste de integração.
- [ ] Componentes de UI novos cobertos por teste de unidade (RTL).
- [ ] Sem `console.log`, `Debugger.Break()`, `debugger`, comentários `TODO` sem issue link.
- [ ] Documentação (OpenAPI / JSDoc) atualizada para a API pública afetada.
- [ ] Acessibilidade: foco visível, contraste OK, navegável por teclado.
- [ ] Sem anti-padrões da Seção 8.2.
- [ ] PR revisado (mesmo que pelo próprio agente em modo auto-review).

### 13.5 Estratégia de trabalho do agente de IA
- O agente deve abrir PRs para cada feature, não commitar direto em `main`.
- Cada PR deve referenciar a seção do spec implementada.
- O agente deve rodar testes e lint localmente antes de abrir PR.
- Em caso de ambiguidade não coberta pelo spec, o agente deve parar e perguntar — não inventar.

---

## 14. Considerações Finais

Este documento reflete todas as decisões acordadas e está pronto para orientar o desenvolvimento do MVP. O propósito é duplo: entregar um produto funcional mínimo **e** servir de benchmark para avaliar a capacidade do agente de IA (zcode + GLM 5.3) em um projeto fullstack realista com .NET, React e Azure.

Princípios orientadores para o agente durante a implementação:
1. **Seguir o spec literalmente.** Desvios devem ser justificados em PR.
2. **Quando em dúvida, perguntar.** Não inventar comportamento.
3. **Preferir convenção sobre configuração** — seguir os padrões aqui definidos em vez de criar novos.
4. **Testar o que escrever.** Toda funcionalidade vem com teste.
5. **Manter o escopo.** Features fora da Seção 2 não devem ser implementadas mesmo que "fácil adicionar".
6. **UI sem anti-padrões.** A Seção 8 é mandatória.

Qualquer ambiguidade remanescente deve ser esclarecida antes do início da codificação, registrando a decisão como emenda a este documento.

---

## 15. Segurança

Esta seção concentra as práticas de segurança obrigatórias do MVP. Tópicos explicitamente fora de escopo estão listados na Seção 2.

### 15.1 Princípios
- **Defesa em profundidade:** validação no cliente (Zod) + validação no backend (FluentValidation) + validação no banco (constraints, índices). Nenhuma camada confia na outra.
- **Menor privilégio:** cada role tem acesso apenas ao mínimo necessário (Seção 5.2). O frontend oculta ações não autorizadas, mas o backend é a fonte de verdade.
- **Falha segura:** em caso de erro, negar acesso / retornar 401-403-500 genérico. Nunca expor stack trace ou detalhe interno.
- **Não confiar no cliente:** todo `TenantId`, `UserId`, `Role` é extraído do token JWT no backend, nunca do payload da requisição.

### 15.2 Validação de input
- Todo input de API é validado por FluentValidation (um validator por command/query). Proibido usar DataAnnotations.
- Inputs não conformes retornam `400 Bad Request` com `application/problem+json` listando erros por campo (`details` contém `{ field: [errors] }`).
- Não há confiança implícita em payloads do cliente — mesmo campos "apenas leitura" são revalidados.
- Strings com tamanho máximo definido no validator para evitar ataques de memória/DoS.
- Validação de formato: `Email` (RFC), `Phone` (E.164 normalizado antes de persistir), datas (ISO 8601).

### 15.3 OWASP Top 10 — mitigação no MVP

| Categoria OWASP | Mitigação adotada |
|-----------------|-------------------|
| **A01 — Broken Access Control** | Policies por role (Seção 5.2) + `ITenantContext` extraído do JWT + Global Query Filter por `TenantId`. |
| **A02 — Cryptographic Failures** | TLS 1.2+ no Azure App Service, senhas com PBKDF2 via ASP.NET Identity, JWT assinado com chave de 256 bits em Key Vault. |
| **A03 — Injection** | EF Core parametriza queries automaticamente. Proibido `FromSqlRaw` sem parâmetros. Sem concatenação de SQL. |
| **A04 — Insecure Design** | Spec técnico revisado, ameaças modeladas (multi-tenant isolation). |
| **A05 — Security Misconfiguration** | `appsettings.Production.json` sem segredos, variáveis via Key Vault, HTTPS obrigatório, HSTS habilitado, erros sem stack trace. |
| **A06 — Vulnerable Components** | `dotnet list package --vulnerable` no CI. `pnpm audit` no CI. Dependabot habilitado. |
| **A07 — Auth Failures** | JWT curto (15 min) + refresh token hash no DB com revogação. Log de tentativas falhadas (Seção 15.6). |
| **A08 — Data Integrity Failures** | Tokens assinados com chave simétrica de 256 bits, rotação manual documentada. |
| **A09 — Logging Failures** | Logs estruturados em Application Insights com `traceId`, `userId`, `tenantId`. Eventos de auth logados (Seção 15.6). |
| **A10 — SSRF** | Não há fetch de URL arbitrária no backend. |

### 15.4 Headers de segurança HTTP

Configurados no `Program.cs` do backend via middleware:

```csharp
app.UseHsts(); // Apenas em Production
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none';";
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});
```

No frontend (Vite), CSP refinada via `vite-plugin-csp-guard` ou meta tag no `index.html`. Em dev, CSP mais permissiva para permitir HMR.

### 15.5 Cookies
- **Refresh token cookie:** `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`, expira em 7 dias.
- **Sem outros cookies de sessão.** Aplicação não usa cookies para nada além do refresh.
- Proibido cookies `SameSite=None` no MVP.

### 15.6 Logging de eventos de segurança
Eventos que devem ser logados via `ILogger` com nível `Warning` (ou `Information` para sucessos), incluindo `userId`, `tenantId`, `traceId`, IP de origem:

- ✅ Login bem-sucedido
- ⚠️ Login falhado (email informado, motivo)
- ⚠️ Tentativa de acesso negado (403) — endpoint, role do usuário
- ⚠️ Token JWT inválido ou expirado
- ⚠️ Refresh token revogado ou inválido
- ✅ Criação de tenant
- ✅ Criação/alteração de usuário
- ⚠️ Transição de status inválida (409) — pode indicar abuso

Não logar: senhas (mesmo hash), tokens completos (apenas primeiros 8 chars + `...`), dados clínicos do paciente, dados de pagamento.

### 15.7 Multi-tenant isolation
- Toda query EF Core passa pelo Global Query Filter de `TenantId` automaticamente.
- Toda entidade nova herdada de `BaseEntity` DEVE registrar o filtro no `OnModelCreating`.
- Teste de integração obrigatório: tentar acessar recurso de tenant A logado como tenant B deve retornar 404 (não 403, para não vazar existência).
- `ITenantContext` é populado uma vez por request a partir da claim `tenant_id` do JWT. Proibido setter público além do middleware de autenticação.

### 15.8 Segredos e chaves
- **JWT signing key:** 256 bits (32+ caracteres) gerada via `openssl rand -base64 32`, armazenada em Key Vault em produção e `user-secrets` em dev.
- **Connection string:** sem senha hardcoded. Em dev, SQL local via Docker com senha forte. Em prod, Managed Identity do App Service autenticando no SQL via Microsoft Entra ID (antigo AAD), ou SQL user com senha em Key Vault.
- **Rotação:** manual, documentada em `docs/operations.md` (gerado depois). Frequência: a cada 90 dias para JWT, a cada 180 dias para senhas SQL.
- **Proibido:** segredos em `appsettings.json` commitado, segredos em variáveis de ambiente do GitHub Actions em texto plano (usar GitHub Secrets).

### 15.9 CORS
- Origens permitidas configuradas via variável `CORS__AllowedOrigins` (separadas por `;`).
- Dev: `http://localhost:5173`.
- Prod: URL do frontend (ex.: `https://app.clinica.com`).
- `AllowAnyOrigin: false`, `AllowCredentials: true` (necessário para refresh cookie), métodos limitados a `GET, POST, PUT, PATCH, DELETE, OPTIONS`, headers limitados a `Authorization, Content-Type`.

### 15.10 Checklist de segurança para PRs
Antes de aprovar qualquer PR, verificar:
- [ ] Nenhum segredo commitado (rodar `git-secrets` ou revisão manual de diff)
- [ ] Novo endpoint tem `[Authorize]` e policy correta
- [ ] Novo input tem FluentValidation
- [ ] Nova entidade herda de `BaseEntity` e tem Global Query Filter
- [ ] Teste de integração cobre caso "acesso a recurso de outro tenant"
- [ ] Logs de evento de segurança adicionados quando aplicável
- [ ] Sem `console.log` de dados sensíveis no frontend
- [ ] Headers de segurança ainda presentes (não removidos por engano)

---

## 16. Glossário rápido (referência)

- **Tenant:** clínica/consultório cliente do SaaS. Identificado por `TenantId` (Guid).
- **Doctor:** médico com role de administrador do tenant. Acesso total.
- **Receptionist:** recepcionista com role restrita. Sem acesso a prontuário clínico.
- **Appointment:** consulta/agendamento. Tem status, preço e flag de pago.
- **ClinicalNote:** evolução clínica em texto livre, vinculada a um `Appointment`. Imutável.
- **DoctorSchedule:** agenda semanal recorrente do médico (em hora local do tenant).
- **DoD:** Definition of Done (Seção 13.4).
- **ProblemDetails:** formato de erro padronizado RFC 7807.
- **Global Query Filter:** filtro automático do EF Core que isola dados por `TenantId` e `IsDeleted`.
