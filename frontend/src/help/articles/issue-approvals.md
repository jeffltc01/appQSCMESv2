# Issue Approvals

Issue Approvals lets authorized reviewers evaluate bug reports, feature requests, and questions submitted through the Report Issue screen. Pending submissions are displayed as a card list. Approving an issue creates a corresponding GitHub issue; rejecting it sends the submitter a notification with optional notes.

**Access:** Requires issue approval permission (typically Administrator or designated reviewers).

## How It Works

1. **Review pending issues.** The screen displays a card list of all issues awaiting review. Each card shows the title, type, submitter, and date submitted.
2. **Open a submission.** Tap a card to view the full issue content in a detail view.
3. **Edit if needed.** The detail view supports inline editing, so you can refine the title, description, or other fields before approving.
4. **Approve.** Tap **Approve** to create a GitHub issue from the submission. The submitter will see a link to the GitHub issue on their card.
5. **Reject.** Tap **Reject** to decline the submission. Optionally add notes explaining why. The submitter sees the rejection status and your notes.

## Fields & Controls

| Element | Description |
|---|---|
| **Pending card list** | Shows all submissions awaiting review, sorted by submission date. |
| **Title** | The issue title. Editable inline before approval. |
| **Type** | Bug Report, Feature Request, or Question. |
| **Submitter** | The user who submitted the issue. |
| **Date Submitted** | When the issue was created. |
| **Detail view** | Full content of the submission with all fields from the original form. |
| **Inline editing** | Allows the reviewer to modify fields before approving. |
| **Approve button** | Creates a GitHub issue and marks the submission as approved. |
| **Reject button** | Marks the submission as rejected. |
| **Rejection Notes** | Optional text field explaining the reason for rejection. Visible to the submitter. |

## Tips

- Edit the title and description before approving if the original submission needs clarification — this ensures the GitHub issue is clear and actionable from the start.
- Rejection notes are visible to the submitter, so provide constructive feedback (e.g., "Duplicate of #42" or "Please add reproduction steps and resubmit").
- Issues that are approved cannot be unapproved. If a GitHub issue was created in error, manage it directly in GitHub.
