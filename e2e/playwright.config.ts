import { defineConfig, devices } from '@playwright/test';

const CI = !!process.env.CI;

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 2 : 0,
  workers: CI ? 1 : undefined,
  reporter: CI ? [['html', { open: 'never' }], ['list']] : [['html'], ['list']],
  timeout: 30_000,

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --project ../backend/MESv2.Api',
      url: 'http://localhost:5001/healthz',
      reuseExistingServer: !CI,
      timeout: 30_000,
    },
    {
      command: process.platform === 'win32' ? 'npm.cmd run dev' : 'npm run dev',
      cwd: '../frontend',
      url: 'http://localhost:5173',
      reuseExistingServer: !CI,
      timeout: 15_000,
    },
  ],
});
