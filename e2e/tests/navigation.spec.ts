import { test, expect } from '@playwright/test';
import { loginViaAPI, setTabletCache, clearTabletCache, TEST_USERS, SEED } from '../helpers/auth';

test.describe('Navigation & Role-Based Access', () => {
  test('unauthenticated user sees login screen', async ({ page }) => {
    await page.goto('/menu');
    await expect(page.getByRole('heading', { name: 'MES Login' })).toBeVisible();
  });

  test('admin sees all menu tiles', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.admin.empNo);
    await page.goto('/menu');

    await expect(page.getByText('MES Admin')).toBeVisible({ timeout: 10000 });

    const expectedTiles = [
      'Product Maintenance', 'User Maintenance', 'Vendor Maintenance',
      'Work Center Config', 'Defect Codes', 'Defect Locations',
      'Asset Management', 'Kanban Card Mgmt', 'Characteristics',
      'Control Plans', 'Plant Gear', "Who's On the Floor",
      'Annotation Types', 'Production Lines', 'Annotations',
      'Serial Number Lookup', 'Sellable Tank Status', 'Plant Printers',
      'Operator View',
    ];

    for (const tile of expectedTiles) {
      await expect(page.getByRole('button', { name: tile })).toBeVisible();
    }
  });

  test('supervisor sees tiles up to tier 4', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.supervisor.empNo, TEST_USERS.supervisor.pin);
    await page.goto('/menu');

    await expect(page.getByText('MES Admin')).toBeVisible({ timeout: 10000 });

    await expect(page.getByRole('button', { name: 'Sellable Tank Status' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Kanban Card Mgmt' })).toBeVisible();

    await expect(page.getByRole('button', { name: 'Work Center Config' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'Control Plans' })).toBeHidden();
  });

  test('operator with tablet cache redirects to /operator', async ({ page }) => {
    const wc = SEED.workCenters.longSeamInsp;
    const pl = SEED.productionLines.line1Cleveland;

    await loginViaAPI(page, TEST_USERS.operator.empNo);
    await setTabletCache(page, {
      workCenterId: wc.id,
      workCenterName: wc.name,
      dataEntryType: wc.dataEntryType,
      productionLineId: pl.id,
      productionLineName: pl.name,
      numberOfWelders: wc.welders,
    });

    await page.goto('/');
    await expect(page).toHaveURL(/\/operator/, { timeout: 10000 });
  });

  test('operator without tablet cache redirects to /tablet-setup', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.operator.empNo);
    await clearTabletCache(page);

    await page.goto('/');
    await expect(page).toHaveURL(/\/tablet-setup/, { timeout: 10000 });
  });

  test('admin can navigate to a sub-screen and back to menu', async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.admin.empNo);
    await page.goto('/menu');

    await expect(page.getByText('MES Admin')).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: 'Defect Codes' }).click();

    await expect(page.getByText('Defect Codes')).toBeVisible({ timeout: 10000 });

    await page.getByRole('button', { name: 'Menu' }).click();
    await expect(page).toHaveURL('/menu');
  });
});
