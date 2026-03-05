export type RuntimeEnvironment = 'dev' | 'test' | 'prod';

function normalizeValue(value?: string): string {
  return value?.trim().toLowerCase() ?? '';
}

function parseEnvironment(value: string): RuntimeEnvironment | null {
  if (!value) return null;

  if (value === 'prod' || value === 'production') return 'prod';
  if (value === 'test' || value === 'testing') return 'test';
  if (value === 'dev' || value === 'development' || value === 'local') return 'dev';

  if (value.includes('prod')) return 'prod';
  if (value.includes('test') || value.includes('qa') || value.includes('stage') || value.includes('stag')) return 'test';
  if (value.includes('dev') || value.includes('local')) return 'dev';

  return null;
}

export function resolveRuntimeEnvironment(rawAppEnv?: string, rawMode?: string): RuntimeEnvironment {
  const appEnv = parseEnvironment(normalizeValue(rawAppEnv));
  if (appEnv) return appEnv;

  const modeEnv = parseEnvironment(normalizeValue(rawMode));
  if (modeEnv) return modeEnv;

  return 'dev';
}

export function getRuntimeEnvironment(): RuntimeEnvironment {
  return resolveRuntimeEnvironment(import.meta.env.VITE_APP_ENV, import.meta.env.MODE);
}

export function getEnvironmentWatermarkLabel(env: RuntimeEnvironment): 'DEV' | 'TEST' | null {
  if (env === 'prod') return null;
  return env === 'test' ? 'TEST' : 'DEV';
}
