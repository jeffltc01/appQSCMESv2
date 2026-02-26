import { test, expect, type Page } from '@playwright/test';
import { SEED } from '../helpers/auth';

const wc = SEED.workCenters.rolls;
const pl = SEED.productionLines.line1Cleveland;

async function routeWcProductionLineConfig(page: Page) {
  await page.route('**/api/workcenters/*/production-lines', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 'wcpl-test-1',
          workCenterId: wc.id,
          productionLineId: pl.id,
          productionLineName: pl.name,
          sequence: 1,
          isActive: true,
          numberOfWelders: wc.welders,
          downtimeTrackingEnabled: true,
          downtimeThresholdMinutes: 0.01,
        },
      ]),
    });
  });
}

async function seedOperatorSession(page: Page) {
  await page.addInitScript(([workCenterId, workCenterName, dataEntryType, productionLineId, productionLineName, assetId]) => {
    sessionStorage.setItem('mes_auth', JSON.stringify({
      token: 'e2e-token',
      isWelder: false,
      user: {
        id: '00000000-0000-0000-0000-000000000002',
        employeeNumber: 'EMP002',
        displayName: 'E2E Operator',
        roleTier: 6,
        roleName: 'Operator',
        defaultSiteId: '11111111-1111-1111-1111-111111111111',
        isCertifiedWelder: false,
        userType: 0,
        plantCode: '000',
        plantName: 'Cleveland',
        plantTimeZoneId: 'America/Chicago',
      },
    }));

    localStorage.setItem('cachedWorkCenterId', workCenterId);
    localStorage.setItem('cachedWorkCenterName', workCenterName);
    localStorage.setItem('cachedWorkCenterDisplayName', workCenterName);
    localStorage.setItem('cachedDataEntryType', dataEntryType);
    localStorage.setItem('cachedProductionLineId', productionLineId);
    localStorage.setItem('cachedProductionLineName', productionLineName);
    localStorage.setItem('cachedAssetId', assetId);
    localStorage.setItem('cachedAssetName', 'Rolls Asset 1');
    localStorage.setItem('cachedNumberOfWelders', '1');
  }, [wc.id, wc.name, wc.dataEntryType, pl.id, pl.name, SEED.assets.rollsCle1]);
}

test.describe('Operator downtime overlay', () => {
  test.beforeEach(async ({ page }) => {
    await seedOperatorSession(page);
    await routeWcProductionLineConfig(page);
  });

  test('shows assigned reason buttons after inactivity', async ({ page }) => {
    await page.route('**/api/workcenters/*/production-lines/*/downtime-config', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          downtimeTrackingEnabled: true,
          downtimeThresholdMinutes: 0.01,
          applicableReasons: [
            {
              id: 'reason-breakdown',
              downtimeReasonCategoryId: 'category-equipment',
              categoryName: 'Equipment',
              name: 'Breakdown',
              isActive: true,
              countsAsDowntime: true,
              sortOrder: 0,
            },
          ],
        }),
      });
    });

    await page.goto('/operator');

    await expect(page.getByText('Looks like there was some downtime')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('button', { name: 'Breakdown' })).toBeVisible();
  });

  test('shows explicit config error state when downtime-config refresh fails', async ({ page }) => {
    let configGetCount = 0;
    await page.route('**/api/workcenters/*/production-lines/*/downtime-config', async (route) => {
      configGetCount += 1;

      // StrictMode can call mount effects more than once in development.
      // Keep initial fetches successful, then fail the first inactivity refresh.
      if (configGetCount <= 2) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            downtimeTrackingEnabled: true,
            downtimeThresholdMinutes: 0.01,
            applicableReasons: [],
          }),
        });
        return;
      }

      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'forced config failure' }),
      });
    });

    await page.goto('/operator');

    await expect(page.getByTestId('downtime-config-error')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/Unable to load downtime reasons for this station/)).toBeVisible();
  });
});
