import { useState, useCallback } from 'react';

export function useLocalStorage<T>(key: string, initialValue: T): [T, (value: T) => void] {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch {
      return initialValue;
    }
  });

  const setValue = useCallback(
    (value: T) => {
      setStoredValue(value);
      try {
        window.localStorage.setItem(key, JSON.stringify(value));
      } catch {
        // localStorage unavailable
      }
    },
    [key],
  );

  return [storedValue, setValue];
}

export interface TabletCache {
  cachedWorkCenterId: string;
  cachedWorkCenterName: string;
  cachedWorkCenterDisplayName: string;
  cachedDataEntryType: string;
  cachedProductionLineId: string;
  cachedProductionLineName: string;
  cachedAssetId: string;
  cachedAssetName: string;
  cachedMaterialQueueForWCId?: string;
  cachedNumberOfWelders: number;
}

export function getTabletCache(): TabletCache | null {
  try {
    const wcId = localStorage.getItem('cachedWorkCenterId');
    if (!wcId) return null;
    return {
      cachedWorkCenterId: wcId,
      cachedWorkCenterName: localStorage.getItem('cachedWorkCenterName') ?? '',
      cachedWorkCenterDisplayName: localStorage.getItem('cachedWorkCenterDisplayName') ?? localStorage.getItem('cachedWorkCenterName') ?? '',
      cachedDataEntryType: localStorage.getItem('cachedDataEntryType') ?? '',
      cachedProductionLineId: localStorage.getItem('cachedProductionLineId') ?? '',
      cachedProductionLineName: localStorage.getItem('cachedProductionLineName') ?? '',
      cachedAssetId: localStorage.getItem('cachedAssetId') ?? '',
      cachedAssetName: localStorage.getItem('cachedAssetName') ?? '',
      cachedMaterialQueueForWCId: localStorage.getItem('cachedMaterialQueueForWCId') ?? undefined,
      cachedNumberOfWelders: parseInt(localStorage.getItem('cachedNumberOfWelders') ?? '0', 10),
    };
  } catch {
    return null;
  }
}

export function setTabletCache(cache: TabletCache) {
  localStorage.setItem('cachedWorkCenterId', cache.cachedWorkCenterId);
  localStorage.setItem('cachedWorkCenterName', cache.cachedWorkCenterName);
  localStorage.setItem('cachedWorkCenterDisplayName', cache.cachedWorkCenterDisplayName);
  localStorage.setItem('cachedDataEntryType', cache.cachedDataEntryType);
  localStorage.setItem('cachedProductionLineId', cache.cachedProductionLineId);
  localStorage.setItem('cachedProductionLineName', cache.cachedProductionLineName);
  localStorage.setItem('cachedAssetId', cache.cachedAssetId);
  localStorage.setItem('cachedAssetName', cache.cachedAssetName);
  if (cache.cachedMaterialQueueForWCId) {
    localStorage.setItem('cachedMaterialQueueForWCId', cache.cachedMaterialQueueForWCId);
  } else {
    localStorage.removeItem('cachedMaterialQueueForWCId');
  }
  localStorage.setItem('cachedNumberOfWelders', String(cache.cachedNumberOfWelders));
}
