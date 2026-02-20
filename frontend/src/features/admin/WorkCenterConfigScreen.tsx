import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminWorkCenterGroup, WorkCenterSiteConfig } from '../../types/domain.ts';
import styles from './CardList.module.css';

const DATA_ENTRY_TYPES = [
  'Rolls', 'Barcode', 'Fitup', 'Hydro', 'Spot', 'DataPlate',
  'RealTimeXray', 'Plasma', 'MatQueue-Material', 'MatQueue-Fitup', 'MatQueue-Shell',
];

interface SiteConfigEdit {
  workCenterId: string;
  plantName: string;
  siteName: string;
  numberOfWelders: string;
  materialQueueForWCId: string;
}

export function WorkCenterConfigScreen() {
  const [groups, setGroups] = useState<AdminWorkCenterGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminWorkCenterGroup | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [baseName, setBaseName] = useState('');
  const [dataEntryType, setDataEntryType] = useState('');
  const [siteEdits, setSiteEdits] = useState<SiteConfigEdit[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try { setGroups(await adminWorkCenterApi.getGrouped()); }
    catch { setError('Failed to load work centers.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const allSiteConfigs = groups.flatMap(g => g.siteConfigs);

  const openEdit = (group: AdminWorkCenterGroup) => {
    setEditing(group);
    setBaseName(group.baseName);
    setDataEntryType(group.dataEntryType ?? '');
    setSiteEdits(group.siteConfigs.map(sc => ({
      workCenterId: sc.workCenterId,
      plantName: sc.plantName,
      siteName: sc.siteName,
      numberOfWelders: String(sc.numberOfWelders),
      materialQueueForWCId: sc.materialQueueForWCId ?? '',
    })));
    setError(''); setModalOpen(true);
  };

  const updateSiteEdit = (idx: number, field: keyof SiteConfigEdit, value: string) => {
    setSiteEdits(prev => prev.map((se, i) => i === idx ? { ...se, [field]: value } : se));
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true); setError('');
    try {
      const updated = await adminWorkCenterApi.updateGroup(editing.groupId, {
        baseName,
        dataEntryType: dataEntryType || undefined,
        siteConfigs: siteEdits.map(se => ({
          workCenterId: se.workCenterId,
          siteName: se.siteName,
          numberOfWelders: Number(se.numberOfWelders),
          materialQueueForWCId: se.materialQueueForWCId || undefined,
        })),
      });
      setGroups(prev => prev.map(g => g.groupId === updated.groupId ? updated : g));
      setModalOpen(false);
    } catch { setError('Failed to save config.'); }
    finally { setSaving(false); }
  };

  const findWcName = (id?: string) => {
    if (!id) return undefined;
    const sc = allSiteConfigs.find(s => s.workCenterId === id);
    return sc?.siteName;
  };

  return (
    <AdminLayout title="Work Center Config">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {groups.map(group => (
            <div key={group.groupId} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{group.baseName}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(group)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{group.workCenterTypeName}</span>
              </div>
              {group.dataEntryType && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Entry</span>
                  <span className={styles.cardFieldValue}>{group.dataEntryType}</span>
                </div>
              )}
              <div style={{ marginTop: 8, display: 'flex', flexDirection: 'column', gap: 4 }}>
                {group.siteConfigs.map(sc => (
                  <div key={sc.workCenterId} style={{ display: 'flex', gap: 8, alignItems: 'center', fontSize: 13 }}>
                    <span className={`${styles.badge} ${styles.badgeBlue}`}>{sc.plantName}</span>
                    <span style={{ flex: 1 }}>{sc.siteName}</span>
                    <span style={{ color: '#868686' }}>Welders: {sc.numberOfWelders}</span>
                    {sc.materialQueueForWCName && (
                      <span style={{ color: '#868686', fontSize: 12 }}>Queue: {sc.materialQueueForWCName}</span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={`Edit ${editing?.baseName ?? 'Work Center'}`}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={error}
      >
        <Label>Base Name</Label>
        <Input value={baseName} onChange={(_, d) => setBaseName(d.value)} />
        <Label>Data Entry Type</Label>
        <Dropdown
          value={dataEntryType || 'None'}
          selectedOptions={[dataEntryType]}
          onOptionSelect={(_, d) => setDataEntryType(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {DATA_ENTRY_TYPES.map(t => <Option key={t} value={t}>{t}</Option>)}
        </Dropdown>

        <div style={{ marginTop: 12, display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Label style={{ fontWeight: 600 }}>Per-Site Configuration</Label>
          {siteEdits.map((se, idx) => (
            <div key={se.workCenterId} style={{ border: '1px solid #e5e5e5', padding: 10, display: 'flex', flexDirection: 'column', gap: 6 }}>
              <span style={{ fontWeight: 600, fontSize: 13 }}>{se.plantName}</span>
              <Label>Name at this site</Label>
              <Input value={se.siteName} onChange={(_, d) => updateSiteEdit(idx, 'siteName', d.value)} />
              <Label>Number of Welders</Label>
              <Input type="number" value={se.numberOfWelders} onChange={(_, d) => updateSiteEdit(idx, 'numberOfWelders', d.value)} />
              <Label>Material Queue For WC</Label>
              <Dropdown
                value={findWcName(se.materialQueueForWCId) ?? 'None'}
                selectedOptions={[se.materialQueueForWCId]}
                onOptionSelect={(_, d) => updateSiteEdit(idx, 'materialQueueForWCId', d.optionValue ?? '')}
              >
                <Option value="">None</Option>
                {allSiteConfigs
                  .filter(s => s.workCenterId !== se.workCenterId && s.plantName === se.plantName)
                  .map(s => (
                    <Option key={s.workCenterId} value={s.workCenterId} text={s.siteName}>
                      {s.siteName}
                    </Option>
                  ))
                }
              </Dropdown>
            </div>
          ))}
        </div>
      </AdminModal>
    </AdminLayout>
  );
}
