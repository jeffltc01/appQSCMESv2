# Checklist Templates

Checklist Templates is the admin screen for creating and maintaining reusable checklist definitions used by safety and operations workflows.

## How It Works

1. **Filter templates.** Use Site and Type filters to narrow the list.
2. **Create or edit.** Click **Add Template** to create a new one, or use the edit icon on an existing card.
3. **Define template scope.** Choose `PlantWorkCenter`, `SiteDefault`, or `GlobalDefault`, then set Site/Work Center/Line as needed.
4. **Set behavior.** Configure response mode (`PFNA` or `PF`), active status, fail-note requirement, and safety profile.
5. **Build questions.** Add questions one at a time, reorder them, or bulk import pass/fail prompts.

## Fields & Controls

| Element | Description |
|---|---|
| **Title / Template Code** | Required identity fields for each template version. |
| **Checklist Type** | Template category (`SafetyPreShift`, `SafetyPeriodic`, `OpsPreShift`, `OpsChangeover`). |
| **Scope Level** | Where template applies: line-specific, site default, or global default. |
| **Version / Effective Dates** | Controls rollout/version timing for template variants. |
| **Response Mode** | Default pass/fail mode behavior for pass/fail questions. |
| **Checklist Questions** | Question list with prompt, response type, options, and required/fail-note flags. |
| **Import as PassFail Questions** | Bulk-adds questions from pasted lines (one prompt per line). |

## Tips

- Use `SiteDefault` when most lines at a site share the same checklist.
- Use `PlantWorkCenter` when line-level differences are important.
- For `Select` response type, include at least two distinct options.
