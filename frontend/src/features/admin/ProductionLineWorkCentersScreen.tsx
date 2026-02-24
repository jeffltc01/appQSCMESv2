import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner, Switch, Checkbox } from '@fluentui/react-components';
import { EditRegular, AddRegular, DeleteRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminWorkCenterApi, productionLineApi, downtimeConfigApi, downtimeReasonCategoryApi } from '../../api/endpoints.ts';
import type { AdminWorkCenterGroup, WorkCenterProductionLine, ProductionLineAdmin, DowntimeReasonCategory } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function ProductionLineWorkCentersScreen() {
  const { user } = useAuth();
  const isQualityManagerOrAbove = (user?.roleTier ?? 99) <= 3;

  const [groups, setGroups] = useState<AdminWorkCenterGroup[]>([]);
  const [plConfigs, setPlConfigs] = useState<Record<string, WorkCenterProductionLine[]>>({});
  const [allProductionLines, setAllProductionLines] = useState<ProductionLineAdmin[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [plModalOpen, setPlModalOpen] = useState(false);
  const [plEditing, setPlEditing] = useState<WorkCenterProductionLine | null>(null);
  const [plWcId, setPlWcId] = useState('');
  const [plProductionLineId, setPlProductionLineId] = useState('');
  const [plDisplayName, setPlDisplayName] = useState('');
  const [plNumberOfWelders, setPlNumberOfWelders] = useState('0');
  const [plSaving, setPlSaving] = useState(false);
  const [plError, setPlError] = useState('');
  const [plDowntimeEnabled, setPlDowntimeEnabled] = useState(false);
  const [plDowntimeThreshold, setPlDowntimeThreshold] = useState('5');
  const [plEnableWorkCenterChecklist, setPlEnableWorkCenterChecklist] = useState(false);
  const [plEnableSafetyChecklist, setPlEnableSafetyChecklist] = useState(false);
  const [plReasonCategories, setPlReasonCategories] = useState<DowntimeReasonCategory[]>([]);
  const [plSelectedReasonIds, setPlSelectedReasonIds] = useState<string[]>([]);
  const [plReasonsLoading, setPlReasonsLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [groupData, plData] = await Promise.all([
        adminWorkCenterApi.getGrouped(),
        productionLineApi.getAll(),
      ]);
      setGroups(groupData);
      setAllProductionLines(plData);

      const configMap: Record<string, WorkCenterProductionLine[]> = {};
      await Promise.all(
        groupData.map(async (g) => {
          try {
            configMap[g.groupId] = await adminWorkCenterApi.getProductionLineConfigs(g.groupId);
          } catch {
            configMap[g.groupId] = [];
          }
        }),
      );
      setPlConfigs(configMap);
    } catch {
      setError('Failed to load production line work centers.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const resolvePlantId = useCallback((productionLineId: string) =>
    allProductionLines.find((p) => p.id === productionLineId)?.plantId, [allProductionLines]);

  const loadReasons = useCallback(async (plantId: string, wcId?: string, plId?: string) => {
    setPlReasonsLoading(true);
    try {
      const [cats, config] = await Promise.all([
        downtimeReasonCategoryApi.getAll(plantId),
        wcId && plId ? downtimeConfigApi.get(wcId, plId).catch(() => null) : Promise.resolve(null),
      ]);
      setPlReasonCategories(cats.filter((c) => c.isActive));
      const assignedIds = config?.applicableReasons.map((r) => r.id) ?? [];
      setPlSelectedReasonIds(assignedIds);
    } catch {
      setPlReasonCategories([]);
      setPlSelectedReasonIds([]);
    } finally {
      setPlReasonsLoading(false);
    }
  }, []);

  const openPlAdd = (wcId: string) => {
    const wc = groups.find((g) => g.groupId === wcId);
    setPlEditing(null);
    setPlWcId(wcId);
    setPlProductionLineId('');
    setPlDisplayName(wc?.baseName ?? '');
    setPlNumberOfWelders('0');
    setPlDowntimeEnabled(false);
    setPlDowntimeThreshold('5');
    setPlEnableWorkCenterChecklist(false);
    setPlEnableSafetyChecklist(false);
    setPlReasonCategories([]);
    setPlSelectedReasonIds([]);
    setPlError('');
    setPlModalOpen(true);
  };

  const openPlEdit = (wcId: string, config: WorkCenterProductionLine) => {
    setPlEditing(config);
    setPlWcId(wcId);
    setPlProductionLineId(config.productionLineId);
    setPlDisplayName(config.displayName);
    setPlNumberOfWelders(String(config.numberOfWelders));
    setPlDowntimeEnabled(config.downtimeTrackingEnabled);
    setPlDowntimeThreshold(String(config.downtimeThresholdMinutes));
    setPlEnableWorkCenterChecklist(!!config.enableWorkCenterChecklist);
    setPlEnableSafetyChecklist(!!config.enableSafetyChecklist);
    setPlError('');
    setPlModalOpen(true);
    const plantId = resolvePlantId(config.productionLineId);
    if (plantId) loadReasons(plantId, wcId, config.productionLineId);
  };

  const handlePlSave = async () => {
    setPlSaving(true);
    setPlError('');
    try {
      if (plEditing) {
        const updated = await adminWorkCenterApi.updateProductionLineConfig(plWcId, plEditing.productionLineId, {
          displayName: plDisplayName,
          numberOfWelders: Number(plNumberOfWelders),
          downtimeTrackingEnabled: plDowntimeEnabled,
          downtimeThresholdMinutes: Number(plDowntimeThreshold) || 5,
          enableWorkCenterChecklist: plEnableWorkCenterChecklist,
          enableSafetyChecklist: plEnableSafetyChecklist,
        });
        if (plDowntimeEnabled) {
          await downtimeConfigApi.setReasons(plWcId, plEditing.productionLineId, { reasonIds: plSelectedReasonIds });
        }
        setPlConfigs((prev) => ({
          ...prev,
          [plWcId]: (prev[plWcId] ?? []).map((c) => (c.id === updated.id ? updated : c)),
        }));
      } else {
        const created = await adminWorkCenterApi.createProductionLineConfig(plWcId, {
          productionLineId: plProductionLineId,
          displayName: plDisplayName,
          numberOfWelders: Number(plNumberOfWelders),
          enableWorkCenterChecklist: plEnableWorkCenterChecklist,
          enableSafetyChecklist: plEnableSafetyChecklist,
        });
        if (plDowntimeEnabled) {
          await downtimeConfigApi.update(plWcId, plProductionLineId, {
            downtimeTrackingEnabled: true,
            downtimeThresholdMinutes: Number(plDowntimeThreshold) || 5,
          });
          if (plSelectedReasonIds.length > 0) {
            await downtimeConfigApi.setReasons(plWcId, plProductionLineId, { reasonIds: plSelectedReasonIds });
          }
        }
        setPlConfigs((prev) => ({
          ...prev,
          [plWcId]: [...(prev[plWcId] ?? []), {
            ...created,
            downtimeTrackingEnabled: plDowntimeEnabled,
            downtimeThresholdMinutes: Number(plDowntimeThreshold) || 5,
          }],
        }));
      }
      setPlModalOpen(false);
    } catch {
      setPlError(plEditing ? 'Failed to update configuration.' : 'Failed to create configuration. It may already exist.');
    } finally {
      setPlSaving(false);
    }
  };

  const handlePlDelete = async (wcId: string, plId: string) => {
    try {
      await adminWorkCenterApi.deleteProductionLineConfig(wcId, plId);
      setPlConfigs((prev) => ({
        ...prev,
        [wcId]: (prev[wcId] ?? []).filter((c) => c.productionLineId !== plId),
      }));
    } catch {
      // no-op
    }
  };

  const existingPlIdsForWc = (wcId: string) => (plConfigs[wcId] ?? []).map((c) => c.productionLineId);

  return (
    <AdminLayout title="Production Line Work Centers">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
          {error && <div style={{ color: '#dc3545', marginBottom: 12 }}>{error}</div>}
          <div className={styles.grid}>
            {groups.map((group) => (
              <div key={group.groupId} className={styles.card}>
                <div className={styles.cardHeader}>
                  <span className={styles.cardTitle}>{group.baseName}</span>
                </div>
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Type</span>
                  <span className={styles.cardFieldValue}>{group.workCenterTypeName}</span>
                </div>

                <div style={{ marginTop: 12, borderTop: '1px solid #e5e5e5', paddingTop: 8 }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 }}>
                    <span style={{ fontWeight: 600, fontSize: 13 }}>Per-Production Line</span>
                    {isQualityManagerOrAbove && (
                      <Button appearance="subtle" icon={<AddRegular />} size="small" onClick={() => openPlAdd(group.groupId)} />
                    )}
                  </div>
                  {(plConfigs[group.groupId] ?? []).length === 0 ? (
                    <div style={{ fontSize: 12, color: '#868686' }}>No per-line overrides configured.</div>
                  ) : (
                    (plConfigs[group.groupId] ?? []).map((plc) => (
                      <div key={plc.id} style={{ display: 'flex', gap: 8, alignItems: 'center', fontSize: 13, padding: '2px 0' }}>
                        <span style={{ color: '#868686', minWidth: 100 }}>{plc.productionLineName} ({plc.plantName})</span>
                        <span style={{ flex: 1 }}>{plc.displayName}</span>
                        <span style={{ color: '#868686' }}>Welders: {plc.numberOfWelders}</span>
                        {isQualityManagerOrAbove && (
                          <>
                            <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openPlEdit(group.groupId, plc)} />
                            <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handlePlDelete(group.groupId, plc.productionLineId)} />
                          </>
                        )}
                      </div>
                    ))
                  )}
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      <AdminModal
        open={plModalOpen}
        title={plEditing ? 'Edit Production Line Config' : 'Add Production Line Config'}
        onConfirm={handlePlSave}
        onCancel={() => setPlModalOpen(false)}
        confirmLabel="Save"
        loading={plSaving}
        error={plError}
      >
        {!plEditing && (
          <>
            <Label>Production Line</Label>
            <Dropdown
              value={allProductionLines.find((pl) => pl.id === plProductionLineId)?.name ?? ''}
              selectedOptions={[plProductionLineId]}
              onOptionSelect={(_, d) => {
                const id = d.optionValue ?? '';
                setPlProductionLineId(id);
                const plantId = resolvePlantId(id);
                if (plantId && plDowntimeEnabled) loadReasons(plantId);
                else {
                  setPlReasonCategories([]);
                  setPlSelectedReasonIds([]);
                }
              }}
              placeholder="Select a production line..."
            >
              {allProductionLines
                .filter((pl) => !existingPlIdsForWc(plWcId).includes(pl.id))
                .map((pl) => (
                  <Option key={pl.id} value={pl.id} text={`${pl.name} (${pl.plantName})`}>
                    {pl.name} ({pl.plantName})
                  </Option>
                ))}
            </Dropdown>
          </>
        )}
        {plEditing && (
          <div style={{ marginBottom: 8 }}>
            <Label>Production Line</Label>
            <Input value={plEditing.productionLineName} disabled />
          </div>
        )}
        <Label>Display Name</Label>
        <Input value={plDisplayName} onChange={(_, d) => setPlDisplayName(d.value)} />
        <Label>Number of Welders</Label>
        <Input type="number" value={plNumberOfWelders} onChange={(_, d) => setPlNumberOfWelders(d.value)} />

        <div style={{ borderTop: '1px solid #dee2e6', marginTop: 12, paddingTop: 12 }}>
          <Switch
            label="Enable WC Checklists"
            checked={plEnableWorkCenterChecklist}
            onChange={(_, d) => setPlEnableWorkCenterChecklist(d.checked)}
          />
          <Switch
            label="Enable Safety Checklists"
            checked={plEnableSafetyChecklist}
            onChange={(_, d) => setPlEnableSafetyChecklist(d.checked)}
          />
        </div>

        <div style={{ borderTop: '1px solid #dee2e6', marginTop: 12, paddingTop: 12 }}>
          <Switch
            label="Enable Downtime Tracking"
            checked={plDowntimeEnabled}
            onChange={(_, d) => {
              setPlDowntimeEnabled(d.checked);
              if (d.checked && plReasonCategories.length === 0) {
                const plId = plEditing?.productionLineId ?? plProductionLineId;
                const plantId = resolvePlantId(plId);
                if (plantId) loadReasons(plantId, plEditing ? plWcId : undefined, plEditing ? plId : undefined);
              }
            }}
          />
          {plDowntimeEnabled && (
            <>
              <Label style={{ marginTop: 8 }}>Inactivity Threshold (minutes)</Label>
              <Input type="number" value={plDowntimeThreshold} onChange={(_, d) => setPlDowntimeThreshold(d.value)} min="1" />
              <Label style={{ marginTop: 12 }}>Applicable Reason Codes</Label>
              {plReasonsLoading ? (
                <Spinner size="tiny" label="Loading reasons..." />
              ) : plReasonCategories.length === 0 ? (
                <div style={{ fontSize: 12, color: '#868686', padding: '4px 0' }}>
                  No reason codes defined for this plant. Create them in Downtime Reasons first.
                </div>
              ) : (
                <div style={{ maxHeight: 220, overflowY: 'auto', border: '1px solid #e5e5e5', borderRadius: 4, padding: '4px 8px', marginTop: 4 }}>
                  {plReasonCategories.map((cat) => {
                    const activeReasons = cat.reasons.filter((r) => r.isActive);
                    if (activeReasons.length === 0) return null;
                    const allSelected = activeReasons.every((r) => plSelectedReasonIds.includes(r.id));
                    const someSelected = activeReasons.some((r) => plSelectedReasonIds.includes(r.id));
                    return (
                      <div key={cat.id} style={{ marginBottom: 6 }}>
                        <Checkbox
                          checked={allSelected ? true : someSelected ? 'mixed' : false}
                          onChange={() => {
                            const ids = activeReasons.map((r) => r.id);
                            setPlSelectedReasonIds((prev) =>
                              allSelected ? prev.filter((id) => !ids.includes(id)) : [...new Set([...prev, ...ids])],
                            );
                          }}
                          label={<span style={{ fontWeight: 600, fontSize: 13 }}>{cat.name}</span>}
                        />
                        <div style={{ paddingLeft: 24 }}>
                          {activeReasons.map((reason) => (
                            <Checkbox
                              key={reason.id}
                              checked={plSelectedReasonIds.includes(reason.id)}
                              onChange={(_, d) => {
                                setPlSelectedReasonIds((prev) =>
                                  d.checked ? [...prev, reason.id] : prev.filter((id) => id !== reason.id),
                                );
                              }}
                              label={<span style={{ fontSize: 13 }}>{reason.name}</span>}
                            />
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </>
          )}
        </div>
      </AdminModal>
    </AdminLayout>
  );
}
