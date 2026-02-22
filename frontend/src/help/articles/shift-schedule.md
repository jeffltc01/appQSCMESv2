# Shift Schedule

Shift Schedule manages weekly work schedules for each plant. Each row in the table represents one week and shows the scheduled hours and break minutes for each day (Monday through Sunday). Schedules are used by the Supervisor Dashboard and OEE calculations to determine planned production time.

**Access:** Quality Manager (3.0) and above. Directors (2.0) and above can switch between plants.

## How It Works

1. **View existing schedules.** The table displays all shift schedules for the current plant. Each row shows one week with columns for Monday through Sunday, each displaying the scheduled hours and break minutes.
2. **Create a new schedule.** Click **Add Schedule**. A form opens with:
   - A **week picker** that allows selection from 4 weeks in the past to 26 weeks in the future.
   - Day-by-day entry fields for hours and break minutes (Monday through Sunday).
   - **Preset buttons** for common patterns: "5×8s" (five 8-hour days, Mon–Fri) and "4×10s" (four 10-hour days, Mon–Thu).
   - A **net minutes** display that auto-calculates total available production minutes (total hours minus total breaks) for the week.
3. **Apply a preset.** Tap a preset button to auto-fill the hours and breaks. You can still adjust individual days after applying a preset.
4. **Save the schedule.** Click **Save**. The new week appears in the table.
5. **Edit a schedule.** Click the pencil icon on any schedule row. The form reopens pre-filled with that week's hours and breaks. The week picker is locked — you can change hours and break minutes, then click **Save** to apply updates.
6. **Delete a schedule.** Click the delete action on a schedule row and confirm.

## Fields & Controls

| Element | Description |
|---|---|
| **Plant Selector** | Switches the view to another plant. Visible only to Director (2.0) and above. |
| **Schedule table** | Displays all weekly schedules. Each row = one week, columns = Mon–Sun. |
| **Week Picker** | Selects the week for a new schedule. Range: 4 weeks past to 26 weeks future. |
| **Hours (per day)** | Scheduled work hours for that day (e.g., 8, 10, 0). |
| **Break Minutes (per day)** | Total break minutes for that day (e.g., 30, 60, 0). |
| **Preset: 5×8s** | Auto-fills Mon–Fri with 8 hours each, Sat–Sun with 0. |
| **Preset: 4×10s** | Auto-fills Mon–Thu with 10 hours each, Fri–Sun with 0. |
| **Net Minutes** | Auto-calculated: total scheduled hours converted to minutes, minus total break minutes. |
| **Add Schedule** | Opens the creation form. |
| **Edit (pencil icon)** | Opens the form pre-filled with the selected week's values. The week cannot be changed during edit. |
| **Delete** | Removes a schedule week. |

## Tips

- OEE calculations depend on shift schedules. If no schedule exists for the current week, the Supervisor Dashboard will show a warning that OEE cannot be calculated.
- Use the presets as a starting point and then tweak individual days for weeks with holidays or overtime.
- The net minutes display updates live as you change hours and breaks, so you can see the impact of adjustments immediately.
- Each plant maintains its own independent schedule. If all plants run the same shifts, you still need to create schedules for each plant separately.
