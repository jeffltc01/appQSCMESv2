# Frontend Testing Guidelines

Use these patterns to reduce flaky test failures between local and CI runs.

## Async UI Interactions

- Prefer `findBy...` for elements that render after user interactions.
- Avoid immediate `getBy...` calls right after opening dialogs, menus, or toasts.

## Scoped Queries

- Scope queries to the container under test when names repeat.
- For dialogs, query actions with `within(dialog)` instead of global `screen`.

## Shared Helpers

- Reuse `src/test/dialogTestUtils.ts` helpers for dialog interactions:
  - `openDialogByTrigger(...)`
  - `getDialogActionButton(...)`
- For Fluent UI dialogs rendered via portals, use hidden-aware queries where needed:
  - `screen.findByRole('dialog', { hidden: true })`
  - `within(dialog).getByRole('button', { name: /save|add|deactivate/i, hidden: true })`

## CI-Parity Checks

- Keep fast local loops with targeted tests.
- Use local tiers:
  - Tier 1 (fast): `npm run verify:tier1`
  - Tier 2 (full frontend parity): `npm run verify:tier2`
- Before deploy-related pushes, run full parity from repo root:
  - Windows + WSL Ubuntu: `powershell -ExecutionPolicy Bypass -File .\\scripts\\verify-parity-wsl.ps1`
  - Native bash/WSL: `bash ./scripts/verify-parity.sh`
