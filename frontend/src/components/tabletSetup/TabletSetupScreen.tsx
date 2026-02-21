import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Label,
  Dropdown,
  Option,
  Spinner,
  type OptionOnSelectData,
} from '@fluentui/react-components';
import { useAuth } from '../../auth/AuthContext.tsx';
import { workCenterApi, productionLineApi, assetApi, tabletSetupApi, adminWorkCenterApi } from '../../api/endpoints.ts';
import { setTabletCache } from '../../hooks/useLocalStorage.ts';
import type { WorkCenter, ProductionLine, Asset } from '../../types/domain.ts';
import styles from './TabletSetupScreen.module.css';

export function TabletSetupScreen() {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [productionLines, setProductionLines] = useState<ProductionLine[]>([]);
  const [assets, setAssets] = useState<Asset[]>([]);

  const [selectedWcId, setSelectedWcId] = useState('');
  const [selectedPlId, setSelectedPlId] = useState('');
  const [selectedAssetId, setSelectedAssetId] = useState('');

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const needsAsset = assets.length > 0;

  useEffect(() => {
    if (!user) return;
    if (user.roleTier > 5) {
      setError('This tablet has not been set up. Please contact a Team Lead or Supervisor to run Tablet Setup.');
      setLoading(false);
      return;
    }
    loadDropdowns();
  }, [user]);

  const loadDropdowns = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    try {
      const [wcs, pls] = await Promise.all([
        workCenterApi.getWorkCenters(),
        productionLineApi.getProductionLines(user.plantCode),
      ]);
      setWorkCenters(wcs);
      setProductionLines(pls);
      if (pls.length === 1) {
        setSelectedPlId(pls[0].id);
      }
    } catch {
      setError('Unable to load configuration data. Please check your connection and try again.');
    } finally {
      setLoading(false);
    }
  }, [user]);

  const loadAssets = useCallback(async (wcId: string, plId: string) => {
    if (!wcId || !plId) {
      setAssets([]);
      setSelectedAssetId('');
      return;
    }
    try {
      const assetList = await assetApi.getAssets(wcId, plId);
      setAssets(assetList);
      setSelectedAssetId(assetList.length === 1 ? assetList[0].id : '');
    } catch {
      setAssets([]);
      setSelectedAssetId('');
    }
  }, []);

  const handleWcChange = useCallback(
    (_: unknown, data: OptionOnSelectData) => {
      const wcId = data.optionValue ?? '';
      setSelectedWcId(wcId);
      setSelectedAssetId('');
      loadAssets(wcId, selectedPlId);
    },
    [selectedPlId, loadAssets],
  );

  const handlePlChange = useCallback(
    (_: unknown, data: OptionOnSelectData) => {
      const plId = data.optionValue ?? '';
      setSelectedPlId(plId);
      setSelectedAssetId('');
      loadAssets(selectedWcId, plId);
    },
    [selectedWcId, loadAssets],
  );

  const handleSave = useCallback(async () => {
    setSaving(true);
    setError('');
    try {
      await tabletSetupApi.save({
        workCenterId: selectedWcId,
        productionLineId: selectedPlId,
        assetId: selectedAssetId || null,
      });

      const wc = workCenters.find((w) => w.id === selectedWcId);
      const pl = productionLines.find((p) => p.id === selectedPlId);
      const asset = assets.find((a) => a.id === selectedAssetId);

      let displayName = wc?.name ?? '';
      let numberOfWelders = wc?.numberOfWelders ?? 0;

      try {
        const plConfig = await adminWorkCenterApi.getProductionLineConfig(selectedWcId, selectedPlId);
        displayName = plConfig.displayName;
        numberOfWelders = plConfig.numberOfWelders;
      } catch {
        // No per-line config exists; fall back to WorkCenter defaults
      }

      setTabletCache({
        cachedWorkCenterId: selectedWcId,
        cachedWorkCenterName: wc?.name ?? '',
        cachedWorkCenterDisplayName: displayName,
        cachedDataEntryType: wc?.dataEntryType ?? '',
        cachedProductionLineId: selectedPlId,
        cachedProductionLineName: pl?.name ?? '',
        cachedAssetId: selectedAssetId,
        cachedAssetName: asset?.name ?? '',
        cachedMaterialQueueForWCId: wc?.materialQueueForWCId,
        cachedNumberOfWelders: numberOfWelders,
      });

      navigate('/operator');
    } catch {
      setError('Failed to save tablet configuration. Please try again.');
    } finally {
      setSaving(false);
    }
  }, [selectedWcId, selectedPlId, selectedAssetId, workCenters, productionLines, assets, navigate]);

  const canSave = Boolean(selectedWcId && selectedPlId && (!needsAsset || selectedAssetId));

  if (user && user.roleTier > 5) {
    return (
      <div className={styles.container}>
        <div className={styles.form}>
          <h1 className={styles.title}>Tablet Setup</h1>
          <p className={styles.message}>{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.form}>
        <h1 className={styles.title}>Tablet Setup</h1>
        <p className={styles.instructions}>
          This is a one-time task to setup this Tablet for which Work Center it's associated with.
          Please fill out the fields below and then click Save to continue.
        </p>

        {loading ? (
          <Spinner size="medium" label="Loading..." />
        ) : (
          <>
            <div className={styles.field}>
              <Label className={styles.label}>*Associated Work Center</Label>
              <Dropdown
                value={workCenters.find((w) => w.id === selectedWcId)?.name ?? ''}
                selectedOptions={[selectedWcId]}
                onOptionSelect={handleWcChange}
                className={styles.dropdown}
                size="large"
                placeholder="Select a work center..."
              >
                {workCenters.map((wc) => (
                  <Option key={wc.id} value={wc.id}>
                    {wc.name}
                  </Option>
                ))}
              </Dropdown>
            </div>

            <div className={styles.field}>
              <Label className={styles.label}>*Production Line</Label>
              <Dropdown
                value={productionLines.find((p) => p.id === selectedPlId)?.name ?? ''}
                selectedOptions={[selectedPlId]}
                onOptionSelect={handlePlChange}
                className={styles.dropdown}
                size="large"
                disabled={productionLines.length <= 1}
              >
                {productionLines.map((pl) => (
                  <Option key={pl.id} value={pl.id}>
                    {pl.name}
                  </Option>
                ))}
              </Dropdown>
            </div>

            {needsAsset && (
              <div className={styles.field}>
                <Label className={styles.label}>*Associated Asset</Label>
                <Dropdown
                  value={assets.find((a) => a.id === selectedAssetId)?.name ?? ''}
                  selectedOptions={[selectedAssetId]}
                  onOptionSelect={(_, data) => {
                    if (data.optionValue) setSelectedAssetId(data.optionValue);
                  }}
                  className={styles.dropdown}
                  size="large"
                  disabled={!selectedWcId}
                  placeholder="Select an asset..."
                >
                  {assets.map((a) => (
                    <Option key={a.id} value={a.id}>
                      {a.name}
                    </Option>
                  ))}
                </Dropdown>
              </div>
            )}

            {error && <div className={styles.error}>{error}</div>}

            <Button
              appearance="primary"
              onClick={handleSave}
              disabled={!canSave || saving}
              className={styles.saveButton}
              size="large"
            >
              {saving ? <Spinner size="tiny" /> : 'Save'}
            </Button>
          </>
        )}
      </div>
    </div>
  );
}
