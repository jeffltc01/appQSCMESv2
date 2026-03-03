import { test, expect } from '@playwright/test';
import { loginViaUI, TEST_USERS } from '../helpers/auth';

test.describe('Login', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('shows MES Login heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'MES Login' })).toBeVisible();
  });

  test('admin login lands on /menu', async ({ page }) => {
    await loginViaUI(page, TEST_USERS.admin.empNo);
    await expect(page).toHaveURL('/menu');
    await expect(page.getByText('MES Admin')).toBeVisible();
  });

  test('supervisor login with PIN lands on /menu', async ({ page }) => {
    const { empNo, pin } = TEST_USERS.supervisor;

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

    await expect(page).toHaveURL('/menu');
    await expect(page.getByText('MES Admin').or(page.getByText('MES Menu'))).toBeVisible();
  });

  test('operator without tablet cache lands on /tablet-setup', async ({ page }) => {
    await page.evaluate(() => {
      localStorage.removeItem('cachedWorkCenterId');
    });
    await loginViaUI(page, TEST_USERS.operator.empNo);
    await expect(page).toHaveURL('/tablet-setup');
  });

  test('supervisor login on phone lands on /mobile', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await loginViaUI(page, TEST_USERS.supervisor.empNo, TEST_USERS.supervisor.pin);
    await expect(page).toHaveURL('/mobile');
    await expect(page.getByText('Supervisor Hub')).toBeVisible();
  });

  test('operator login on phone lands on quick actions', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.evaluate(() => {
      localStorage.removeItem('cachedWorkCenterId');
    });
    await loginViaUI(page, TEST_USERS.operator.empNo);
    await expect(page).toHaveURL('/mobile/operator-quick-actions');
    await expect(page.getByText('Operator Quick Actions')).toBeVisible();
  });

  test('invalid employee number shows error', async ({ page }) => {
    await page.locator('#emp-input').fill('INVALID999');
    await page.locator('#emp-input').blur();

    await expect(page.getByText(/not recognized|not found/i)).toBeVisible({ timeout: 5000 });
  });

  test('wrong PIN shows login error', async ({ page }) => {
    const { empNo, pin } = TEST_USERS.supervisor;
    test.skip(!pin, 'Skipping wrong-PIN test when supervisor account has no PIN.');
    const wrongPin = pin === '9999' ? '0000' : '9999';

    const responsePromise = page.waitForResponse((r) =>
      r.url().includes('/api/users/login-config') && r.status() === 200,
    );
    await page.locator('#emp-input').fill(empNo);
    await page.locator('#emp-input').blur();
    await responsePromise;

    await page.locator('#pin-input').fill(wrongPin);
    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page.getByText(/login failed/i)).toBeVisible({ timeout: 5000 });
  });

  test('logout returns to login screen', async ({ page }) => {
    await loginViaUI(page, TEST_USERS.admin.empNo);
    await expect(page).toHaveURL('/menu');

    await page.getByRole('button', { name: 'Logout' }).click();

    await expect(page).toHaveURL(/\/login|\/$/);
    await expect(page.getByRole('heading', { name: 'MES Login' })).toBeVisible();
  });
});
