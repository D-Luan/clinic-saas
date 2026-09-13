---
name: add-feature
description: Use when creating a new feature end-to-end (entity + use case + endpoint + frontend page). Triggers on phrases like "criar feature", "nova feature", "add feature", "implementar [X]", "create endpoint", "nova tela", "criar tela".
---

# Como criar uma feature neste projeto

## Pré-requisitos

- Ter lido `docs/spec.md` completamente.
- Confirmar com o usuário qual feature está sendo criada e qual task do `ROADMAP.md` ela atende.
- Estar em uma feature branch (`feat/<task-id>-<short-desc>`), nunca em `main`.

## Passos obrigatórios (em ordem)

### Backend

1. **Entidade** em `src/HealthBr.Domain/Entities/` herdando de `BaseEntity` (Id, TenantId, CreatedAt, UpdatedAt, IsDeleted). Adicionar enum se necessário em `src/HealthBr.Domain/Enums/`. Exceção: `ClinicalNote` não usa soft delete (spec 4.5).
2. **DTOs** em `src/HealthBr.Application/Features/<Feature>/Dto/` (Request, Response, ListResponse com paginação `{ items, page, pageSize, total, totalPages }`).
3. **Validator** em `src/HealthBr.Application/Features/<Feature>/Validators/` usando FluentValidation. Proibido DataAnnotations. Definir tamanho máximo em strings (spec 15.2).
4. **Use case** em `src/HealthBr.Application/Features/<Feature>/Commands|Queries/` (Command/Query + Handler separados). Handlers injetam `ITenantContext` e `IRepository<T>`. `Application` não referencia EF Core nem ASP.NET Core (spec 6.1).
5. **Mapping** em `src/HealthBr.Infrastructure/Persistence/Mappings/` (Fluent API). Toda entidade nova herda de `BaseEntity` e DEVE registrar o Global Query Filter no `OnModelCreating` (spec 15.7).
6. **Migration:** `dotnet ef migrations add Add<Feature> --project src/HealthBr.Infrastructure --startup-project src/HealthBr.Api`.
7. **Controller** em `src/HealthBr.Api/Controllers/` com rota `/api/v1/<feature>`. Aplicar `[Authorize(Policy = "...")]` conforme seção 5.2. `TenantId` sempre vem do JWT, nunca do payload (spec 15.1).
8. **Teste de integração** em `tests/HealthBr.IntegrationTests/Features/<Feature>/` usando Testcontainers (SQL Server real, transação revertida). Cobrir: happy path, auth falha (401), role errada (403), validação (400), conflito de domínio (409), não encontrado (404) e **acesso a recurso de outro tenant → 404** (spec 15.7).

### Frontend

9. **Pasta** em `frontend/src/features/<feature>/` com subpastas `components/`, `api/`, `hooks/` e `index.ts` (barrel com a API pública da feature).
10. **Schemas Zod** em `frontend/src/features/<feature>/api/schemas.ts` espelhando os validators do backend.
11. **Query hooks** em `frontend/src/features/<feature>/api/use-<feature>.ts` usando TanStack Query. Query keys via `shared/lib/query-keys.ts`. Mutations com `onSuccess` invalidando queries e `onError` chamando `toast.error()`.
12. **Componentes de UI** em `frontend/src/features/<feature>/components/` usando shadcn/ui de `shared/components`. Proibido importar de outra feature — extraia para `shared`.
13. **Página** em `frontend/src/routes/(app)/<feature>/index.tsx` (thin — apenas orquestra a feature).
14. **Teste de unidade** com React Testing Library cobrindo render, interação e estados (loading com Skeleton, error, empty com `<EmptyState>`).

## Padrões obrigatórios

- **API versioning:** `/api/v1/...` sempre.
- **Erros:** ProblemDetails (RFC 7807) com `errorCode`, `message`, `details?`. Sem stack trace.
- **Paginação:** contrato `{ items, page, pageSize, total, totalPages }`. `page` default 1, `pageSize` default 20 max 100.
- **Datas:** UTC no backend (`datetime2(0)`), conversão para `America/Sao_Paulo` no frontend via `shared/lib/datetime.ts`. `DoctorSchedule` não converte (já está em local).
- **Forms:** React Hook Form + Zod. Nunca `useState` para campos.
- **Loading states:** `<Skeleton>` do shadcn, nunca texto "Loading...".
- **Empty states:** componente `<EmptyState>` padronizado de `shared/components`.

## Proibições (não faça)

- ❌ DataAnnotations (use FluentValidation)
- ❌ axios (use `fetch` via `shared/lib/api.ts`)
- ❌ Redux (use Zustand apenas para estado UI mínimo)
- ❌ localStorage para tokens (memória + httpOnly cookie para refresh)
- ❌ `rounded-2xl` ou `rounded-3xl` em componentes (use `rounded-md`)
- ❌ Emojis na UI (use `lucide-react`)
- ❌ Ilustrações SVG decorativas em empty states
- ❌ Gradientes roxo→rosa ou azul→verde
- ❌ Glassmorphism (`backdrop-blur` sobre fundos coloridos)
- ❌ `text-blue-500` direto (use tokens CSS da seção 8.3)
- ❌ `console.log` em código de produção
- ❌ `TODO` sem link de issue

## Checklist final (Definition of Done — spec seção 13.4)

Antes de marcar a feature como pronta, verificar:

- [ ] `dotnet test` passa
- [ ] `pnpm test` passa
- [ ] `dotnet format --verify-no-changes` limpo
- [ ] `pnpm lint` limpo
- [ ] Build limpo sem warnings
- [ ] Endpoint coberto por teste de integração
- [ ] Componentes de UI cobertos por teste de unidade
- [ ] Sem `console.log` / `Debugger.Break()` / `debugger`
- [ ] Foco visível, contraste AA, navegável por teclado
- [ ] Nenhum anti-padrão da seção 8.2 do spec
- [ ] OpenAPI atualizado para endpoints novos

## Referências

- `docs/spec.md` seções 4 (dados), 5 (regras), 6 (estrutura), 7 (frontend), 8 (design), 9 (API), 13 (DoD), 15 (segurança)
