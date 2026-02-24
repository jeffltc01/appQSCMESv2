# Rolls Work Center Training Video Script

This script is derived from `frontend/src/help/articles/rolls.md` and `designInput/SPEC_WC_ROLLS.md`.

## Audience
- Rolls operators and team leads onboarding to MES v2.

## Runtime
- Target: 80-100 seconds.

## Scene Script + Shot Map

1. **Scene 1 - Title and purpose**
   - **Visual**: `01_rolls_idle.png`
   - **Narration**: "This is the Rolls station in MES v2. Think of this as the handoff point where raw plate becomes a traceable shell record that downstream stations can trust."
   - **On-screen caption**: "Rolls = first station in shell production."

2. **Scene 2 - Advance material queue**
   - **Visual**: `02_queue_advanced.png`
   - **Narration**: "Before any shell scan, advance the material queue. That activates the batch and auto-loads size, heat, coil, planned quantity, and remaining count."
   - **On-screen caption**: "Advance queue before scanning shell labels."

3. **Scene 3 - First shell and thickness prompt**
   - **Visual**: `03_thickness_prompt.png`
   - **Narration**: "On the first shell of a batch, Rolls asks for thickness confirmation. Treat this as a quick quality gate before you settle into a repeat rhythm."
   - **On-screen caption**: "First shell requires thickness inspection."

4. **Scene 4 - Record created and history update**
   - **Visual**: `04_shell_recorded.png`
   - **Narration**: "Once the label pair is valid, the system writes the production record and updates history immediately, so traceability stays current in real time."
   - **On-screen caption**: "Matched labels create a production record."

5. **Scene 5 - Continue normal flow**
   - **Visual**: `05_second_shell_recorded.png`
   - **Narration**: "From here, it is a repeat loop. Each accepted shell increments your count and burns down remaining material with no extra bookkeeping."
   - **On-screen caption**: "Repeat for each shell in the active batch."

6. **Scene 6 - Queue advance prompt**
   - **Visual**: `06_advance_queue_prompt.png`
   - **Narration**: "When remaining hits zero, Rolls prompts you to advance. Pick Yes for the next batch, or No if floor reality does not match expected quantity."
   - **On-screen caption**: "Zero remaining triggers queue advance prompt."

7. **Scene 7 - Label state guidance**
   - **Visual**: `07_scan_label2_state.png`
   - **Narration**: "Use the scan-state banner as your guide. After label one, the screen explicitly waits for label two to prevent accidental mis-pairing."
   - **On-screen caption**: "Follow the scan state indicator."

8. **Scene 8 - Manual mode fallback**
   - **Visual**: `08_manual_mode_input.png`
   - **Narration**: "If scanner input is unavailable, switch to manual entry. The rules do not change, you are only swapping how the serial gets into the system."
   - **On-screen caption**: "Manual mode is available when scanners are offline."

9. **Scene 9 - Close**
   - **Visual**: `09_rolls_close.png`
   - **Narration**: "The operating pattern is simple: activate batch, verify labels, clear quality checks, and keep an eye on remaining. If the queue is empty, pull in Material Handling."
   - **On-screen caption**: "Advance, scan, inspect, record."

## Capture Notes
- Use 1920x1080 screenshots.
- Capture from the real app with seeded data.
- Keep overlays and prompts visible long enough for screenshot clarity.
