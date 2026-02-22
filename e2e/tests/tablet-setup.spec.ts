import { test, expect } from '@playwright/test';
import { loginViaAPI, clearTabletCache, TEST_USERS } from '../helpers/auth';

test.describe('Tablet Setup', () => {
  test('supervisor can configure tablet and land on /operator', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.supervisor.empNo, TEST_USERS.supervisor.pin);
    await clearTabletCache(page);
    await page.goto('/tablet-setup');

    await expect(page.getByRole('heading', { name: 'Tablet Setup' })).toBeVisible();
    await expect(page.getByText('one-time task')).toBeVisible();

    const wcDropdown = page.getByText('Select a work center...');
    await wcDropdown.click();
    await page.getByRole('option', { name: 'Long Seam Inspection' }).click();

    const plDropdown = page.locator('text=*Production Line').locator('..').locator('[role="combobox"]');
    await expect(plDropdown).toBeVisible();

    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page).toHaveURL(/\/operator/, { timeout: 10000 });
  });

  test('operator (tier 6) sees access restriction message', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.operator.empNo);
    await clearTabletCache(page);
    await page.goto('/tablet-setup');

    await expect(page.getByRole('heading', { name: 'Tablet Setup' })).toBeVisible();
    await expect(
      page.getByText('Please contact a Team Lead or Supervisor'),
    ).toBeVisible();
  });
});
