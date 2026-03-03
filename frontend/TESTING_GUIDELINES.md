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

## CI-Parity Checks

- Keep fast local loops with targeted tests.
- Before deploy-related pushes, run a broader check:
  - `npm run test:coverage`
  - `npm run typecheck`
