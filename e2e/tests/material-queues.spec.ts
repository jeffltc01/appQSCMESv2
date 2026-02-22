import { test, expect } from '@playwright/test';
import { SEED } from '../helpers/auth';
import { navigateToWorkCenter } from '../helpers/production';

// ==========================================================================
// Rolls Material Queue — UI tests
// ==========================================================================
test.describe('Material Queue - Rolls Material', () => {
  const wc = SEED.workCenters.rollsMaterial;

  test.beforeEach(async ({ page }) => {
    await navigateToWorkCenter(page, wc);
    await expect(page.getByText(/Material Queue for: Rolls/i)).toBeVisible({ timeout: 10_000 });
  });

  test('renders the queue screen with Add button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /Add Material to Queue/i })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refresh' })).toBeVisible();
  });

  test('add, verify, and delete a plate material item', async ({ page }) => {
    // Open the Add form
    await page.getByRole('button', { name: /Add Material to Queue/i }).click();
    await expect(page.getByText(/Add Material to Queue/i).last()).toBeVisible({ timeout: 3_000 });

    // Select Product — click the "Select Product..." button to open tile selection
    await page.getByRole('button', { name: /Select Product/i }).click();
    await expect(page.getByText(/Select Product/i).last()).toBeVisible({ timeout: 3_000 });
    // Pick the 120-gal plate product
    const plateTile = page.locator('button').filter({ hasText: /120.*PL.*\.140/i }).first();
    await expect(plateTile).toBeVisible({ timeout: 3_000 });
    await plateTile.click();

    // Select Mill
    await page.getByRole('button', { name: /Select Mill/i }).click();
    await expect(page.getByText(/Select Plate Mill/i)).toBeVisible({ timeout: 3_000 });
    const millTile = page.locator('button').filter({ hasText: /Nucor/i }).first();
    await expect(millTile).toBeVisible({ timeout: 3_000 });
    await millTile.click();

    // Select Processor
    await page.getByRole('button', { name: /Select Processor/i }).click();
    await expect(page.getByText(/Select Plate Processor/i)).toBeVisible({ timeout: 3_000 });
    const processorTile = page.locator('button').filter({ hasText: /Metals USA/i }).first();
    await expect(processorTile).toBeVisible({ timeout: 3_000 });
    await processorTile.click();

    // Fill text fields
    const heatField = page.getByText('Heat Number').locator('..').locator('input');
    await heatField.fill('E2E-HEAT-MAT');

    const coilField = page.getByText('Coil Number').locator('..').locator('input');
    await coilField.fill('E2E-COIL-MAT');

    const qtyField = page.getByText('Quantity').locator('..').locator('input');
    await qtyField.fill('3');

    // Save
    await page.getByRole('button', { name: 'Save' }).click();

    // Item should appear in the queue
    await expect(page.getByText(/PL.*\.140/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Qty: 3')).toBeVisible();

    // Delete the item
    await page.getByRole('button', { name: /Delete/i }).first().click();

    // Item should be removed (or queue should be empty)
    await expect(page.getByText('Qty: 3')).toBeHidden({ timeout: 5_000 });
  });
});

// ==========================================================================
// Fitup Queue — UI tests
// ==========================================================================
test.describe('Material Queue - Fitup Queue', () => {
  const wc = SEED.workCenters.fitupQueue;

  test.beforeEach(async ({ page }) => {
    await navigateToWorkCenter(page, wc);
    await expect(page.getByText(/Material Queue for: Fitup/i)).toBeVisible({ timeout: 10_000 });
  });

  test('renders the queue screen with Add button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /Add Material to Queue/i })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refresh' })).toBeVisible();
  });

  test('add a head material item with CMF vendor', async ({ page }) => {
    // Open Add form
    await page.getByRole('button', { name: /Add Material to Queue/i }).click();
    await expect(page.getByText(/Add Material to Queue/i).last()).toBeVisible({ timeout: 3_000 });

    // Select Product — pick 120-gal head
    await page.getByRole('button', { name: /Select Product/i }).click();
    await expect(page.getByText(/Select Product/i).last()).toBeVisible({ timeout: 3_000 });
    const headTile = page.locator('button').filter({ hasText: /120.*ELLIP/i }).first();
    await expect(headTile).toBeVisible({ timeout: 3_000 });
    await headTile.click();

    // Select Head Vendor — pick CMF
    await page.getByRole('button', { name: /Select Vendor/i }).click();
    await expect(page.getByText(/Select Head Vendor/i)).toBeVisible({ timeout: 3_000 });
    const cmfTile = page.locator('button').filter({ hasText: /CMF/i }).first();
    await expect(cmfTile).toBeVisible({ timeout: 3_000 });
    await cmfTile.click();

    // CMF vendor shows Lot Number field (not Heat/Coil)
    const lotField = page.getByText('Lot Number').locator('..').locator('input');
    await expect(lotField).toBeVisible({ timeout: 3_000 });
    await lotField.fill('E2E-LOT-FITUP');

    // Enter Queue Card code
    const cardField = page.getByPlaceholder(/scan card/i);
    await cardField.fill('05');

    // Save
    await page.getByRole('button', { name: 'Save' }).click();

    // Item should appear in the queue
    await expect(page.getByText(/ELLIP/i)).toBeVisible({ timeout: 5_000 });

    // Clean up — delete the item
    await page.getByRole('button', { name: /Delete/i }).first().click();
    await page.waitForTimeout(1_000);
  });
});
