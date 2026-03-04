const e=`# Checklist Score Types

Score Types define reusable scoring options for checklist questions that use the \`Score\` response type.

## Access

- Open \`Menu -> Checklist Score Types\`.
- Only **Administrator (1.0)** and **Director (2.0)** roles can create, edit, or archive score types.

## What You Can Do

- Create a score type with a unique name.
- Add one or more score values (numeric score + description).
- Edit an existing score type and reorder or adjust values.
- Archive a score type when it should no longer be used.

## Archiving Behavior

- Archiving sets the score type inactive.
- Inactive score types are not valid for new checklist template score questions.
- Existing historical checklist responses remain intact.

## Template Integration

- In Checklist Template Editor, when a question uses \`Score\`, a score type selection is required.
- Question response options are loaded from the selected score type values.
`;export{e as default};
