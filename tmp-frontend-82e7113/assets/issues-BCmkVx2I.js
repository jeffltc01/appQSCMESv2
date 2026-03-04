const e=`# Issues

The Issues screen combines request submission and review workflows in one place. Users can submit bug reports, feature requests, and general questions; approvers can review pending items and approve or deny them.

## How It Works

1. **Open Issues.** The default list shows your issue requests.
2. **Filter list.** Use search, type, and status filters to narrow results.
3. **Create request.** Click **New Issue**, select request type, complete required fields, and submit.
4. **Open full details.** Click the eye icon on any card to review full request details in a modal.
5. **Review pending (approvers).** Enable **Needs Approval Only** to focus on requests waiting for action.
6. **Approve or deny.** From the details modal, approve (creates GitHub issue) or deny with optional notes.

## Request Types

### Bug Report

| Field | Required | Description |
|---|---|
| **Title** | Yes | Short summary of the bug. |
| **Area** | Yes | App area affected. |
| **Describe the Bug** | Yes | What is broken. |
| **Steps to Reproduce** | Yes | How to reproduce the issue. |
| **Expected Behavior** | Yes | Expected outcome. |
| **Actual Behavior** | Yes | Actual outcome. |
| **Screenshots or Error Messages** | No | Supporting details. |
| **Browser** | Yes | Browser in use when issue occurred. |
| **Severity** | Yes | Impact level. |

### Feature Request

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | Short summary of the request. |
| **Area** | Yes | App area impacted. |
| **What Problem Does This Solve?** | Yes | Problem being addressed. |
| **Describe the Feature You'd Like** | Yes | Desired behavior. |
| **Alternatives or Workarounds** | No | Existing workaround context. |
| **How Important Is This to You?** | Yes | Priority/importance statement. |
| **Additional Context** | No | Extra background. |

### General Question

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | Short summary of the question. |
| **Area** | Yes | App area related to the question. |
| **Your Question** | Yes | Main question text. |
| **Additional Context** | No | Helpful supporting information. |

## Key Controls

| Element | Description |
|---|---|
| **New Issue** | Opens the multi-type submission form. |
| **Search / Type / Status filters** | Narrows visible cards in list view. |
| **Needs Approval Only** | Reviewer toggle for pending-only queue. |
| **View details (eye icon)** | Opens a full details modal for any issue card. |
| **Review icon** | Opens the approval dialog for pending requests. |
| **Approve** | Approves request and creates corresponding GitHub issue. |
| **Deny** | Rejects request with optional reviewer notes. |
| **GitHub link on card** | Visible after approval when issue number/link exists. |

## Tips

- Be specific in bug steps and expected/actual behavior to improve triage quality.
- Use search with status/type filters to find older requests quickly.
- Reviewers should add denial notes so submitters know what to fix before resubmitting.
`;export{e as default};
