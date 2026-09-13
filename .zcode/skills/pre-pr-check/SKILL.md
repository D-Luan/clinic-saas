---
name: pre-pr-check
description: Use before committing or opening a PR to validate the Definition of Done. Triggers on phrases like "commit", "vou commitar", "abrir PR", "pre-pr check", "revisar antes de commitar", "validar PR".
---

# Checklist pré-commit / pré-PR

## Antes de commitar, rodar obrigatoriamente:

1. `dotnet build` — sem warnings
2. `dotnet test` — todos passam
3. `dotnet format --verify-no-changes` — sem mudanças pendentes
4. `cd frontend && pnpm lint` — limpo
5. `cd frontend && pnpm test` — todos passam
6. `cd frontend && pnpm build` — sem warnings

## Bloqueadores de commit (NÃO commitar se encontrar):

- ❌ `console.log` em código frontend
- ❌ `Debugger.Break()` ou `debugger` em qualquer lugar
- ❌ Comentários `TODO` ou `FIXME` sem link de issue
- ❌ Segredos hard-coded (connection strings, chaves JWT, senhas)
- ❌ `appsettings.json` com segredos (deve ir em user-secrets ou Key Vault)
- ❌ `.env` commitado (apenas `.env.example`)
- ❌ Qualquer anti-padrão da seção 8.2 do spec.md
- ❌ Componente sem `aria-label` em ícone funcional
- ❌ Endpoint sem `[Authorize]` quando deveria ter
- ❌ Endpoint sem teste de integração
- ❌ Componente de UI sem teste de unidade
- ❌ `rounded-2xl` em componentes
- ❌ Emojis na UI
- ❌ Gradientes proibidos (roxo-rosa, azul-verde)
- ❌ Uso direto de cores Tailwind default (`text-blue-500` etc.) em vez de tokens
- ❌ Commit direto em `main` (sempre feature branch + PR)

## Checklist de segurança (spec 15.10)

- [ ] Nenhum segredo no diff
- [ ] Novo endpoint tem `[Authorize]` e policy correta
- [ ] Novo input tem FluentValidation
- [ ] Nova entidade herda de `BaseEntity` e tem Global Query Filter
- [ ] Teste de integração cobre caso "acesso a recurso de outro tenant"
- [ ] Logs de evento de segurança adicionados quando aplicável
- [ ] Sem `console.log` de dados sensíveis no frontend
- [ ] Headers de segurança ainda presentes

## Formato do commit

- Conventional commits em inglês: `feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`.
- Corpo do commit referenciando a task do ROADMAP.md: `Refs: task 3.1`.
- PR obrigatório para `main`, com squash and merge, referenciando a task na descrição.

## Em caso de falha

- Se um teste falha: corrija antes de commitar. Não use `--no-verify`.
- Se o lint falha: rode `dotnet format` ou `pnpm lint --fix` conforme aplicável.
- Se encontrar um anti-padrão: refatore antes de commitar.

## Referências

- `docs/spec.md` seções 8.2, 13.2 (git workflow), 13.3, 13.4, 15.10
