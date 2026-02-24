import { test, expect } from '@playwright/test';
import type { APIRequestContext } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { FRONTEND_BASE_URL, loginViaAPI, setTabletCache } from '../helpers/auth';
import {
  generateShellSerial,
} from '../helpers/production';

interface TimelineScene {
  id: string;
  screenshot: string;
  durationSec: number;
  caption: string;
  narration: string;
}

interface TimelineFile {
  title: string;
  outputFileName: string;
  scenes: TimelineScene[];
}

interface TrainingContext {
  seedEmpNo: string;
  seedPin: string | null;
  operatorEmpNo: string;
  operatorPin: string | null;
  token: string;
  siteId: string;
  workCenterId: string;
  workCenterName: string;
  workCenterWelders: number;
  productionLineId: string;
  productionLineName: string;
  productId: string;
  millVendorId: string;
  processorVendorId: string;
}

const ROOT = process.cwd();
const TRAINING_DIR = join(ROOT, 'training', 'rolls');
const TIMELINE_PATH = join(TRAINING_DIR, 'rolls-video-timeline.json');
const OUTPUT_DIR = join(ROOT, 'artifacts', 'rolls-training', 'screenshots');
const NOTES_PATH = join(ROOT, 'artifacts', 'rolls-training', 'capture-notes.txt');

async function loadTimeline(): Promise<TimelineFile> {
  const raw = await readFile(TIMELINE_PATH, 'utf8');
  return JSON.parse(raw) as TimelineFile;
}

async function setManualMode(page: import('@playwright/test').Page) {
  const scannerInput = page.locator('input[aria-hidden="true"]');
  if ((await scannerInput.count()) > 0) {
    const externalInputSwitch = page.getByRole('switch', { name: /External Input/i });
    await externalInputSwitch.click();
    await page.waitForTimeout(200);
  }
}

async function loginFromProxy(
  request: APIRequestContext,
  empNo: string,
  pin: string | null,
): Promise<{ token: string; siteId: string }> {
  const configResp = await request.get(`/api/users/login-config?empNo=${encodeURIComponent(empNo)}`, {
    baseURL: FRONTEND_BASE_URL,
  });
  expect(configResp.ok(), `login-config failed for ${empNo} (${configResp.status()})`).toBeTruthy();
  const config = await configResp.json();

  const loginResp = await request.post('/api/auth/login', {
    baseURL: FRONTEND_BASE_URL,
    data: {
      employeeNumber: empNo,
      pin,
      siteId: config.defaultSiteId,
      isWelder: false,
    },
  });
  expect(loginResp.ok(), `login failed for ${empNo} (${loginResp.status()})`).toBeTruthy();
  const body = await loginResp.json();
  return { token: body.token as string, siteId: config.defaultSiteId as string };
}

async function authedGet<T>(request: APIRequestContext, token: string, path: string): Promise<T> {
  const resp = await request.get(path, {
    baseURL: FRONTEND_BASE_URL,
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(resp.ok(), `GET ${path} failed (${resp.status()})`).toBeTruthy();
  return (await resp.json()) as T;
}

async function seedRollsViaProxy(request: APIRequestContext, context: TrainingContext) {
  const now = Date.now();
  const payloads = [
    {
      productId: context.productId,
      vendorMillId: context.millVendorId,
      vendorProcessorId: context.processorVendorId,
      heatNumber: `VID-HEAT-${now}-A`,
      coilNumber: `VID-COIL-${now}-A`,
      quantity: 2,
    },
    {
      productId: context.productId,
      vendorMillId: context.millVendorId,
      vendorProcessorId: context.processorVendorId,
      heatNumber: `VID-HEAT-${now}-B`,
      coilNumber: `VID-COIL-${now}-B`,
      quantity: 1,
    },
  ];

  for (const payload of payloads) {
    const resp = await request.post(`/api/workcenters/${context.workCenterId}/material-queue`, {
      baseURL: FRONTEND_BASE_URL,
      headers: { Authorization: `Bearer ${context.token}` },
      data: payload,
    });
    expect(resp.ok(), `seed material failed (${resp.status()})`).toBeTruthy();
  }
}

async function resolveTrainingContext(request: APIRequestContext): Promise<TrainingContext> {
  const seedEmpNo = process.env.TRAINING_EMP_NO || '1';
  const seedPin = process.env.TRAINING_EMP_PIN || null;
  const login = await loginFromProxy(request, seedEmpNo, seedPin);

  const workCenters = await authedGet<Array<{ id: string; name: string; dataEntryType: string; numberOfWelders: number }>>(
    request,
    login.token,
    '/api/workcenters',
  );
  const rolls = workCenters.find((w) => w.dataEntryType === 'Rolls');
  expect(rolls, 'Could not locate a Rolls work center from /api/workcenters').toBeTruthy();

  const lines = await authedGet<Array<{ id: string; name: string; plantId: string }>>(
    request,
    login.token,
    `/api/productionlines?plantId=${encodeURIComponent(login.siteId)}`,
  );
  expect(lines.length > 0, 'No production lines found for logged-in site').toBeTruthy();

  const products = await authedGet<Array<{ id: string }>>(
    request,
    login.token,
    `/api/products?type=Plate&plantId=${encodeURIComponent(login.siteId)}`,
  );
  expect(products.length > 0, 'No Plate products found for seeding').toBeTruthy();

  const millVendors = await authedGet<Array<{ id: string }>>(
    request,
    login.token,
    `/api/vendors?type=Mill&plantId=${encodeURIComponent(login.siteId)}`,
  );
  expect(millVendors.length > 0, 'No Mill vendors found for seeding').toBeTruthy();

  const processorVendors = await authedGet<Array<{ id: string }>>(
    request,
    login.token,
    `/api/vendors?type=Processor&plantId=${encodeURIComponent(login.siteId)}`,
  );
  expect(processorVendors.length > 0, 'No Processor vendors found for seeding').toBeTruthy();

  const users = await authedGet<Array<{
    employeeNumber: string;
    roleTier: number;
    defaultSiteId: string;
    requirePinForLogin: boolean;
    isActive: boolean;
  }>>(
    request,
    login.token,
    '/api/users/admin',
  );
  const operatorUser = users.find(
    (u) => u.isActive && u.defaultSiteId === login.siteId && u.roleTier >= 6 && !u.requirePinForLogin,
  );
  expect(operatorUser, 'No active operator user without PIN was found in this site').toBeTruthy();

  return {
    seedEmpNo,
    seedPin,
    operatorEmpNo: operatorUser!.employeeNumber,
    operatorPin: null,
    token: login.token,
    siteId: login.siteId,
    workCenterId: rolls!.id,
    workCenterName: rolls!.name,
    workCenterWelders: rolls!.numberOfWelders ?? 1,
    productionLineId: lines[0].id,
    productionLineName: lines[0].name,
    productId: products[0].id,
    millVendorId: millVendors[0].id,
    processorVendorId: processorVendors[0].id,
  };
}

test.describe.serial('Rolls training screenshot capture', () => {
  const timelinePromise = loadTimeline();
  let context: TrainingContext | null = null;

  test.beforeAll(async ({ request }) => {
    await mkdir(OUTPUT_DIR, { recursive: true });
    context = await resolveTrainingContext(request);
    await seedRollsViaProxy(request, context);
  });

  test('captures all screenshot scenes from timeline', async ({ page }) => {
    expect(context, 'Training context should be initialized in beforeAll').toBeTruthy();
    const resolvedContext = context as TrainingContext;
    const timeline = await timelinePromise;
    const expected = new Set(timeline.scenes.map((s) => s.screenshot));
    const captured = new Set<string>();

    const capture = async (fileName: string, waitMs = 400) => {
      await page.waitForTimeout(waitMs);
      await page.screenshot({ path: join(OUTPUT_DIR, fileName), fullPage: false });
      captured.add(fileName);
    };

    await page.setViewportSize({ width: 1920, height: 1080 });
    await loginViaAPI(page, resolvedContext.operatorEmpNo, resolvedContext.operatorPin ?? undefined);
    await setTabletCache(page, {
      workCenterId: resolvedContext.workCenterId,
      workCenterName: resolvedContext.workCenterName,
      dataEntryType: 'Rolls',
      productionLineId: resolvedContext.productionLineId,
      productionLineName: resolvedContext.productionLineName,
      numberOfWelders: resolvedContext.workCenterWelders,
    });
    await page.goto('/operator/');

    // Higher-tier users can land on the admin menu first; hop into Operator View.
    const operatorViewButton = page.getByRole('button', { name: /Operator View/i });
    if (await operatorViewButton.count()) {
      await operatorViewButton.first().click();
    }

    await expect(page.getByText('Material Queue', { exact: true })).toBeVisible({ timeout: 15_000 });
    await setManualMode(page);

    // Scene 01: initial idle state.
    await capture('01_rolls_idle.png');

    // Advance queue by selecting the first queued card in manual mode.
    const queueCardQty1 = page.locator('button').filter({ hasText: /\s1$/ }).first();
    const queueCardQty2 = page.locator('button').filter({ hasText: /\s2$/ }).first();
    const queueCardAny = page.locator('button').filter({ hasText: /\(.+\).+\d+$/ }).first();
    const queueCard = (await queueCardQty1.isVisible()) ? queueCardQty1 : ((await queueCardQty2.isVisible()) ? queueCardQty2 : queueCardAny);
    if (await queueCard.isVisible()) {
      await queueCard.click();
    }
    await expect(page.getByText(/Shell Count/i)).toBeVisible({ timeout: 10_000 });
    await capture('02_queue_advanced.png');

    const serialA = generateShellSerial();
    const serialB = generateShellSerial();
    const serialC = generateShellSerial();

    const serialInput = page.getByPlaceholder(/enter serial number/i);
    await serialInput.fill(serialA);
    await page.getByRole('button', { name: 'Submit' }).click();
    const thicknessPrompt = page.getByText(/Thickness Inspection/i);
    if (await thicknessPrompt.isVisible({ timeout: 2_500 }).catch(() => false)) {
      await capture('03_thickness_prompt.png');
      await page.getByRole('button', { name: 'Pass' }).click();
    } else {
      // Some environments have no active inspection control plan for Rolls.
      await capture('03_thickness_prompt.png');
    }
    await expect(page.getByText(new RegExp(`Shell ${serialA} recorded`, 'i'))).toBeVisible({ timeout: 10_000 });
    await capture('04_shell_recorded.png');

    await serialInput.fill(serialB);
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText(new RegExp(`Shell ${serialB} recorded`, 'i'))).toBeVisible({ timeout: 10_000 });
    await capture('05_second_shell_recorded.png');

    const advancePrompt = page.getByText(/Advance Queue\?/i);
    if (await advancePrompt.isVisible({ timeout: 2_500 }).catch(() => false)) {
      await capture('06_advance_queue_prompt.png');
      await page.getByRole('button', { name: 'No' }).click();
    } else {
      // Fallback frame when the prompt does not appear in this data state.
      await capture('06_advance_queue_prompt.png');
    }

    // Show the label-2 guided state using scanner input simulation.
    const externalInputSwitch = page.getByRole('switch', { name: /External Input/i });
    const scannerInput = page.locator('input[aria-hidden="true"]');
    if ((await scannerInput.count()) === 0) {
      await externalInputSwitch.click();
    }
    await scannerInput.waitFor({ state: 'attached', timeout: 5_000 });
    await scannerInput.fill(`SC;${serialC}/L1`);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(300);
    await expect(page.getByText(/Scan Label 2/i).first()).toBeVisible({ timeout: 5_000 });
    await capture('07_scan_label2_state.png');

    // Return to manual mode and show fallback input controls.
    await setManualMode(page);
    await expect(page.getByText(/Scan Shell Label/i)).toBeVisible({ timeout: 5_000 });
    await capture('08_manual_mode_input.png');

    // Close frame for final recap.
    await capture('09_rolls_close.png');

    const missing = [...expected].filter((file) => !captured.has(file));
    expect(missing, `Missing screenshots: ${missing.join(', ')}`).toEqual([]);

    await writeFile(
      NOTES_PATH,
      [
        `Captured ${captured.size} screenshots for ${timeline.title}.`,
        `Output directory: ${OUTPUT_DIR}`,
      ].join('\n'),
      'utf8',
    );
  });
});
