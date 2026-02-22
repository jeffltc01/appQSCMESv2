import { test, expect } from '@playwright/test';
import { loginViaAPI, TEST_USERS } from '../helpers/auth';

test.describe('Admin - User Maintenance CRUD', () => {
  test.beforeEach(async ({ page }) => {
    await loginViaAPI(page, TEST_USERS.admin.empNo);
    await page.goto('/menu/users');
  });

  test('user list loads with seeded users', async ({ page }) => {
    await expect(page.getByText('User Maintenance')).toBeVisible({ timeout: 10000 });

    await expect(page.getByText('Jeff Thompson')).toBeVisible();
    await expect(page.getByText('Sarah Miller')).toBeVisible();
    await expect(page.getByText('EMP001')).toBeVisible();
  });

  test('add user modal opens and has required fields', async ({ page }) => {
    await expect(page.getByText('User Maintenance')).toBeVisible({ timeout: 10000 });

    await page.getByRole('button', { name: 'Add User' }).click();

    await expect(page.getByText('Add User')).toBeVisible();
    await expect(page.getByText('Employee Number')).toBeVisible();
    await expect(page.getByText('First Name')).toBeVisible();
    await expect(page.getByText('Last Name')).toBeVisible();
    await expect(page.getByText('Display Name')).toBeVisible();
    await expect(page.getByText('Role')).toBeVisible();
    await expect(page.getByText('Default Site')).toBeVisible();
  });

  test('can create a new user', async ({ page }) => {
    await expect(page.getByText('User Maintenance')).toBeVisible({ timeout: 10000 });

    await page.getByRole('button', { name: 'Add User' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();

    const dialog = page.getByRole('dialog');
    const empInput = dialog.locator('input').first();
    await empInput.fill('E2ETEST01');

    const inputs = dialog.locator('input[type="text"], input:not([type])');
    const firstNameInput = inputs.nth(1);
    const lastNameInput = inputs.nth(2);
    const displayNameInput = inputs.nth(3);

    await firstNameInput.fill('E2E');
    await lastNameInput.fill('TestUser');
    await displayNameInput.fill('E2E TestUser');

    await page.getByRole('button', { name: 'Add', exact: true }).click();

    await expect(page.getByRole('dialog')).toBeHidden({ timeout: 10000 });
    await expect(page.getByText('E2E TestUser')).toBeVisible();
  });

  test('can search/filter users', async ({ page }) => {
    await expect(page.getByText('User Maintenance')).toBeVisible({ timeout: 10000 });

    const searchBox = page.getByPlaceholder(/search/i);
    await searchBox.fill('Jeff');

    await expect(page.getByText('Jeff Thompson')).toBeVisible();
    await expect(page.getByText('Sarah Miller')).toBeHidden();
  });
});
