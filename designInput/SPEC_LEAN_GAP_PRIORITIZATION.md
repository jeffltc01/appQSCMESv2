# Lean Maturity Gap Prioritization

## Purpose

Define which Lean capability gaps should be implemented first in MES v2, based on expected business impact and delivery effort.

## Scoring Model

- Impact score: 1 (low) to 5 (high), weighted by safety, quality, throughput, and labor productivity.
- Effort score: 1 (low) to 5 (high), weighted by cross-layer complexity (DB, API, UI, tests, rollout risk).
- Priority index: `Impact / Effort`.

## Prioritized Gap Backlog

| Rank | Gap | Impact | Effort | Priority Index | Why it matters now |
|---|---|---:|---:|---:|---|
| 1 | Takt execution loop | 5 | 2 | 2.50 | Directly improves pace discipline and short-interval recovery behavior at work centers. |
| 2 | Andon event lifecycle and escalation | 5 | 3 | 1.67 | Reduces response delay for line stops and recurring quality disruptions. |
| 3 | Value-stream lead-time decomposition | 4 | 3 | 1.33 | Surfaces queue/wait losses hidden behind daily totals and OEE-only views. |
| 4 | Structured RCA/CAPA workflow | 4 | 4 | 1.00 | Prevents repeated defects/downtime through accountability and verification loops. |
| 5 | Replenishment kanban loop controls | 4 | 4 | 1.00 | Upgrades local FIFO queueing into system-level pull replenishment discipline. |
| 6 | Heijunka level-loading | 3 | 5 | 0.60 | High strategic value, but requires stronger upstream planning/process maturity first. |

## Delivery Sequencing

1. **Phase 1 (quick-win controls):** takt + lead-time visibility in existing supervisor/dashboard surfaces.
2. **Phase 2 (response):** andon event model + escalation timers integrated with downtime/annotations.
3. **Phase 3 (learning):** RCA/CAPA workflow linked to andon/defect/downtime events.
4. **Phase 4 (flow control):** replenishment kanban policies and release governance.
5. **Phase 5 (advanced planning):** heijunka pilot by plant/line.

## Stakeholder RACI

| Workstream | Responsible | Accountable | Consulted | Informed |
|---|---|---|---|---|
| Takt + dashboard metrics | Supervisor + Ops analyst | Plant Manager | Team Leads, Quality Manager | Operators |
| Andon lifecycle | Team Leads + Supervisors | Plant Manager | Quality Manager, Maintenance Lead | Directors |
| RCA/CAPA workflow | Quality Tech + Quality Manager | Quality Director | Supervisors, Operations Director | Plant teams |
| Replenishment kanban | Material Handler Leads | Operations Director | Supervisors, Quality Manager | Operators |
| Heijunka pilot | Operations Director | Operations Director | Plant Manager, Scheduler, Quality Director | All plants |

## Acceptance Criteria

- A signed-off gap ranking exists and is traceable to this file.
- Phase order is agreed by Operations + Quality leadership.
- Work items for the next two phases are ready for implementation specs and estimation.
