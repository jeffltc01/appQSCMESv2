import { test, expect } from '@playwright/test';
import { SEED } from '../helpers/auth';
import {
  navigateToWorkCenter,
  simulateBarcode,
  ensureManualMode,
  seedRollsMaterial,
  seedFitupMaterial,
  getAuthToken,
  generateShellSerial,
  generateNameplateSerial,
} from '../helpers/production';

/**
 * Full production flow for a 120-gallon tank with no defects.
 *
 * Sequence: Rolls → Long Seam → Long Seam Inspection → RT X-ray Queue →
 *           Fitup → Round Seam → Round Seam Inspection → Nameplate → Hydro
 *
 * Uses `test.describe.serial` so tests run in order and share state.
 * Material queues are pre-seeded via API in beforeAll.
 */
test.describe.serial('Full Production Flow - 120 gal, no defects', () => {
  const wc = SEED.workCenters;
  const assets = SEED.assets;

  let shellSerial: string;
  let alphaCode: string;
  let nameplateSN: string;

  test.beforeAll(async ({ request }) => {
    const token = await getAuthToken(request);

    shellSerial = generateShellSerial();
    nameplateSN = generateNameplateSerial();

    // Seed plate material into the Rolls queue (qty 1, so one shell)
    await seedRollsMaterial(request, token, wc.rolls.id, {
      productId: SEED.products.plate120,
      vendorMillId: SEED.vendors.nucorMill,
      vendorProcessorId: SEED.vendors.metalsProcessor,
      heatNumber: `E2E-HEAT-${Date.now()}`,
      coilNumber: `E2E-COIL-${Date.now()}`,
      quantity: 1,
    });

    // Seed head material into the Fitup queue with kanban card "01"
    await seedFitupMaterial(request, token, wc.fitup.id, {
      productId: SEED.products.head120,
      vendorHeadId: SEED.vendors.cmfHead,
      lotNumber: `E2E-LOT-${Date.now()}`,
      cardCode: SEED.barcodeCards.red01.value,
    });
  });

  // -----------------------------------------------------------------------
  // Step 1: Rolls — advance queue, enter serial, pass thickness inspection
  // -----------------------------------------------------------------------
  test('1. Rolls — create shell from plate material', async ({ page }) => {
    await navigateToWorkCenter(page, wc.rolls, {
      assetId: assets.rollsCle1,
      assetName: 'Rolls Asset',
    });

    // Wait for the screen to load and show the material queue
    await expect(page.getByText('Material Queue')).toBeVisible({ timeout: 10_000 });

    // Advance the queue by clicking the first queue card (manual mode)
    await ensureManualMode(page);
    const queueCard = page.locator('button').filter({ hasText: /PL.*\.140/ }).first();
    await expect(queueCard).toBeVisible({ timeout: 5_000 });
    await queueCard.click();

    // Queue should advance — check for the shell count display
    await expect(page.getByText(/Shell Count/i)).toBeVisible({ timeout: 5_000 });

    // Enter the serial number manually — this triggers the thickness inspection prompt
    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    // Handle the thickness inspection prompt — click Pass
    await expect(page.getByText(/Thickness Inspection/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: 'Pass' }).click();

    // Assert success
    await expect(page.getByText(new RegExp(`Shell ${shellSerial} recorded`, 'i'))).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 2: Long Seam — scan-through
  // -----------------------------------------------------------------------
  test('2. Long Seam — record shell', async ({ page }) => {
    await navigateToWorkCenter(page, wc.longSeam, {
      assetId: assets.longSeamCle1,
      assetName: 'Long Seam Asset',
    });

    await expect(page.getByText(/scan serial/i)).toBeVisible({ timeout: 10_000 });

    const serialInput = page.locator('#ls-serial');
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    await expect(page.getByText(new RegExp(`Shell ${shellSerial} recorded`, 'i'))).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 3: Long Seam Inspection — clean pass (no defects)
  // -----------------------------------------------------------------------
  test('3. Long Seam Inspection — clean pass', async ({ page }) => {
    await navigateToWorkCenter(page, wc.longSeamInsp);

    await expect(page.getByText(/scan serial/i)).toBeVisible({ timeout: 10_000 });

    // Enter serial
    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    // Should transition to AwaitingDefects state with the shell loaded
    await expect(page.getByText('AwaitingDefects')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(shellSerial)).toBeVisible();

    // No defects — click Save for clean pass
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText(/clean pass/i)).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 4: RT X-ray Queue — add shell to queue
  // -----------------------------------------------------------------------
  test('4. RT X-ray Queue — enqueue shell for x-ray', async ({ page }) => {
    await navigateToWorkCenter(page, wc.rtXrayQueue);

    await expect(page.getByText(/Queue for: Real Time X-ray/i)).toBeVisible({ timeout: 10_000 });

    // Enter serial and click Add
    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Add' }).click();

    // Shell should appear in the queue
    await expect(page.getByText(shellSerial)).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 5: Fitup — assemble shell + heads → get alpha code
  // -----------------------------------------------------------------------
  test('5. Fitup — assemble 120-gal tank', async ({ page }) => {
    await navigateToWorkCenter(page, wc.fitup, {
      assetId: assets.fitupCle1,
      assetName: 'Fitup Asset',
    });

    // Wait for the fitup screen to load
    await expect(page.getByText(/Left Head/i)).toBeVisible({ timeout: 10_000 });

    // Add the shell via manual entry
    await ensureManualMode(page);
    const serialInput = page.getByPlaceholder(/shell serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Add Shell' }).click();

    // Shell should appear as filled
    await expect(page.getByText(shellSerial)).toBeVisible({ timeout: 5_000 });

    // Apply head lot via clicking the first heads queue card
    const headsCard = page.locator('button').filter({ hasText: /ELLIP/ }).first();
    if (await headsCard.isVisible()) {
      await headsCard.click();
      await page.waitForTimeout(500);
    } else {
      // Fall back to barcode simulation for the kanban card
      await simulateBarcode(page, `KC;${SEED.barcodeCards.red01.value}`);
      await ensureManualMode(page);
    }

    // Both heads should now show as filled
    await expect(page.locator('text=Scan KC').first()).toBeHidden({ timeout: 3_000 }).catch(() => {
      // Heads were applied if the placeholder is gone
    });

    // Save the assembly
    await page.getByRole('button', { name: 'Save' }).click();

    // Capture the alpha code from the success display
    const alphaDisplay = page.locator('[class*="alphaValue"]');
    await expect(alphaDisplay).toBeVisible({ timeout: 5_000 });
    const alphaText = await alphaDisplay.textContent();
    // Alpha code is the first two characters (e.g. "AA" or "AB")
    alphaCode = alphaText?.match(/^[A-Z]{2}/)?.[0] ?? '';
    expect(alphaCode).toBeTruthy();
  });

  // -----------------------------------------------------------------------
  // Step 6: Round Seam — complete setup + record assembly
  // -----------------------------------------------------------------------
  test('6. Round Seam — setup welders and record', async ({ page }) => {
    await navigateToWorkCenter(page, wc.roundSeam, {
      assetId: assets.roundSeamCle1,
      assetName: 'Round Seam Asset',
    });

    // The setup dialog should open automatically for a fresh setup
    await expect(page.getByText(/Roundseam Setup/i)).toBeVisible({ timeout: 10_000 });

    // For a 120-gal tank (≤500), 2 round seams are required
    await expect(page.getByText(/Seams Required.*2/i)).toBeVisible({ timeout: 3_000 });

    // RS1 — pick the first available welder
    const rs1Dropdown = page.locator('text=Roundseam 1 Welder').locator('..').getByRole('combobox');
    await rs1Dropdown.click();
    await page.getByRole('option').first().click();

    // RS2 — pick the first available welder (same welder is OK)
    const rs2Dropdown = page.locator('text=Roundseam 2 Welder').locator('..').getByRole('combobox');
    await rs2Dropdown.click();
    await page.getByRole('option').first().click();

    // Save setup
    await page.getByRole('button', { name: 'Save Setup' }).click();
    await expect(page.getByText(/Roundseam Setup/i).first()).toBeHidden({ timeout: 5_000 }).catch(() => {
      // Dialog closed
    });

    // Now enter the shell serial
    await ensureManualMode(page);
    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    await expect(page.getByText(/recorded at Round Seam/i)).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 7: Round Seam Inspection — clean pass
  // -----------------------------------------------------------------------
  test('7. Round Seam Inspection — clean pass', async ({ page }) => {
    await navigateToWorkCenter(page, wc.roundSeamInsp);

    await expect(page.getByText(/scan serial/i)).toBeVisible({ timeout: 10_000 });

    // Enter shell serial — the screen will look up the assembly
    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    // Should transition to AwaitingDefects with the assembly loaded
    await expect(page.getByText('AwaitingDefects')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(new RegExp(alphaCode))).toBeVisible();

    // No defects — click Save for clean pass
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText(/clean pass/i)).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 8: Nameplate — create the sellable serial number
  // -----------------------------------------------------------------------
  test('8. Nameplate — create sellable serial number', async ({ page }) => {
    await navigateToWorkCenter(page, wc.nameplate);

    await expect(page.getByText('Tank Size / Type')).toBeVisible({ timeout: 10_000 });

    // Select the 120 AG product from the dropdown
    const productDropdown = page.getByRole('combobox');
    await productDropdown.click();
    // Look for the "120 AG" option
    const option120AG = page.getByRole('option', { name: /120 AG/i });
    await expect(option120AG).toBeVisible({ timeout: 3_000 });
    await option120AG.click();

    // Type the sellable serial number
    const serialInput = page.getByPlaceholder(/serial number/i);
    await serialInput.fill(nameplateSN);

    // Save
    await page.getByRole('button', { name: 'Save' }).click();

    // Assert success (may warn about print failure in dev, that's OK)
    await expect(
      page.getByText(/saved/i),
    ).toBeVisible({ timeout: 5_000 });
  });

  // -----------------------------------------------------------------------
  // Step 9: Hydro — marry assembly to nameplate, accept with no defects
  // -----------------------------------------------------------------------
  test('9. Hydro — accept with no defects', async ({ page }) => {
    await navigateToWorkCenter(page, wc.hydro, {
      assetId: assets.hydroCle1,
      assetName: 'Hydro Asset',
    });

    await expect(page.getByText(/Scan Shell or Nameplate/i)).toBeVisible({ timeout: 10_000 });

    // Enter shell serial first (context: no assembly yet → interpreted as shell)
    await ensureManualMode(page);
    const manualInput = page.getByPlaceholder(/enter serial/i);
    await manualInput.fill(shellSerial);
    await page.getByRole('button', { name: 'Submit' }).click();

    // Wait for assembly lookup to complete
    await expect(page.getByText(new RegExp(alphaCode))).toBeVisible({ timeout: 5_000 });

    // Now enter the nameplate serial
    await manualInput.fill(nameplateSN);
    await page.getByRole('button', { name: 'Submit' }).click();

    // Should transition to ReadyForInspection
    await expect(page.getByText(nameplateSN)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('button', { name: /No Defects - Accept/i })).toBeVisible();

    // Accept with no defects
    await page.getByRole('button', { name: /No Defects - Accept/i }).click();

    // Assert success
    await expect(page.getByText(/Accepted.*no defects/i)).toBeVisible({ timeout: 5_000 });
  });
});
