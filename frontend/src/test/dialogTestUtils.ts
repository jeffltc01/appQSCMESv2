import { screen, waitFor, within } from '@testing-library/react';

type Clicker = {
  click: (element: Element) => Promise<void>;
};

/**
 * Opens a dialog by clicking a trigger button and waiting for the dialog to render.
 */
export async function openDialogByTrigger(
  user: Clicker,
  trigger: Element,
  dialogName: string | RegExp,
): Promise<HTMLElement> {
  await user.click(trigger);

  let matchedDialog: HTMLElement | null = null;
  await waitFor(() => {
    const dialogs = screen.queryAllByRole('dialog', { hidden: true });
    matchedDialog = dialogs.find((dialog) =>
      within(dialog).queryByText(dialogName, { selector: 'h1,h2,h3,h4,h5,h6,[role="heading"]' }),
    ) as HTMLElement | undefined ?? null;

    if (!matchedDialog) {
      throw new Error(`Dialog for heading "${String(dialogName)}" was not found.`);
    }
  });

  if (!matchedDialog) {
    throw new Error(`Dialog for heading "${String(dialogName)}" was not found.`);
  }

  return matchedDialog;
}

/**
 * Finds an action button inside a specific dialog, avoiding global name collisions.
 */
export function getDialogActionButton(
  dialog: HTMLElement,
  buttonName: string | RegExp,
) {
  return within(dialog).getByRole('button', { name: buttonName, hidden: true });
}
