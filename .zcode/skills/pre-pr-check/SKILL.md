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

## Fluxo autônomo de Git (executar após passar pelo checklist acima)

Após validar que o código está pronto para commit, executar:

1. Verificar branch atual: `git branch --show-current`
   - Deve estar em `feat/<task-id>-<short-desc>`. Se estiver em `main`, **PARAR** e perguntar ao usuário.
2. Stage das mudanças: `git add .`
3. Verificar diff staged: `git diff --cached --stat` — confirmar que não há arquivos inesperados (`.env`, segredos, `bin/`, `obj/`, `node_modules/`).
4. Commit: `git commit -m "feat: <description>" -m "Refs: <task-id>"`
5. Push: `git push -u origin <branch>`
6. Abrir PR:

   ```bash
   gh pr create \
     --title "feat: <description>" \
     --body "Closes task <task-id>.

   ## O que foi feito
   - <bullet 1>
   - <bullet 2>

   ## Decisões tomadas
   - <decisão 1>

   ## Dívidas/Dúvidas
   - <item se houver>" \
     --base main
   ```

7. Marcar `[x]` no ROADMAP.md na task correspondente
8. Commit do ROADMAP: `git commit -m "docs: mark task <task-id> as done"`
9. Push: `git push`
10. Reportar ao usuário: URL do PR, resumo das decisões, dúvidas e próxima task sugerida.

### Quando NÃO executar o fluxo autônomo

PARE antes de commitar e pergunte ao usuário se:

- Testes falharam 3+ vezes consecutivas
- Há ambiguidade no spec sobre o que implementar
- A task envolve segurança (auth, JWT, cookies, headers HTTP, multi-tenant isolation)
- A task envolve timezone ou conversão de datas
- Há configuração de ambiente faltando (git auth, gh CLI, secrets, user-secrets)
- O spec parece contraditório em alguma parte relevante à task

### Proibições absolutas

- ❌ `git merge` (apenas o usuário faz merge via PR)
- ❌ `git push --force` em `main`
- ❌ `git commit` em `main` direto
- ❌ `git commit --no-verify`
- ❌ Commitar arquivos `.env`, `appsettings.Development.json`, ou qualquer segredo
- ❌ Apagar branches remotas sem permissão explícita
- ❌ Rebase interativo sem pedir
