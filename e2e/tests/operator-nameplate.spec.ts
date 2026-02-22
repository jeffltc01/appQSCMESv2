import { test, expect } from '@playwright/test';
import { loginViaAPI, setTabletCache, SEED, TEST_USERS } from '../helpers/auth';

test.describe('Operator - Nameplate', () => {
  const wc = SEED.workCenters.nameplate;
  const pl = SEED.productionLines.line1Cleveland;

  test.beforeEach(async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.operator.empNo);
    await setTabletCache(page, {
      workCenterId: wc.id,
      workCenterName: wc.name,
      dataEntryType: wc.dataEntryType,
      productionLineId: pl.id,
      productionLineName: pl.name,
      numberOfWelders: wc.welders,
    });
    await page.goto('/operator');
  });

  test('renders nameplate form with product dropdown', async ({ page }) => {
    await expect(page.getByText(wc.name)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Tank Size / Type')).toBeVisible();
    await expect(page.getByText('Serial Number')).toBeVisible();
  });

  test('save button is disabled until form is filled', async ({ page }) => {
    await expect(page.getByText('Tank Size / Type')).toBeVisible({ timeout: 10000 });

    const saveBtn = page.getByRole('button', { name: 'Save' });
    await expect(saveBtn).toBeDisabled();

    const productDropdown = page.getByRole('combobox', { name: /product/i }).or(
      page.locator('text=Select product').first(),
    );
    await productDropdown.click();
    const firstOption = page.getByRole('option').first();
    await firstOption.click();

    await page.getByPlaceholder(/serial number/i).fill('W00999999');

    await expect(saveBtn).toBeEnabled();
  });
});
