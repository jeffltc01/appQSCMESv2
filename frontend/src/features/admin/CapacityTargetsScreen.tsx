import { useState, useEffect, useCallback } from 'react';
import {
  Button,
  Input,
  Label,
  Spinner,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { AddRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import {
  capacityTargetApi,
  adminWorkCenterApi,
  adminPlantGearApi,
  workCenterApi,
} from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type {
  CapacityTarget,
  WorkCenterProductionLine,
  PlantWithGear,
} from '../../types/domain.ts';
import styles from './CapacityTargetsScreen.module.css';

interface GearOption {
  id: string;
  level: number;
  name: string;
}

export function CapacityTargetsScreen() {
  const { user } = useAuth();
  const plantId = user?.defaultSiteId ?? '';
  const [targets, setTargets] = useState<CapacityTarget[]>([]);
  const [wcpls, setWcpls] = useState<(WorkCenterProductionLine & { wcName: string })[]>([]);
  const [gears, setGears] = useState<GearOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);

  const [selectedWcplId, setSelectedWcplId] = useState('');
  const [selectedWcplLabel, setSelectedWcplLabel] = useState('');
  const [selectedGearId, setSelectedGearId] = useState('');
  const [selectedGearLabel, setSelectedGearLabel] = useState('');
  const [tankSize, setTankSize] = useState('');
  const [targetUph, setTargetUph] = useState('');

  const load = useCallback(async () => {
    if (!plantId) return;
    setLoading(true);
    try {
      const [tgts, wcs, plantGearData] = await Promise.all([
        capacityTargetApi.getAll(plantId),
        workCenterApi.getWorkCenters(),
        adminPlantGearApi.getAll(),
      ]);
      setTargets(tgts);

      const myPlant = plantGearData.find((p: PlantWithGear) => p.plantId === plantId);
      setGears(myPlant?.gears.map(g => ({ id: g.id, level: g.level, name: g.name })) ?? []);

      const allWcpls: (WorkCenterProductionLine & { wcName: string })[] = [];
      for (const wc of wcs) {
        try {
          const configs = await adminWorkCenterApi.getProductionLineConfigs(wc.id);
          for (const cfg of configs) {
            allWcpls.push({ ...cfg, wcName: wc.name });
          }
        } catch { /* skip */ }
      }
      setWcpls(allWcpls);
    } catch { /* ignore */ }
    finally { setLoading(false); }
  }, [plantId]);

  useEffect(() => { load(); }, [load]);

  const handleSave = async () => {
    if (!selectedWcplId || !selectedGearId || !targetUph) return;
    setSaving(true);
    try {
      await capacityTargetApi.create({
        workCenterProductionLineId: selectedWcplId,
        tankSize: tankSize ? parseInt(tankSize, 10) : null,
        plantGearId: selectedGearId,
        targetUnitsPerHour: parseFloat(targetUph),
      });
      setShowForm(false);
      resetForm();
      await load();
    } catch { alert('Failed to create capacity target. It may already exist for this combination.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this capacity target?')) return;
    try { await capacityTargetApi.delete(id); await load(); }
    catch { alert('Failed to delete.'); }
  };

  const resetForm = () => {
    setSelectedWcplId('');
    setSelectedWcplLabel('');
    setSelectedGearId('');
    setSelectedGearLabel('');
    setTankSize('');
    setTargetUph('');
  };

  const groupedTargets = targets.reduce<Record<string, CapacityTarget[]>>((acc, t) => {
    const key = `${t.workCenterName} - ${t.productionLineName}`;
    (acc[key] ??= []).push(t);
    return acc;
  }, {});

  return (
    <AdminLayout title="Capacity Targets">
      {loading ? (
        <div style={{ textAlign: 'center', padding: 48 }}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
          <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
            <Button
              appearance="primary"
              icon={<AddRegular />}
              onClick={() => { setShowForm(true); resetForm(); }}
              style={{ borderRadius: 0 }}
            >
              New Target
            </Button>
          </div>

          {showForm && (
            <div className={styles.formCard}>
              <div className={styles.formHeader}>New Capacity Target</div>
              <div className={styles.formGrid}>
                <div className={styles.formField}>
                  <Label weight="semibold">Work Center / Line</Label>
                  <Dropdown
                    placeholder="Select..."
                    value={selectedWcplLabel}
                    selectedOptions={selectedWcplId ? [selectedWcplId] : []}
                    onOptionSelect={(_, d) => {
                      setSelectedWcplId(d.optionValue ?? '');
                      setSelectedWcplLabel(d.optionText ?? '');
                    }}
                  >
                    {wcpls.map(w => (
                      <Option key={w.id} value={w.id} text={`${w.wcName} - ${w.productionLineName}`}>
                        {w.wcName} - {w.productionLineName}
                      </Option>
                    ))}
                  </Dropdown>
                </div>
                <div className={styles.formField}>
                  <Label weight="semibold">Gear</Label>
                  <Dropdown
                    placeholder="Select gear..."
                    value={selectedGearLabel}
                    selectedOptions={selectedGearId ? [selectedGearId] : []}
                    onOptionSelect={(_, d) => {
                      setSelectedGearId(d.optionValue ?? '');
                      setSelectedGearLabel(d.optionText ?? '');
                    }}
                  >
                    {gears.map(g => (
                      <Option key={g.id} value={g.id} text={`Gear ${g.level}`}>
                        Gear {g.level}
                      </Option>
                    ))}
                  </Dropdown>
                </div>
                <div className={styles.formField}>
                  <Label weight="semibold">Tank Size (blank = default)</Label>
                  <Input
                    type="number"
                    value={tankSize}
                    onChange={(_, d) => setTankSize(d.value)}
                    placeholder="e.g. 120"
                  />
                </div>
                <div className={styles.formField}>
                  <Label weight="semibold">Target Units/Hour</Label>
                  <Input
                    type="number"
                    value={targetUph}
                    onChange={(_, d) => setTargetUph(d.value)}
                    placeholder="e.g. 15"
                  />
                </div>
              </div>
              <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
                <Button appearance="primary" onClick={handleSave} disabled={saving} style={{ borderRadius: 0 }}>
                  {saving ? <Spinner size="tiny" /> : 'Save'}
                </Button>
                <Button appearance="outline" onClick={() => setShowForm(false)} style={{ borderRadius: 0 }}>
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {targets.length === 0 && !showForm ? (
            <div style={{ textAlign: 'center', padding: 48, color: '#868e96' }}>
              No capacity targets configured. OEE Performance cannot be calculated without targets.
            </div>
          ) : (
            Object.entries(groupedTargets).map(([group, items]) => (
              <div key={group} className={styles.groupSection}>
                <div className={styles.groupHeader}>{group}</div>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>Gear</th>
                      <th>Tank Size</th>
                      <th>Target UPH</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map(t => (
                      <tr key={t.id}>
                        <td>Gear {t.gearLevel}</td>
                        <td>{t.tankSize !== null ? t.tankSize : <span className={styles.defaultLabel}>Default (all sizes)</span>}</td>
                        <td className={styles.uphCell}>{t.targetUnitsPerHour}</td>
                        <td>
                          <Button
                            size="small"
                            icon={<DeleteRegular />}
                            appearance="subtle"
                            onClick={() => handleDelete(t.id)}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ))
          )}
        </>
      )}
    </AdminLayout>
  );
}
