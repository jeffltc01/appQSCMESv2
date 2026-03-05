# Master Prompt: Execute Full Scheduling Build

Use this as the single prompt to Cursor AI.

---

Read and execute the full implementation contract in:

`designInput/PROMPTPACK_HEIJUNKA_SCHEDULING_IMPLEMENTATION.md`

Execution requirements:

1. Follow phases strictly in order.
2. Do not skip tests or verification gates.
3. Keep controllers thin and place business logic in services.
4. Respect existing role hierarchy and site-scoped permissions.
5. Use the Phase 1 KPI profile only (as defined in the scheduling spec and prompt pack).
6. Treat unmapped ERP demand as explicit exceptions; never ignore silently.
7. Account for final paint-line scan constraint (no forced weld-line attribution unless traceable).

Before coding:

- restate the plan and affected layers,
- call out assumptions and risks,
- then begin implementation.

Completion criteria:

- all phases in the prompt pack completed,
- migrations and tests executed,
- smoke test evidence captured,
- final report includes changed files, commands, pass/fail, and remaining risks.

---

If blocked by ambiguity, stop and ask only the minimum clarifying question needed to continue.

