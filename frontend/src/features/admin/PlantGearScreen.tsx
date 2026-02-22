import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { adminPlantGearApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { PlantWithGear } from '../../types/domain.ts';
import styles from './CardList.module.css';
import gearStyles from './PlantGear.module.css';

export function PlantGearScreen() {
  const { user } = useAuth();
  const isReadOnly = (user?.roleTier ?? 99) > 2;
  const [plants, setPlants] = useState<PlantWithGear[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<string | null>(null);
  const [limbleEdits, setLimbleEdits] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setLoading(true);
    try { setPlants(await adminPlantGearApi.getAll()); }
    catch { /* ignore */ }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSetGear = async (plantId: string, gearId: string) => {
    setSaving(plantId);
    try {
      await adminPlantGearApi.setGear(plantId, { plantGearId: gearId });
      setPlants(prev => prev.map(p => {
        if (p.plantId !== plantId) return p;
        const gear = p.gears.find(g => g.id === gearId);
        return { ...p, currentPlantGearId: gearId, currentGearLevel: gear?.level };
      }));
    } catch { alert('Failed to set gear.'); }
    finally { setSaving(null); }
  };

  const handleSaveLimble = async (plantId: string) => {
    const value = limbleEdits[plantId];
    if (value === undefined) return;
    setSaving(plantId);
    try {
      await adminPlantGearApi.setLimbleLocationId(plantId, { limbleLocationId: value || undefined });
      setPlants(prev => prev.map(p => p.plantId === plantId ? { ...p, limbleLocationId: value || undefined } : p));
      setLimbleEdits(prev => { const next = { ...prev }; delete next[plantId]; return next; });
    } catch { alert('Failed to save Limble Location ID.'); }
    finally { setSaving(null); }
  };

  return (
    <AdminLayout title="Plant Gear">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {plants.map(plant => (
            <div key={plant.plantId} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{plant.plantName} ({plant.plantCode})</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Current Gear</span>
                <span className={styles.cardFieldValue}>
                  {plant.currentGearLevel != null ? `Gear ${plant.currentGearLevel}` : 'Not set'}
                </span>
              </div>
              {!isReadOnly && (
                <div className={gearStyles.gearRow}>
                  {plant.gears.map(gear => (
                    <Button
                      key={gear.id}
                      appearance={gear.id === plant.currentPlantGearId ? 'primary' : 'outline'}
                      size="small"
                      onClick={() => handleSetGear(plant.plantId, gear.id)}
                      disabled={saving === plant.plantId}
                      className={gearStyles.gearBtn}
                    >
                      {gear.level}
                    </Button>
                  ))}
                </div>
              )}
              <div className={styles.cardField} style={{ marginTop: 8 }}>
                <Label>Limble Location ID</Label>
                {isReadOnly ? (
                  <span className={styles.cardFieldValue}>{plant.limbleLocationId ?? 'Not set'}</span>
                ) : (
                  <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
                    <Input
                      size="small"
                      value={limbleEdits[plant.plantId] ?? plant.limbleLocationId ?? ''}
                      onChange={(_, d) => setLimbleEdits(prev => ({ ...prev, [plant.plantId]: d.value }))}
                      placeholder="e.g. 12345"
                    />
                    {limbleEdits[plant.plantId] !== undefined && (
                      <Button
                        size="small"
                        appearance="primary"
                        onClick={() => handleSaveLimble(plant.plantId)}
                        disabled={saving === plant.plantId}
                      >
                        Save
                      </Button>
                    )}
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </AdminLayout>
  );
}
