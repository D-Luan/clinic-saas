---
name: ui-component
description: Use when creating or modifying any React UI component, page, or screen. Triggers on phrases like "criar componente", "novo componente", "criar tela", "fazer UI", "novo card", "criar form", "criar tabela", "design da tela".
---

# Como criar componentes de UI neste projeto

## Antes de começar

- Leia `docs/spec.md` seção 8 (Design System) completamente. Ela é **mandatória**.
- Confirme qual componente/tela está criando e em qual feature.
- Se for tela nova, verifique se a rota já existe em `frontend/src/routes/`.

## Paleta de cores (use sempre estes tokens CSS, nunca cores Tailwind default)

```css
:root {
  --bg-app: #fafafa;
  --bg-surface: #ffffff;
  --bg-subtle: #f4f4f5;
  --border: #e4e4e7;
  --border-strong: #d4d4d8;

  --text-primary: #18181b;
  --text-secondary: #52525b;
  --text-muted: #a1a1aa;

  --brand-50: #f0f9f9;
  --brand-100: #ccfbf1;
  --brand-500: #0d9488;
  --brand-600: #0f766e;
  --brand-700: #115e59;

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

No Tailwind v4, usar via `@theme` ou classes utilitárias mapeadas. **Proibido** `text-blue-500`, `bg-purple-600` etc. diretamente. A cor primária é o teal-petróleo `#0f766e` (`--brand-600`).

## Anti-padrões PROIBIDOS (seção 8.2 do spec)

- ❌ Gradientes roxo→rosa ou azul→verde em fundos e botões
- ❌ Glassmorphism (`backdrop-blur` em cards sobre fundos coloridos)
- ❌ `rounded-2xl` ou `rounded-3xl` em qualquer lugar. Raio padrão: `rounded-md` (6px). `rounded-lg` (8px) só em modais grandes.
- ❌ Emojis na UI (🚀 ✨ 🎯). Use `lucide-react`.
- ❌ Ilustrações SVG decorativas (Undraw, Storyset) em empty states
- ❌ Cards grandes com sombra pesada para listas de dados — use tabelas densas
- ❌ Headlines com gradient text (`bg-clip-text text-transparent`)
- ❌ Hero sections com CTA gigante — não é landing page
- ❌ Animações excessivas (framer-motion em tudo). Animações só em interações reais (drawer, dialog).
- ❌ Uso de cores default do Tailwind diretamente — sempre via tokens

## Tipografia

- Fonte: Inter (variable) via `@fontsource/inter`. Fallback `system-ui`.
- Mono: `JetBrains Mono` via `@fontsource/jetbrains-mono`.
- Tamanho base: **14px** (não 16). Dashboards admin são densos.
- Escala:
  - `text-xs` 12px — labels, metadados
  - `text-sm` 14px — corpo padrão
  - `text-base` 16px — títulos de seção
  - `text-lg` 18px — títulos de página
- Pesos: `font-normal` (400) corpo, `font-medium` (500) ênfase, `font-semibold` (600) títulos. Proibido `font-bold` em corpo.

## Espaçamento

- Grid de 4px. Sempre múltiplos (`gap-2`, `gap-3`, `gap-4`, `gap-6`).
- Densidade: célula de tabela `py-2 px-3`, não `py-4`.
- Largura máxima: `max-w-7xl` em telas de gestão, `max-w-xl` em formulários centralizados.
- Sidebar fixa `w-60`, colapsável para `w-16` (ícones apenas) em telas < 1280px.

## Componentes base (sempre usar shadcn/ui)

- **Listas de dados:** `<DataTable>` wrapper sobre `@tanstack/react-table`. Sempre tabelas, nunca grid de cards.
- **Forms:** `<Form>` do shadcn (RHF integrado). Inputs `text-sm`, labels `text-xs text-secondary`.
- **Ações primárias:** `<Button variant="default">` (cor brand). Tamanho `default` (h-9).
- **Ações destrutivas:** `<Button variant="destructive">` ou `<AlertDialog>`.
- **Confirmações:** `<AlertDialog>` (nunca `window.confirm`).
- **Detalhes rápidos:** `<Sheet>` (drawer lateral direito) — não trocar de rota para edição simples.
- **Feedback:** `sonner` toasters no canto inferior direito. Máximo 3 visíveis.
- **Badges de status:** `<Badge>` com cor semântica mapeada do enum de status.

## Acessibilidade (mínimo)

- Contraste AA (4.5:1) em todo texto.
- Foco visível: `outline-2 outline-offset-2 outline-brand-500`. Proibido `outline: none` sem fallback.
- Tab order respeita ordem visual.
- `onClick` em não-button → usar `<button>` ou `role="button"` + `onKeyDown`.
- `aria-label` em ícones isolados.

## Ícones

- Biblioteca única: `lucide-react`.
- Tamanho: 16px em UI inline, 20px em navegação, 24px em empty states.
- `aria-hidden` se decorativo, `aria-label` se funcional.

## Estrutura de arquivo de componente

```tsx
// frontend/src/features/<feature>/components/PatientForm.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/shared/components/ui/button'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { patientSchema, type PatientInput } from '../api/schemas'

interface PatientFormProps {
  defaultValues?: Partial<PatientInput>
  onSubmit: (data: PatientInput) => void
  onCancel: () => void
}

export function PatientForm({ defaultValues, onSubmit, onCancel }: PatientFormProps) {
  const form = useForm<PatientInput>({
    resolver: zodResolver(patientSchema),
    defaultValues,
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Nome</FormLabel>
              <FormControl>
                <Input placeholder="João da Silva" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {/* demais campos */}
        <div className="flex justify-end gap-2 pt-4">
          <Button type="button" variant="outline" onClick={onCancel}>Cancelar</Button>
          <Button type="submit">Salvar</Button>
        </div>
      </form>
    </Form>
  )
}
```

Notas: `rounded-md` implícito nos componentes shadcn, sem `rounded-2xl`. Sem emojis. Sem gradientes. Labels e textos em PT-BR. Todo componente novo vem com teste RTL cobrindo render, interação e estados (loading/error/empty).

## Referências

- `docs/spec.md` seção 8 (Design System completo)
- `docs/spec.md` seção 7 (frontend detalhado)
