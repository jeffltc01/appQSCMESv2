import { describe, expect, it } from 'vitest';
import { getEnvironmentWatermarkLabel, resolveRuntimeEnvironment } from './runtimeEnvironment.ts';

describe('resolveRuntimeEnvironment', () => {
  it('prefers explicit app environment over mode', () => {
    expect(resolveRuntimeEnvironment('test', 'production', 'https://qscmes-prod-app.azurewebsites.net')).toBe('test');
  });

  it('maps production values to prod', () => {
    expect(resolveRuntimeEnvironment('prod')).toBe('prod');
    expect(resolveRuntimeEnvironment('production')).toBe('prod');
    expect(resolveRuntimeEnvironment(undefined, 'production', 'https://qscmes-prod-app.azurewebsites.net')).toBe('prod');
  });

  it('maps test-like values to test', () => {
    expect(resolveRuntimeEnvironment('test')).toBe('test');
    expect(resolveRuntimeEnvironment('qa')).toBe('test');
    expect(resolveRuntimeEnvironment('staging')).toBe('test');
  });

  it('maps local development values to dev', () => {
    expect(resolveRuntimeEnvironment('dev')).toBe('dev');
    expect(resolveRuntimeEnvironment('development')).toBe('dev');
    expect(resolveRuntimeEnvironment(undefined, 'development')).toBe('dev');
  });

  it('uses API URL host to infer environment', () => {
    expect(resolveRuntimeEnvironment(undefined, 'production', 'https://qscmes-test-app.azurewebsites.net')).toBe('test');
    expect(resolveRuntimeEnvironment(undefined, 'production', 'https://qscmes-dev-app.azurewebsites.net')).toBe('dev');
  });

  it('uses browser host name to infer environment when app env and api url are absent', () => {
    expect(resolveRuntimeEnvironment(undefined, 'production', undefined, 'test.mes.example.com')).toBe('test');
  });

  it('defaults to dev when input is unknown', () => {
    expect(resolveRuntimeEnvironment('unknown', 'production', undefined, 'unknown-host')).toBe('dev');
  });
});

describe('getEnvironmentWatermarkLabel', () => {
  it('returns null for production', () => {
    expect(getEnvironmentWatermarkLabel('prod')).toBeNull();
  });

  it('returns TEST for test environment', () => {
    expect(getEnvironmentWatermarkLabel('test')).toBe('TEST');
  });

  it('returns DEV for development environment', () => {
    expect(getEnvironmentWatermarkLabel('dev')).toBe('DEV');
  });
});
