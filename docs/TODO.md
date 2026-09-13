# TODO — pendências fora do escopo das tasks

Itens identificados durante a execução que não couberam no escopo da task vigente, conforme a política de escopo do spec (seção 13.6). Cada item deve virar task, ser absorvido por uma task existente ou ser descartado pelo usuário.

- [ ] `docs/architecture.md` e `docs/api-contracts.md` aparecem na estrutura do spec (seção 6.1), mas nenhuma task do ROADMAP os cobre. Sugestão: criar junto do Milestone 8 (polish e validação final), quando arquitetura e contratos estiverem estabilizados.
- [ ] Os testes de smoke em `tests/HealthBr.UnitTests/SmokeTests.cs` e `tests/HealthBr.IntegrationTests/SmokeTests.cs` são placeholders triviais do scaffold; substituir por testes reais conforme as tasks 1.x+ introduzem domínio e integração.
- [ ] O endpoint placeholder `GET /` em `src/HealthBr.Api/Program.cs` não faz parte do contrato de API do spec (seção 9); remover quando a pipeline real da API for montada (task 1.4 introduz health checks em `/health` e `/health/ready`).
