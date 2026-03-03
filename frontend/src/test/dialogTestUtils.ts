import { screen, within } from '@testing-library/react';

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
) {
  await user.click(trigger);
  const heading = await screen.findByRole('heading', { name: dialogName });
  const fromHeading = heading.closest('[role="dialog"], [role="alertdialog"]');
  if (fromHeading instanceof HTMLElement) {
    return fromHeading;
  }

  const dialog = screen.queryByRole('dialog') ?? screen.queryByRole('alertdialog');
  if (dialog instanceof HTMLElement) {
    return dialog;
  }

  throw new Error(`Dialog for heading "${String(dialogName)}" was not found.`);
}

/**
 * Finds an action button inside a specific dialog, avoiding global name collisions.
 */
export function getDialogActionButton(
  dialog: HTMLElement,
  buttonName: string | RegExp,
) {
  return within(dialog).getByRole('button', { name: buttonName });
}
