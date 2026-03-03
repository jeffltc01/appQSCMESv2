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
  const dialog = await screen.findByRole('dialog');
  await within(dialog).findByRole('heading', { name: dialogName });
  return dialog;
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
