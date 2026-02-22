# Plant Printers

Plant Printers manages the NiceLabel printers available at each plant. Printers are displayed as a card grid and can be added, edited, or deleted. Each printer is assigned to a specific print location so the system knows which printer to use for nameplate labels, setdown labels, rolls labels, or receiving labels.

**Access:** Administrator (1.0) can add, edit, and delete printers. Other roles can view the list.

## How It Works

1. **Browse printers.** The screen displays a card for each printer at the current plant. Each card shows the printer name, print location, and enabled status.
2. **Add a printer.** Click **Add Printer**. Fill in the printer name, select the plant, choose a print location, and set the enabled status. Click **Save**.
3. **Edit a printer.** Click on a printer card to open the edit form. Update any fields and click **Save**.
4. **Delete a printer.** Click the delete action on a printer card and confirm. The printer is permanently removed.

## Fields & Controls

| Field | Required | Description |
|---|---|---|
| **Printer Name** | Yes | The display name of the printer (should match the NiceLabel printer name). |
| **Plant** | Yes | The plant where this printer is installed. |
| **Print Location** | Yes | Where the printer is used. Selected from a fixed list: Nameplate, Setdown, Rolls, or Receiving. |
| **Enabled** | No | Checkbox. When unchecked, the printer will not appear in print selection lists at work centers. Defaults to enabled. |

## Tips

- The Print Location determines which work center screens offer this printer. For example, a printer set to "Rolls" will only appear as an option on the Rolls work center screen.
- Disabling a printer is preferable to deleting it if the printer is temporarily offline — this preserves the configuration for when it comes back.
- Printer names should match exactly what is configured in NiceLabel, including capitalization, to avoid print routing errors.
