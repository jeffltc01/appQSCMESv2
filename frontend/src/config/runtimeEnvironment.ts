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

function parseEnvironmentFromUrl(rawUrl?: string): RuntimeEnvironment | null {
  const value = rawUrl?.trim();
  if (!value) return null;

  try {
    const normalized = value.includes('://') ? value : `https://${value}`;
    const host = new URL(normalized).hostname;
    return parseEnvironment(normalizeValue(host));
  } catch {
    return parseEnvironment(normalizeValue(value));
  }
}

export function resolveRuntimeEnvironment(
  rawAppEnv?: string,
  rawMode?: string,
  rawApiUrl?: string,
  rawHostName?: string,
): RuntimeEnvironment {
  const appEnv = parseEnvironment(normalizeValue(rawAppEnv));
  if (appEnv) return appEnv;

  const apiEnv = parseEnvironmentFromUrl(rawApiUrl);
  if (apiEnv) return apiEnv;

  const hostEnv = parseEnvironmentFromUrl(rawHostName);
  if (hostEnv) return hostEnv;

  // Production build mode is used for all hosted environments; do not
  // infer PROD from mode alone or non-prod watermarks will disappear.
  const modeEnv = parseEnvironment(normalizeValue(rawMode));
  if (modeEnv && modeEnv !== 'prod') return modeEnv;

  return 'dev';
}

export function getRuntimeEnvironment(): RuntimeEnvironment {
  const hostName = typeof window !== 'undefined' ? window.location.hostname : undefined;
  return resolveRuntimeEnvironment(
    import.meta.env.VITE_APP_ENV,
    import.meta.env.MODE,
    import.meta.env.VITE_API_URL,
    hostName,
  );
}

export function getEnvironmentWatermarkLabel(env: RuntimeEnvironment): 'DEV' | 'TEST' | null {
  if (env === 'prod') return null;
  return env === 'test' ? 'TEST' : 'DEV';
}
