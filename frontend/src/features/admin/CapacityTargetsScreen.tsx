import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  Button,
  Input,
  Label,
  Spinner,
  Select,
} from '@fluentui/react-components';
import { GridRegular, GridFilled, SaveRegular, ArrowResetRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import {
  capacityTargetApi,
  adminWorkCenterApi,
  adminPlantGearApi,
  adminProductionLineApi,
  siteApi,
  workCenterApi,
} from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type {
  CapacityTarget,
  Plant,
  PlantWithGear,
  WorkCenterProductionLine,
} from '../../types/domain.ts';
import type { BulkCapacityTargetItem } from '../../types/api.ts';
import styles from './CapacityTargetsScreen.module.css';

interface GearCol {
  id: string;
  level: number;
  name: string;
}

interface WcplRow {
  wcplId: string;
  wcName: string;
  productionLineName: string;
}

type CellMode = 'default' | 'tankSize';

const WC_PRODUCTION_SEQUENCE: Record<string, number> = {
  'Rolls': 1,
  'Long Seam': 2,
  'Long Seam Inspection': 3,
  'RT X-ray Queue': 4,
  'Fitup': 5,
  'Round Seam': 6,
  'Round Seam Inspection': 7,
  'Spot X-ray': 8,
  'Nameplate': 9,
  'Hydro': 10,
  'Rolls Material': 11,
  'Fitup Queue': 12,
};

function wcSortOrder(name: string): number {
  return WC_PRODUCTION_SEQUENCE[name] ?? 999;
}

function cellKey(wcplId: string, gearId: string): string {
  return `${wcplId}_${gearId}`;
}

export function CapacityTargetsScreen() {
  const { user } = useAuth();

  const [plants, setPlants] = useState<Plant[]>([]);
  const [selectedPlantId, setSelectedPlantId] = useState(user?.defaultSiteId ?? '');
  const [productionLines, setProductionLines] = useState<{ id: string; name: string }[]>([]);
  const [selectedLineId, setSelectedLineId] = useState('');

  const [gears, setGears] = useState<GearCol[]>([]);
  const [tankSizes, setTankSizes] = useState<number[]>([]);
  const [wcplRows, setWcplRows] = useState<WcplRow[]>([]);
  const [targets, setTargets] = useState<CapacityTarget[]>([]);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [cellModes, setCellModes] = useState<Record<string, CellMode>>({});
  const [defaultValues, setDefaultValues] = useState<Record<string, string>>({});
  const [tankSizeValues, setTankSizeValues] = useState<Record<string, Record<number, string>>>({});

  const initialSnapshot = useRef<{
    cellModes: Record<string, CellMode>;
    defaultValues: Record<string, string>;
    tankSizeValues: Record<string, Record<number, string>>;
  }>({ cellModes: {}, defaultValues: {}, tankSizeValues: {} });

  // Load plants + gears on mount
  useEffect(() => {
    (async () => {
      try {
        const [sitesData, gearData] = await Promise.all([
          siteApi.getSites(),
          adminPlantGearApi.getAll(),
        ]);
        setPlants(sitesData);
        if (selectedPlantId && gearData.length > 0) {
          const myPlant = gearData.find((p: PlantWithGear) => p.plantId === selectedPlantId);
          setGears(myPlant?.gears.map(g => ({ id: g.id, level: g.level, name: g.name })) ?? []);
        }
      } catch { /* ignore */ }
    })();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // When plant changes: load production lines, gears, tank sizes, targets
  useEffect(() => {
    if (!selectedPlantId) return;
    (async () => {
      setLoading(true);
      setError('');
      try {
        const [allLines, gearData, sizes, tgts] = await Promise.all([
          adminProductionLineApi.getAll(),
          adminPlantGearApi.getAll(),
          capacityTargetApi.getTankSizes(selectedPlantId),
          capacityTargetApi.getAll(selectedPlantId),
        ]);
        const plantLines = allLines.filter(l => l.plantId === selectedPlantId);
        setProductionLines(plantLines.map(l => ({ id: l.id, name: l.name })));
        const myPlant = gearData.find((p: PlantWithGear) => p.plantId === selectedPlantId);
        setGears(myPlant?.gears.map(g => ({ id: g.id, level: g.level, name: g.name })) ?? []);
        setTankSizes(sizes);
        setTargets(tgts);

        if (plantLines.length > 0 && !plantLines.some(l => l.id === selectedLineId)) {
          setSelectedLineId(plantLines[0].id);
        }
      } catch {
        setError('Failed to load data.');
      } finally {
        setLoading(false);
      }
    })();
  }, [selectedPlantId]); // eslint-disable-line react-hooks/exhaustive-deps

  // When production line changes: load WCPLs for that line
  const loadWcplRows = useCallback(async () => {
    if (!selectedLineId) { setWcplRows([]); return; }
    try {
      const wcs = await workCenterApi.getWorkCenters();
      const rows: WcplRow[] = [];
      for (const wc of wcs) {
        try {
          const configs = await adminWorkCenterApi.getProductionLineConfigs(wc.id);
          for (const cfg of configs) {
            if (cfg.productionLineId === selectedLineId) {
              rows.push({ wcplId: cfg.id, wcName: wc.name, productionLineName: cfg.productionLineName });
            }
          }
        } catch { /* skip */ }
      }
      rows.sort((a, b) => wcSortOrder(a.wcName) - wcSortOrder(b.wcName));
      setWcplRows(rows);
    } catch { /* ignore */ }
  }, [selectedLineId]);

  useEffect(() => { loadWcplRows(); }, [loadWcplRows]);

  // Populate grid state from loaded targets whenever rows/gears/targets change
  useEffect(() => {
    if (wcplRows.length === 0 || gears.length === 0) return;

    const modes: Record<string, CellMode> = {};
    const defaults: Record<string, string> = {};
    const tsValues: Record<string, Record<number, string>> = {};

    for (const row of wcplRows) {
      for (const gear of gears) {
        const ck = cellKey(row.wcplId, gear.id);
        const cellTargets = targets.filter(
          t => t.workCenterProductionLineId === row.wcplId && t.plantGearId === gear.id
        );
        const defaultTarget = cellTargets.find(t => t.tankSize === null);
        const sizedTargets = cellTargets.filter(t => t.tankSize !== null);

        if (sizedTargets.length > 0) {
          modes[ck] = 'tankSize';
          defaults[ck] = '';
          const sizeMap: Record<number, string> = {};
          for (const st of sizedTargets) {
            sizeMap[st.tankSize!] = String(st.targetUnitsPerHour);
          }
          tsValues[ck] = sizeMap;
        } else {
          modes[ck] = 'default';
          defaults[ck] = defaultTarget ? String(defaultTarget.targetUnitsPerHour) : '';
          tsValues[ck] = {};
        }
      }
    }

    setCellModes(modes);
    setDefaultValues(defaults);
    setTankSizeValues(tsValues);
    initialSnapshot.current = {
      cellModes: { ...modes },
      defaultValues: { ...defaults },
      tankSizeValues: JSON.parse(JSON.stringify(tsValues)),
    };
  }, [wcplRows, gears, targets]);

  const isDirty = useMemo(() => {
    const snap = initialSnapshot.current;
    if (JSON.stringify(cellModes) !== JSON.stringify(snap.cellModes)) return true;
    if (JSON.stringify(defaultValues) !== JSON.stringify(snap.defaultValues)) return true;
    if (JSON.stringify(tankSizeValues) !== JSON.stringify(snap.tankSizeValues)) return true;
    return false;
  }, [cellModes, defaultValues, tankSizeValues]);

  const handleToggleMode = (ck: string) => {
    setCellModes(prev => {
      const current = prev[ck] ?? 'default';
      const next: CellMode = current === 'default' ? 'tankSize' : 'default';
      return { ...prev, [ck]: next };
    });
    if ((cellModes[ck] ?? 'default') === 'default') {
      setDefaultValues(prev => ({ ...prev, [ck]: '' }));
    } else {
      setTankSizeValues(prev => ({ ...prev, [ck]: {} }));
    }
  };

  const handleDefaultChange = (ck: string, value: string) => {
    const [wcplId, gearId] = ck.split('_');
    const sourceRow = wcplRows.find(r => r.wcplId === wcplId);

    if (sourceRow?.wcName === 'Rolls' && gearId) {
      setDefaultValues(prev => {
        const next = { ...prev, [ck]: value };
        for (const row of wcplRows) {
          if (row.wcplId === wcplId) continue;
          const otherCk = cellKey(row.wcplId, gearId);
          if ((cellModes[otherCk] ?? 'default') === 'default') {
            next[otherCk] = value;
          }
        }
        return next;
      });
    } else {
      setDefaultValues(prev => ({ ...prev, [ck]: value }));
    }
  };

  const handleTankSizeChange = (ck: string, size: number, value: string) => {
    setTankSizeValues(prev => ({
      ...prev,
      [ck]: { ...(prev[ck] ?? {}), [size]: value },
    }));
  };

  const handleReset = () => {
    const snap = initialSnapshot.current;
    setCellModes({ ...snap.cellModes });
    setDefaultValues({ ...snap.defaultValues });
    setTankSizeValues(JSON.parse(JSON.stringify(snap.tankSizeValues)));
  };

  const handleSave = async () => {
    if (!selectedLineId) return;
    setSaving(true);
    setError('');
    try {
      const items: BulkCapacityTargetItem[] = [];

      for (const row of wcplRows) {
        for (const gear of gears) {
          const ck = cellKey(row.wcplId, gear.id);
          const mode = cellModes[ck] ?? 'default';

          if (mode === 'default') {
            const val = defaultValues[ck];
            if (val && parseFloat(val) > 0) {
              items.push({
                workCenterProductionLineId: row.wcplId,
                plantGearId: gear.id,
                tankSize: null,
                targetUnitsPerHour: parseFloat(val),
              });
            }
          } else {
            const sizeMap = tankSizeValues[ck] ?? {};
            for (const size of tankSizes) {
              const val = sizeMap[size];
              if (val && parseFloat(val) > 0) {
                items.push({
                  workCenterProductionLineId: row.wcplId,
                  plantGearId: gear.id,
                  tankSize: size,
                  targetUnitsPerHour: parseFloat(val),
                });
              }
            }
          }
        }
      }

      const updated = await capacityTargetApi.bulkUpsert({
        productionLineId: selectedLineId,
        targets: items,
      });
      setTargets(updated);
    } catch {
      setError('Failed to save capacity targets.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <AdminLayout title="Capacity Targets">
      {loading ? (
        <div style={{ textAlign: 'center', padding: 48 }}>
          <Spinner size="medium" label="Loading..." />
        </div>
      ) : (
        <>
          <div className={styles.filterBar}>
            <div className={styles.filterField}>
              <Label weight="semibold">Plant</Label>
              <Select
                value={selectedPlantId}
                onChange={(_, d) => {
                  setSelectedPlantId(d.value);
                  setSelectedLineId('');
                }}
              >
                {plants.map(p => (
                  <option key={p.id} value={p.id}>{p.name} ({p.code})</option>
                ))}
              </Select>
            </div>
            <div className={styles.filterField}>
              <Label weight="semibold">Production Line</Label>
              <Select
                value={selectedLineId}
                onChange={(_, d) => setSelectedLineId(d.value)}
              >
                {productionLines.length === 0 && <option value="">No lines</option>}
                {productionLines.map(l => (
                  <option key={l.id} value={l.id}>{l.name}</option>
                ))}
              </Select>
            </div>
            <div className={styles.helpText}>
              Select a plant and production line to view the grid. Enter target units/hour in each cell.
              Click the <GridRegular fontSize={13} style={{ verticalAlign: 'middle' }} /> icon in any cell to switch to per-tank-size targets.
              Click <strong>Save Changes</strong> when done.
            </div>
          </div>

          {error && <div style={{ color: '#c92a2a', marginBottom: 12 }}>{error}</div>}

          {wcplRows.length === 0 ? (
            <div className={styles.emptyState}>
              {selectedLineId
                ? 'No work centers are configured for this production line.'
                : 'Select a production line to view capacity targets.'}
            </div>
          ) : (
            <>
              <div className={styles.tableContainer}>
                <table className={styles.gridTable}>
                  <thead>
                    <tr>
                      <th>Work Center</th>
                      {gears.map(g => (
                        <th key={g.id}>Gear {g.level}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {wcplRows.map(row => (
                      <tr key={row.wcplId}>
                        <td>{row.wcName}</td>
                        {gears.map(gear => {
                          const ck = cellKey(row.wcplId, gear.id);
                          const mode = cellModes[ck] ?? 'default';
                          const isExpanded = mode === 'tankSize';

                          return (
                            <td key={gear.id}>
                              {isExpanded ? (
                                <div className={styles.cellExpanded}>
                                  <span
                                    className={`${styles.toggleIcon} ${styles.toggleIconActive}`}
                                    onClick={() => handleToggleMode(ck)}
                                    title="Switch to default (single value)"
                                  >
                                    <GridFilled fontSize={14} />
                                  </span>
                                  <div className={styles.tankSizeList}>
                                    {tankSizes.map(size => (
                                      <div key={size} className={styles.tankSizeRow}>
                                        <span className={styles.tankSizeLabel}>{size} gal:</span>
                                        <Input
                                          className={styles.tankSizeInput}
                                          type="number"
                                          size="small"
                                          value={(tankSizeValues[ck] ?? {})[size] ?? ''}
                                          onChange={(_, d) => handleTankSizeChange(ck, size, d.value)}
                                        />
                                      </div>
                                    ))}
                                  </div>
                                </div>
                              ) : (
                                <div className={styles.cell}>
                                  <span
                                    className={styles.toggleIcon}
                                    onClick={() => handleToggleMode(ck)}
                                    title="Switch to per-tank-size targets"
                                  >
                                    <GridRegular fontSize={14} />
                                  </span>
                                  <Input
                                    className={styles.gearInput}
                                    type="number"
                                    size="small"
                                    value={defaultValues[ck] ?? ''}
                                    onChange={(_, d) => handleDefaultChange(ck, d.value)}
                                  />
                                </div>
                              )}
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<SaveRegular />}
                  onClick={handleSave}
                  disabled={saving || !isDirty}
                  style={{ borderRadius: 0 }}
                >
                  {saving ? <Spinner size="tiny" /> : 'Save Changes'}
                </Button>
                <Button
                  appearance="subtle"
                  icon={<ArrowResetRegular />}
                  onClick={handleReset}
                  disabled={saving || !isDirty}
                  style={{ borderRadius: 0 }}
                >
                  Reset
                </Button>
                {isDirty && <span className={styles.dirtyBadge}>Unsaved changes</span>}
              </div>
            </>
          )}
        </>
      )}
    </AdminLayout>
  );
}
