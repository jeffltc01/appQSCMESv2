import { type Page, expect } from '@playwright/test';

export const TEST_USERS = {
  admin: { empNo: 'EMP001' },
  operator: { empNo: 'EMP002' },
  welder: { empNo: 'EMP003' },
  supervisor: { empNo: 'EMP005', pin: '1234' },
} as const;

/**
 * Logs in through the UI by filling the employee number (and PIN if needed),
 * then clicking the Login button. Waits for navigation away from login.
 */
export async function loginViaUI(
  page: Page,
  empNo: string,
  pin?: string,
) {
  await page.goto('/');

  const responsePromise = page.waitForResponse((r) =>
    r.url().includes('/api/users/login-config') && r.status() === 200,
  );
  await page.locator('#emp-input').fill(empNo);
  await page.locator('#emp-input').blur();
  await responsePromise;

  if (pin) {
    await expect(page.locator('#pin-input')).toBeVisible();
    await page.locator('#pin-input').fill(pin);
  }

  await page.getByRole('button', { name: 'Login' }).click();

  await page.waitForURL((url) => !url.pathname.includes('/login') && url.pathname !== '/');
}

/**
 * Authenticates via API (routed through the Vite proxy) and injects the
 * auth state into sessionStorage, bypassing the login UI entirely.
 * Much faster for tests that don't need to verify the login flow.
 *
 * Requires the backend to be running with seed data (Development mode).
 */
export async function loginViaAPI(page: Page, empNo: string, pin?: string) {
  await page.goto('/');

  const configResp = await page.request.get(
    `/api/users/login-config?empNo=${empNo}`,
    { baseURL: 'http://localhost:5173' },
  );
  expect(configResp.ok(), `login-config for ${empNo} failed (${configResp.status()}). Is the backend running with seed data?`).toBeTruthy();
  const config = await configResp.json();

  const loginResp = await page.request.post('/api/auth/login', {
    baseURL: 'http://localhost:5173',
    data: {
      employeeNumber: empNo,
      pin: pin ?? null,
      siteId: config.defaultSiteId,
      isWelder: false,
    },
  });
  expect(loginResp.ok(), `login for ${empNo} failed (${loginResp.status()})`).toBeTruthy();
  const { token, user } = await loginResp.json();

  const authState = JSON.stringify({ user, token, isWelder: false });

  await page.evaluate((state) => {
    sessionStorage.setItem('mes_auth', state);
  }, authState);
}

/**
 * Sets up localStorage with tablet cache values so the operator layout
 * renders the correct work center screen.
 */
export async function setTabletCache(
  page: Page,
  cache: {
    workCenterId: string;
    workCenterName: string;
    displayName?: string;
    dataEntryType: string;
    productionLineId: string;
    productionLineName: string;
    assetId?: string;
    assetName?: string;
    materialQueueForWCId?: string;
    numberOfWelders?: number;
  },
) {
  await page.evaluate((c) => {
    localStorage.setItem('cachedWorkCenterId', c.workCenterId);
    localStorage.setItem('cachedWorkCenterName', c.workCenterName);
    localStorage.setItem('cachedWorkCenterDisplayName', c.displayName ?? c.workCenterName);
    localStorage.setItem('cachedDataEntryType', c.dataEntryType);
    localStorage.setItem('cachedProductionLineId', c.productionLineId);
    localStorage.setItem('cachedProductionLineName', c.productionLineName);
    localStorage.setItem('cachedAssetId', c.assetId ?? '');
    localStorage.setItem('cachedAssetName', c.assetName ?? '');
    if (c.materialQueueForWCId) {
      localStorage.setItem('cachedMaterialQueueForWCId', c.materialQueueForWCId);
    }
    localStorage.setItem('cachedNumberOfWelders', String(c.numberOfWelders ?? 0));
  }, cache);
}

export async function clearTabletCache(page: Page) {
  await page.evaluate(() => {
    const keys = [
      'cachedWorkCenterId', 'cachedWorkCenterName', 'cachedWorkCenterDisplayName',
      'cachedDataEntryType', 'cachedProductionLineId', 'cachedProductionLineName',
      'cachedAssetId', 'cachedAssetName', 'cachedMaterialQueueForWCId',
      'cachedNumberOfWelders',
    ];
    keys.forEach((k) => localStorage.removeItem(k));
  });
}

/** Well-known GUIDs from the dev seed data. */
export const SEED = {
  plants: {
    cleveland: '11111111-1111-1111-1111-111111111111',
    fremont: '22222222-2222-2222-2222-222222222222',
  },
  workCenters: {
    rolls:         { id: 'f1111111-1111-1111-1111-111111111111', name: 'Rolls',                 dataEntryType: 'Rolls',               welders: 1 },
    rollsMaterial: { id: 'fb111111-1111-1111-1111-111111111111', name: 'Rolls Material',        dataEntryType: 'MatQueue-Material',    welders: 0, materialQueueForWCId: 'f1111111-1111-1111-1111-111111111111' },
    longSeam:      { id: 'f2111111-1111-1111-1111-111111111111', name: 'Long Seam',             dataEntryType: 'Barcode-LongSeam',     welders: 1 },
    longSeamInsp:  { id: 'f3111111-1111-1111-1111-111111111111', name: 'Long Seam Inspection',  dataEntryType: 'Barcode-LongSeamInsp', welders: 0 },
    rtXrayQueue:   { id: 'f4111111-1111-1111-1111-111111111111', name: 'RT X-ray Queue',        dataEntryType: 'MatQueue-Shell',       welders: 0 },
    fitup:         { id: 'f5111111-1111-1111-1111-111111111111', name: 'Fitup',                 dataEntryType: 'Fitup',                welders: 1 },
    fitupQueue:    { id: 'fc111111-1111-1111-1111-111111111111', name: 'Fitup Queue',           dataEntryType: 'MatQueue-Fitup',       welders: 0, materialQueueForWCId: 'f5111111-1111-1111-1111-111111111111' },
    roundSeam:     { id: 'f6111111-1111-1111-1111-111111111111', name: 'Round Seam',            dataEntryType: 'Barcode-RoundSeam',    welders: 1 },
    roundSeamInsp: { id: 'f7111111-1111-1111-1111-111111111111', name: 'Round Seam Inspection', dataEntryType: 'Barcode-RoundSeamInsp',welders: 0 },
    spotXray:      { id: 'f8111111-1111-1111-1111-111111111111', name: 'Spot X-ray',            dataEntryType: 'Spot',                 welders: 0 },
    nameplate:     { id: 'f9111111-1111-1111-1111-111111111111', name: 'Nameplate',             dataEntryType: 'DataPlate',            welders: 0 },
    hydro:         { id: 'fa111111-1111-1111-1111-111111111111', name: 'Hydro',                 dataEntryType: 'Hydro',                welders: 0 },
  },
  assets: {
    rollsCle1:     'a0000001-0000-0000-0000-000000000001',
    longSeamCle1:  'a0000001-0000-0000-0000-000000000002',
    fitupCle1:     'a0000001-0000-0000-0000-000000000003',
    roundSeamCle1: 'a0000001-0000-0000-0000-000000000004',
    hydroCle1:     'a0000001-0000-0000-0000-000000000005',
  },
  products: {
    plate120:    'b1011111-1111-1111-1111-111111111111',
    head120:     'b2011111-1111-1111-1111-111111111111',
    sellable120AG: 'b5011111-1111-1111-1111-111111111111',
  },
  vendors: {
    nucorMill:       '51000001-0000-0000-0000-000000000001',
    metalsProcessor: '52000001-0000-0000-0000-000000000001',
    cmfHead:         '53000001-0000-0000-0000-000000000001',
  },
  barcodeCards: {
    red01: { value: '01', id: 'bc000001-0000-0000-0000-000000000001' },
  },
  productionLines: {
    line1Cleveland: { id: 'e1111111-1111-1111-1111-111111111111', name: 'Line 1' },
    line2Cleveland: { id: 'e2111111-1111-1111-1111-111111111111', name: 'Line 2' },
  },
} as const;
