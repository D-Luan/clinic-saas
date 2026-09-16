# TODO — pendências fora do escopo das tasks

Itens identificados durante a execução que não couberam no escopo da task vigente, conforme a política de escopo do spec (seção 13.6). Cada item deve virar task, ser absorvido por uma task existente ou ser descartado pelo usuário.

- [ ] `docs/architecture.md` e `docs/api-contracts.md` aparecem na estrutura do spec (seção 6.1), mas nenhuma task do ROADMAP os cobre. Sugestão: criar junto do Milestone 8 (polish e validação final), quando arquitetura e contratos estiverem estabilizados.
- [x] Os testes de smoke em `tests/HealthBr.UnitTests/SmokeTests.cs` eram placeholders triviais do scaffold; substituídos na task 1.4 pelos testes unitários de mapeamento exceção → ProblemDetails do middleware de erro global.
- [x] O endpoint placeholder `GET /` em `src/HealthBr.Api/Program.cs` foi removido na task 1.4 junto com a montagem da pipeline real (health checks em `/health` e `/health/ready`).
- [ ] `IRepository<T>` (task 1.1) é um contrato genérico minimalista de propósito: sem paginação, sem finders específicos e sem `SaveChanges`/transação. Estender conforme as tasks exigirem — paginação/busca no repositório (tasks 3.1+), interfaces especializadas por feature (ex.: `IPatientRepository`) e `IUnitOfWork`/`ISession` (task 4.2, spec 5.3/4.4).
- [ ] Detecção de reuso de refresh token revogado (revogar toda a família de tokens do usuário, hardening comum de rotação): a spec 5.1 exige apenas rotação + 401 no reuso; o handler da task 1.3 loga Warning mas não revoga em cascata. Revisitar se vazar para o escopo de segurança.
- [ ] Task 2.1 (provisionamento): persistir `adminEmail` normalizado (trim + lowercase), na mesma forma canônica do `LoginCommand` — a collation CI do SQL já tolera diferença de caixa, mas a forma canônica evita divergência entre lookup e armazenamento.
- [ ] `CreatedAt`/`UpdatedAt` são preenchidos por quem cria a entidade (hoje, testes; depois, use cases). Ao implementar os repositórios (tasks 3.x), considerar um `SaveChangesInterceptor` no `HealthBrDbContext` para padronizar os timestamps.
