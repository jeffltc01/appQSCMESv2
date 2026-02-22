# Report Issue

Report Issue lets any user submit bug reports, feature requests, or general questions about MES. The screen has two views: a card list of your own past submissions, and a multi-step creation form that adapts its fields based on the issue type you select.

**Access:** All authenticated users can submit issues and view their own submissions.

## How It Works

### Viewing Past Submissions

1. **Open Report Issue.** The default view shows a card list of issues you have previously submitted.
2. **Check status.** Each card displays a status badge indicating where your issue is in the pipeline (e.g., Pending, Approved, Rejected).
3. **View details.** Tap a card to see the full issue content and any reviewer notes.

### Creating a New Issue

1. **Start a new issue.** Tap the **New Issue** button. A multi-step form opens.
2. **Select the type.** Choose Bug Report, Feature Request, or Question. The form fields update to match your selection.
3. **Fill in the fields.** Complete all required fields for the chosen type (see below).
4. **Submit.** Review your entries and tap **Submit**. The issue enters the approval queue.
5. **After approval.** Once an administrator approves your issue, a GitHub issue is created and the link appears on your submission card.

## Fields & Controls

### Bug Report

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | A short summary of the bug. |
| **Area** | Yes | The app area affected, selected from a dropdown of approximately 13 areas. |
| **Description** | Yes | A detailed explanation of the problem. |
| **Steps to Reproduce** | Yes | Step-by-step instructions to trigger the bug. |
| **Expected Behavior** | Yes | What should have happened. |
| **Actual Behavior** | Yes | What actually happened instead. |
| **Screenshots** | No | Upload one or more screenshots showing the issue. |
| **Browser** | No | The browser and version you were using. |
| **Severity** | Yes | How impactful the bug is (e.g., Critical, High, Medium, Low). |

### Feature Request

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | A short summary of the feature. |
| **Area** | Yes | The app area this feature relates to. |
| **Problem Statement** | Yes | What problem or gap this feature would address. |
| **Desired Feature** | Yes | A description of what you want the feature to do. |
| **Alternatives Considered** | No | Any workarounds or alternative approaches you have tried. |
| **Priority** | Yes | How important this feature is to your workflow. |

### Question

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | A short summary of your question. |
| **Area** | Yes | The app area your question is about. |
| **Question** | Yes | The full question you need answered. |
| **Context** | No | Any background information that helps answer the question. |

### Common Controls

| Element | Description |
|---|---|
| **New Issue button** | Opens the multi-step creation form. |
| **Status badge** | Shows the current state of a submission (Pending, Approved, Rejected). |
| **GitHub link** | Appears on approved issues, linking to the created GitHub issue. |

## Tips

- Be as specific as possible in bug reports — clear reproduction steps dramatically speed up resolution.
- The Area dropdown helps route your issue to the right person. Pick the most specific area rather than "General."
- You will not see issues submitted by other users. To review and approve all submissions, use the Issue Approvals screen (requires approval permission).
