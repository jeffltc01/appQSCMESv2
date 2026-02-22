# Annotation Types

Configure the types of annotations that can be applied to production records (e.g., Quality Hold, Rework, Scrap). Annotation types appear as a card grid with color swatches. Add, edit, or delete types as needed. Deleting is a hard delete. Admin only (Role 1.0).

## How It Works

1. **Browse.** Each card shows the annotation type name, abbreviation, display color swatch, and key settings.
2. **Add an annotation type.** Click **Add Annotation Type**. Fill in the fields and click **Save**.
3. **Edit an annotation type.** Click a card to open the edit form. Update fields and click **Save**.
4. **Delete an annotation type.** Click a card and choose **Delete**. This is a permanent hard delete.

## Fields & Controls

| Element | Description |
|---|---|
| **Name** | The full name of the annotation type (e.g., "Quality Hold"). Required. |
| **Abbreviation** | A short code displayed on badges and annotation icons (e.g., "QH"). Required. |
| **Requires Resolution** | Checkbox. When checked, annotations of this type must be explicitly resolved before the serial number can proceed. |
| **Operator Can Create** | Checkbox. When checked, floor operators can create annotations of this type directly from operator screens. When unchecked, only supervisors and above can create them. |
| **Display Color** | A hex color code (e.g., "#FF0000") used for the annotation icon and badge on production screens. A color swatch preview is shown on the card. Required. |

## Tips

- Choose distinct, high-contrast colors for each type so operators can quickly identify annotation icons on the floor.
- The Requires Resolution setting is important for quality holds — it ensures the issue is tracked to completion before the tank moves forward.
- Deleting an annotation type is permanent. If annotations of that type already exist on production records, consider deactivating it instead if that option is available, or renaming it with a "RETIRED" prefix.
