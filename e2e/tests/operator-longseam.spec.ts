import { test, expect } from '@playwright/test';
import { loginViaAPI, setTabletCache, SEED, TEST_USERS } from '../helpers/auth';

test.describe('Operator - Long Seam Inspection', () => {
  const wc = SEED.workCenters.longSeamInsp;
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

  test('renders operator layout with work center name', async ({ page }) => {
    await expect(page.getByText(wc.name)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Sarah Miller')).toBeVisible();
  });

  test('manual serial entry creates a production record', async ({ page }) => {
    await expect(page.getByText(/scan serial/i)).toBeVisible({ timeout: 10000 });

    const serialInput = page.locator('#ls-serial');
    await serialInput.fill('TEST-SERIAL-001');
    await page.getByRole('button', { name: 'Submit' }).click();

    await expect(
      page.getByText(/recorded|success/i),
    ).toBeVisible({ timeout: 10000 });
  });
});
