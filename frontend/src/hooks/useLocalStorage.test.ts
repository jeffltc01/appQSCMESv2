import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useLocalStorage, getTabletCache, setTabletCache, type TabletCache } from './useLocalStorage';

describe('useLocalStorage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('reads existing value from localStorage on init', () => {
    localStorage.setItem('testKey', JSON.stringify('saved'));
    const { result } = renderHook(() => useLocalStorage('testKey', 'default'));
    expect(result.current[0]).toBe('saved');
  });

  it('returns initialValue when key is missing', () => {
    const { result } = renderHook(() => useLocalStorage('missing', 42));
    expect(result.current[0]).toBe(42);
  });

  it('returns initialValue when JSON parse fails', () => {
    localStorage.setItem('bad', '{not valid json');
    const { result } = renderHook(() => useLocalStorage('bad', 'fallback'));
    expect(result.current[0]).toBe('fallback');
  });

  it('setValue updates state and writes to localStorage', () => {
    const { result } = renderHook(() => useLocalStorage('key', 'initial'));

    act(() => {
      result.current[1]('updated');
    });

    expect(result.current[0]).toBe('updated');
    expect(localStorage.getItem('key')).toBe(JSON.stringify('updated'));
  });
});

describe('getTabletCache', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('returns null when cachedWorkCenterId is absent', () => {
    expect(getTabletCache()).toBeNull();
  });

  it('returns full TabletCache when populated', () => {
    const data: TabletCache = {
      cachedWorkCenterId: 'wc1',
      cachedWorkCenterName: 'Center 1',
      cachedWorkCenterDisplayName: 'Center One',
      cachedDataEntryType: 'manual',
      cachedProductionLineId: 'pl1',
      cachedProductionLineName: 'Line 1',
      cachedAssetId: 'a1',
      cachedAssetName: 'Asset 1',
      cachedMaterialQueueForWCId: 'mq1',
      cachedNumberOfWelders: 3,
    };

    localStorage.setItem('cachedWorkCenterId', data.cachedWorkCenterId);
    localStorage.setItem('cachedWorkCenterName', data.cachedWorkCenterName);
    localStorage.setItem('cachedWorkCenterDisplayName', data.cachedWorkCenterDisplayName);
    localStorage.setItem('cachedDataEntryType', data.cachedDataEntryType);
    localStorage.setItem('cachedProductionLineId', data.cachedProductionLineId);
    localStorage.setItem('cachedProductionLineName', data.cachedProductionLineName);
    localStorage.setItem('cachedAssetId', data.cachedAssetId);
    localStorage.setItem('cachedAssetName', data.cachedAssetName);
    localStorage.setItem('cachedMaterialQueueForWCId', data.cachedMaterialQueueForWCId!);
    localStorage.setItem('cachedNumberOfWelders', String(data.cachedNumberOfWelders));

    expect(getTabletCache()).toEqual(data);
  });

  it('handles partial data with defaults', () => {
    localStorage.setItem('cachedWorkCenterId', 'wc1');

    const result = getTabletCache()!;
    expect(result.cachedWorkCenterId).toBe('wc1');
    expect(result.cachedWorkCenterName).toBe('');
    expect(result.cachedDataEntryType).toBe('');
    expect(result.cachedProductionLineId).toBe('');
    expect(result.cachedProductionLineName).toBe('');
    expect(result.cachedAssetId).toBe('');
    expect(result.cachedAssetName).toBe('');
    expect(result.cachedMaterialQueueForWCId).toBeUndefined();
    expect(result.cachedNumberOfWelders).toBe(0);
  });
});

describe('setTabletCache', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('writes all keys to localStorage', () => {
    const cache: TabletCache = {
      cachedWorkCenterId: 'wc2',
      cachedWorkCenterName: 'Center 2',
      cachedWorkCenterDisplayName: 'Center Two',
      cachedDataEntryType: 'auto',
      cachedProductionLineId: 'pl2',
      cachedProductionLineName: 'Line 2',
      cachedAssetId: 'a2',
      cachedAssetName: 'Asset 2',
      cachedMaterialQueueForWCId: 'mq2',
      cachedNumberOfWelders: 5,
    };

    setTabletCache(cache);

    expect(localStorage.getItem('cachedWorkCenterId')).toBe('wc2');
    expect(localStorage.getItem('cachedWorkCenterName')).toBe('Center 2');
    expect(localStorage.getItem('cachedWorkCenterDisplayName')).toBe('Center Two');
    expect(localStorage.getItem('cachedDataEntryType')).toBe('auto');
    expect(localStorage.getItem('cachedProductionLineId')).toBe('pl2');
    expect(localStorage.getItem('cachedProductionLineName')).toBe('Line 2');
    expect(localStorage.getItem('cachedAssetId')).toBe('a2');
    expect(localStorage.getItem('cachedAssetName')).toBe('Asset 2');
    expect(localStorage.getItem('cachedMaterialQueueForWCId')).toBe('mq2');
    expect(localStorage.getItem('cachedNumberOfWelders')).toBe('5');
  });

  it('removes cachedMaterialQueueForWCId when undefined', () => {
    localStorage.setItem('cachedMaterialQueueForWCId', 'old-value');

    setTabletCache({
      cachedWorkCenterId: 'wc3',
      cachedWorkCenterName: 'Center 3',
      cachedWorkCenterDisplayName: 'Center Three',
      cachedDataEntryType: 'manual',
      cachedProductionLineId: 'pl3',
      cachedProductionLineName: 'Line 3',
      cachedAssetId: 'a3',
      cachedAssetName: 'Asset 3',
      cachedNumberOfWelders: 1,
    });

    expect(localStorage.getItem('cachedMaterialQueueForWCId')).toBeNull();
  });
});
