#!/usr/bin/env node
import { spawn } from 'node:child_process';

const isDryRun = process.argv.includes('--dry-run');
const requiredEnv = ['BACKEND_URL', 'FRONTEND_URL'];
const missing = requiredEnv.filter((name) => !process.env[name] || !process.env[name].trim());
if (missing.length > 0 && !isDryRun) {
  console.error(`Missing required env var(s): ${missing.join(', ')}`);
  console.error('Set BACKEND_URL and FRONTEND_URL, then rerun `npm run smoke:deploy`.');
  process.exit(1);
}

process.env.DEPLOY_SMOKE = 'true';
const command = process.platform === 'win32' ? 'npx.cmd' : 'npx';
const args = [
  'playwright',
  'test',
  'tests/login.spec.ts',
  'tests/tablet-setup.spec.ts',
  'tests/operator-longseam.spec.ts',
  'tests/operator-nameplate.spec.ts',
  '--project=chromium',
];

if (isDryRun) {
  console.log(`DEPLOY_SMOKE=${process.env.DEPLOY_SMOKE} ${command} ${args.join(' ')}`);
  if (missing.length > 0) {
    console.log(`# Missing env vars for real run: ${missing.join(', ')}`);
  }
  process.exit(0);
}

const child = spawn(command, args, {
  stdio: 'inherit',
  env: process.env,
});

child.on('exit', (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 1);
});
