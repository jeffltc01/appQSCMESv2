import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useCurrentHelpArticle } from './useCurrentHelpArticle';

vi.mock('react-router-dom', () => ({
  useLocation: vi.fn(),
}));

import { useLocation } from 'react-router-dom';
const mockUseLocation = vi.mocked(useLocation);

describe('useCurrentHelpArticle', () => {
  it('returns operator article matching dataEntryType on /operator route', () => {
    mockUseLocation.mockReturnValue({ pathname: '/operator', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle('Rolls'));
    expect(result.current.slug).toBe('rolls');
  });

  it('returns operator-layout when dataEntryType is unknown on /operator', () => {
    mockUseLocation.mockReturnValue({ pathname: '/operator', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle('UnknownType'));
    expect(result.current.slug).toBe('operator-layout');
  });

  it('matches admin routes by path', () => {
    mockUseLocation.mockReturnValue({ pathname: '/menu/products', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle());
    expect(result.current.slug).toBe('products');
  });

  it('matches the menu route', () => {
    mockUseLocation.mockReturnValue({ pathname: '/menu', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle());
    expect(result.current.slug).toBe('menu');
  });

  it('matches login route', () => {
    mockUseLocation.mockReturnValue({ pathname: '/login', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle());
    expect(result.current.slug).toBe('login');
  });

  it('matches tablet-setup route', () => {
    mockUseLocation.mockReturnValue({ pathname: '/tablet-setup', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle());
    expect(result.current.slug).toBe('tablet-setup');
  });

  it('falls back to overview for unknown routes', () => {
    mockUseLocation.mockReturnValue({ pathname: '/unknown', search: '', hash: '', state: null, key: '' });
    const { result } = renderHook(() => useCurrentHelpArticle());
    expect(result.current.slug).toBe('overview');
  });

  it('maps all operator dataEntryTypes correctly', () => {
    const mappings: Record<string, string> = {
      'Rolls': 'rolls',
      'MatQueue-Material': 'rolls-material',
      'Barcode-LongSeam': 'long-seam',
      'Barcode-LongSeamInsp': 'long-seam-insp',
      'Fitup': 'fitup',
      'MatQueue-Fitup': 'fitup-queue',
      'Barcode-RoundSeam': 'round-seam',
      'Barcode-RoundSeamInsp': 'round-seam-insp',
      'RealTimeXray': 'rt-xray-queue',
      'Spot': 'spot-xray',
      'DataPlate': 'nameplate',
      'Hydro': 'hydro',
    };

    for (const [dataEntryType, expectedSlug] of Object.entries(mappings)) {
      mockUseLocation.mockReturnValue({ pathname: '/operator', search: '', hash: '', state: null, key: '' });
      const { result } = renderHook(() => useCurrentHelpArticle(dataEntryType));
      expect(result.current.slug).toBe(expectedSlug);
    }
  });
});
