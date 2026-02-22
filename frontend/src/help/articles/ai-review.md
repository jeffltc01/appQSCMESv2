# AI Review (Authorized Inspector Review)

AI Review allows Authorized Inspectors to perform batch reviews of production records. The screen shows a table of today's production records for a selected work center. The AI selects one or more records, optionally add a comment, and submit the annotation in a single batch.

**Access:** Authorized Inspectors (5.5) and Quality / Operations Director (2.0) and above.

## How It Works

1. **Select a work center.** Use the work center dropdown at the top of the screen to choose which station's records to review.
2. **Review the table.** The table loads today's production records for the selected work center. Each row shows key record details alongside a checkbox.
3. **Select records.** Check the boxes next to the records you want to review. Records that have already been reviewed are shown but their checkboxes are disabled.
4. **Add a comment (optional).** Enter an optional comment that will be attached to all selected records in the batch.
5. **Submit.** Tap **Submit Review** to record the review for all selected records. The table refreshes and the reviewed records become disabled.
6. **Auto-refresh.** The table automatically refreshes every 30 seconds to pick up new production records as they are created on the floor.

## Fields & Controls


| Element                        | Description                                                                                               |
| ------------------------------ | --------------------------------------------------------------------------------------------------------- |
| **Work Center dropdown**       | Filters the table to show production records for the selected work center.                                |
| **Record table**               | Displays today's production records with key fields (serial number, product, operator, timestamps, etc.). |
| **Checkbox**                   | Select individual records for batch review. Disabled for already-reviewed records.                        |
| **Comment field**              | Optional text input for a review comment applied to all selected records.                                 |
| **Submit Review button**       | Submits the review for all checked records.                                                               |
| **Already reviewed indicator** | Visual cue (disabled checkbox and/or styling) indicating a record has already been reviewed.              |


## Tips

- You do not need to review every record individually — select a batch and submit them all at once to save time.
- Already-reviewed records remain visible for reference but cannot be reviewed again from this screen.
- The 30-second auto-refresh ensures you see new records without manually reloading. If you are mid-selection, your checkboxes are preserved across refreshes.
- If no records appear, confirm the correct work center is selected and that production has started for the day.

