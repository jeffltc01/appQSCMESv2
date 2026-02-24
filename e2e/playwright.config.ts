import { defineConfig, devices } from '@playwright/test';

const CI = !!process.env.CI;
const DEPLOY_SMOKE = process.env.DEPLOY_SMOKE === 'true';
const FRONTEND_URL = process.env.FRONTEND_URL ?? 'http://localhost:5173';
const BACKEND_URL = process.env.BACKEND_URL ?? 'http://localhost:5001';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 2 : 0,
  workers: CI ? 1 : undefined,
  reporter: CI ? [['html', { open: 'never' }], ['list']] : [['html'], ['list']],
  timeout: 30_000,

  use: {
    baseURL: FRONTEND_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: DEPLOY_SMOKE ? undefined : [
    {
      command: 'dotnet run --project ../backend/MESv2.Api',
      url: `${BACKEND_URL}/healthz`,
      reuseExistingServer: !CI,
      timeout: 30_000,
    },
    {
      command: process.platform === 'win32' ? 'npm.cmd run dev' : 'npm run dev',
      cwd: '../frontend',
      url: FRONTEND_URL,
      reuseExistingServer: !CI,
      timeout: 15_000,
    },
  ],
});
