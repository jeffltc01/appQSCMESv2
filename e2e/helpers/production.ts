import { type Page, type APIRequestContext, expect } from '@playwright/test';
import { loginViaAPI, setTabletCache, SEED, TEST_USERS } from './auth';

const FRONTEND = 'http://localhost:5173';
const BACKEND = 'http://localhost:5001';
const pl = SEED.productionLines.line1Cleveland;

// ---------------------------------------------------------------------------
// Auth token (for direct API seeding outside of a Page context)
// ---------------------------------------------------------------------------

let _cachedToken: string | null = null;

export async function getAuthToken(request: APIRequestContext): Promise<string> {
  if (_cachedToken) return _cachedToken;

  const configResp = await request.get(
    `${BACKEND}/api/users/login-config?empNo=${TEST_USERS.operator.empNo}`,
  );
  expect(
    configResp.ok(),
    `login-config failed (${configResp.status()}). Is the backend running on port 5001 with seed data?`,
  ).toBeTruthy();
  const config = await configResp.json();

  const loginResp = await request.post(`${BACKEND}/api/auth/login`, {
    data: {
      employeeNumber: TEST_USERS.operator.empNo,
      pin: null,
      siteId: config.defaultSiteId,
      isWelder: false,
    },
  });
  expect(loginResp.ok(), `login failed (${loginResp.status()})`).toBeTruthy();
  const { token } = await loginResp.json();
  _cachedToken = token;
  return token;
}

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

// ---------------------------------------------------------------------------
// Navigate to a work center (login + tablet cache + goto)
// ---------------------------------------------------------------------------

interface WCConfig {
  id: string;
  name: string;
  dataEntryType: string;
  welders: number;
  materialQueueForWCId?: string;
}

export async function navigateToWorkCenter(
  page: Page,
  wc: WCConfig,
  opts?: { assetId?: string; assetName?: string },
) {
  await loginViaAPI(page, TEST_USERS.operator.empNo);
  await setTabletCache(page, {
    workCenterId: wc.id,
    workCenterName: wc.name,
    dataEntryType: wc.dataEntryType,
    productionLineId: pl.id,
    productionLineName: pl.name,
    assetId: opts?.assetId,
    assetName: opts?.assetName,
    materialQueueForWCId: wc.materialQueueForWCId,
    numberOfWelders: wc.welders,
  });
  await page.goto('/operator');
}

// ---------------------------------------------------------------------------
// Barcode simulation via the hidden external-input field
// ---------------------------------------------------------------------------

/**
 * Simulates a barcode scan by toggling external input ON, typing into the
 * hidden input, and pressing Enter. The useBarcode hook parses on Enter.
 */
export async function simulateBarcode(page: Page, barcode: string) {
  const toggle = page.getByRole('button', { name: /external input/i });
  const hiddenInput = page.locator('input[aria-hidden="true"]');

  // If the hidden input isn't visible, toggle external input ON
  if (!(await hiddenInput.count())) {
    await toggle.click();
  }

  await hiddenInput.waitFor({ state: 'attached', timeout: 3000 });
  await hiddenInput.focus();
  await hiddenInput.fill(barcode);
  await page.keyboard.press('Enter');

  // Small pause to let React state updates propagate
  await page.waitForTimeout(300);
}

/**
 * Ensure external input is OFF so manual UI controls are accessible.
 */
export async function ensureManualMode(page: Page) {
  const hiddenInput = page.locator('input[aria-hidden="true"]');
  if ((await hiddenInput.count()) > 0) {
    const toggle = page.getByRole('button', { name: /external input/i });
    await toggle.click();
    await page.waitForTimeout(200);
  }
}

// ---------------------------------------------------------------------------
// API-based material seeding
// ---------------------------------------------------------------------------

export async function seedRollsMaterial(
  request: APIRequestContext,
  token: string,
  wcId: string,
  item: {
    productId: string;
    vendorMillId: string;
    vendorProcessorId: string;
    heatNumber: string;
    coilNumber: string;
    quantity: number;
  },
) {
  const resp = await request.post(`${BACKEND}/api/workcenters/${wcId}/material-queue`, {
    headers: authHeaders(token),
    data: item,
  });
  expect(resp.ok(), `seedRollsMaterial failed: ${resp.status()}`).toBeTruthy();
  return resp.json();
}

export async function seedFitupMaterial(
  request: APIRequestContext,
  token: string,
  wcId: string,
  item: {
    productId: string;
    vendorHeadId: string;
    lotNumber?: string;
    heatNumber?: string;
    coilSlabNumber?: string;
    cardCode: string;
  },
) {
  const resp = await request.post(`${BACKEND}/api/workcenters/${wcId}/fitup-queue`, {
    headers: authHeaders(token),
    data: item,
  });
  expect(resp.ok(), `seedFitupMaterial failed: ${resp.status()}`).toBeTruthy();
  return resp.json();
}

// ---------------------------------------------------------------------------
// Serial number generation
// ---------------------------------------------------------------------------

let _serialCounter = 0;

export function generateShellSerial(): string {
  _serialCounter++;
  const ts = Date.now().toString(36).slice(-5);
  return `E2E${ts}${String(_serialCounter).padStart(2, '0')}`;
}

export function generateNameplateSerial(): string {
  _serialCounter++;
  const ts = Date.now().toString(36).slice(-5);
  return `W00E2E${ts}${_serialCounter}`;
}
