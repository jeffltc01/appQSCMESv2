import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner, Switch } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminWorkCenterGroup, WorkCenterType } from '../../types/domain.ts';
import styles from './CardList.module.css';

const DATA_ENTRY_TYPES = [
  'Rolls', 'Barcode-LongSeam', 'Barcode-LongSeamInsp', 'Barcode-RoundSeam', 'Barcode-RoundSeamInsp',
  'Fitup', 'Hydro', 'Spot', 'DataPlate',
  'RealTimeXray', 'Plasma', 'MatQueue-Material', 'MatQueue-Fitup', 'MatQueue-Shell',
];

export function WorkCenterConfigScreen() {
  const { user } = useAuth();
  const isAdmin = (user?.roleTier ?? 99) <= 1;

  const [groups, setGroups] = useState<AdminWorkCenterGroup[]>([]);
  const [wcTypes, setWcTypes] = useState<WorkCenterType[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminWorkCenterGroup | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [baseName, setBaseName] = useState('');
  const [productionSequence, setProductionSequence] = useState('');
  const [dataEntryType, setDataEntryType] = useState('');
  const [materialQueueForWCId, setMaterialQueueForWCId] = useState('');
  const [isHoldTagEnabled, setIsHoldTagEnabled] = useState(false);

  // Create work center modal state
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createName, setCreateName] = useState('');
  const [createTypeId, setCreateTypeId] = useState('');
  const [createProductionSequence, setCreateProductionSequence] = useState('');
  const [createDataEntryType, setCreateDataEntryType] = useState('');
  const [createMqId, setCreateMqId] = useState('');
  const [createIsHoldTagEnabled, setCreateIsHoldTagEnabled] = useState(false);
  const [createSaving, setCreateSaving] = useState(false);
  const [createError, setCreateError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [groupData, typesData] = await Promise.all([
        adminWorkCenterApi.getGrouped(),
        adminWorkCenterApi.getTypes(),
      ]);
      setGroups(groupData);
      setWcTypes(typesData);
    } catch {
      setError('Failed to load work centers.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const allWcOptions = groups.map(g => ({ id: g.groupId, name: g.baseName }));

  const openEdit = (group: AdminWorkCenterGroup) => {
    setEditing(group);
    setBaseName(group.baseName);
    setProductionSequence(group.productionSequence != null ? String(group.productionSequence) : '');
    setDataEntryType(group.dataEntryType ?? '');
    setMaterialQueueForWCId(group.siteConfigs[0]?.materialQueueForWCId ?? '');
    setIsHoldTagEnabled(group.isHoldTagEnabled ?? false);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true); setError('');
    try {
      const updated = await adminWorkCenterApi.updateGroup(editing.groupId, {
        baseName,
        productionSequence: productionSequence.trim() === '' ? undefined : Number(productionSequence),
        dataEntryType: dataEntryType || undefined,
        materialQueueForWCId: materialQueueForWCId || undefined,
        isHoldTagEnabled,
      });
      setGroups(prev => prev.map(g => g.groupId === updated.groupId ? updated : g));
      setModalOpen(false);
    } catch { setError('Failed to save config.'); }
    finally { setSaving(false); }
  };

  const openCreate = () => {
    setCreateName('');
    setCreateTypeId(wcTypes[0]?.id ?? '');
    setCreateProductionSequence('');
    setCreateDataEntryType('');
    setCreateMqId('');
    setCreateIsHoldTagEnabled(false);
    setCreateError('');
    setCreateModalOpen(true);
  };

  const handleCreate = async () => {
    if (!createName || !createTypeId) return;
    setCreateSaving(true); setCreateError('');
    try {
      const created = await adminWorkCenterApi.create({
        name: createName,
        workCenterTypeId: createTypeId,
        productionSequence: createProductionSequence.trim() === '' ? undefined : Number(createProductionSequence),
        dataEntryType: createDataEntryType || undefined,
        materialQueueForWCId: createMqId || undefined,
        isHoldTagEnabled: createIsHoldTagEnabled,
      });
      setGroups(prev => [...prev, created].sort((a, b) => a.baseName.localeCompare(b.baseName)));
      setCreateModalOpen(false);
    } catch { setCreateError('Failed to create work center. The name may already be in use.'); }
    finally { setCreateSaving(false); }
  };

  const findWcName = (id?: string) => {
    if (!id) return undefined;
    return allWcOptions.find(w => w.id === id)?.name;
  };

  return (
    <AdminLayout title="Work Centers" onAdd={isAdmin ? openCreate : undefined}>
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {groups.map(group => (
            <div key={group.groupId} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{group.baseName}</span>
                <div className={styles.cardActions}>
                  {isAdmin && (
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(group)} />
                  )}
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
              {group.productionSequence != null && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Sequence</span>
                  <span className={styles.cardFieldValue}>{group.productionSequence}</span>
                </div>
              )}
              {group.siteConfigs[0]?.materialQueueForWCName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Queue For</span>
                  <span className={styles.cardFieldValue}>{group.siteConfigs[0].materialQueueForWCName}</span>
                </div>
              )}
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Hold Tag Enabled</span>
                <span className={styles.cardFieldValue}>
                  {group.isHoldTagEnabled ? 'Yes' : 'No'}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Base WC Config Modal (Admin only) */}
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
        <Label>Production Sequence</Label>
        <Input
          value={productionSequence}
          onChange={(_, d) => setProductionSequence(d.value)}
          type="number"
          step="0.1"
          placeholder="e.g. 4.5"
        />
        <Label>Data Entry Type</Label>
        <Dropdown
          value={dataEntryType || 'None'}
          selectedOptions={[dataEntryType]}
          onOptionSelect={(_, d) => setDataEntryType(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {DATA_ENTRY_TYPES.map(t => <Option key={t} value={t}>{t}</Option>)}
        </Dropdown>

        {dataEntryType.startsWith('MatQueue') && (
          <>
            <Label>Material Queue For WC</Label>
            <Dropdown
              value={findWcName(materialQueueForWCId) ?? 'None'}
              selectedOptions={[materialQueueForWCId]}
              onOptionSelect={(_, d) => setMaterialQueueForWCId(d.optionValue ?? '')}
            >
              <Option value="">None</Option>
              {allWcOptions
                .filter(w => w.id !== editing?.groupId)
                .map(w => (
                  <Option key={w.id} value={w.id} text={w.name}>
                    {w.name}
                  </Option>
                ))
              }
            </Dropdown>
          </>
        )}
        <Switch
          label="Enable Hold Tag Entry"
          checked={isHoldTagEnabled}
          onChange={(_, d) => setIsHoldTagEnabled(d.checked)}
        />
      </AdminModal>

      {/* Create Work Center Modal (Admin only) */}
      <AdminModal
        open={createModalOpen}
        title="Add Work Center"
        onConfirm={handleCreate}
        onCancel={() => setCreateModalOpen(false)}
        confirmLabel="Create"
        loading={createSaving}
        error={createError}
        confirmDisabled={!createName || !createTypeId}
      >
        <Label>Name</Label>
        <Input value={createName} onChange={(_, d) => setCreateName(d.value)} placeholder="e.g. Rolls" />
        <Label>Work Center Type</Label>
        <Dropdown
          value={wcTypes.find(t => t.id === createTypeId)?.name ?? ''}
          selectedOptions={[createTypeId]}
          onOptionSelect={(_, d) => setCreateTypeId(d.optionValue ?? '')}
        >
          {wcTypes.map(t => <Option key={t.id} value={t.id}>{t.name}</Option>)}
        </Dropdown>
        <Label>Production Sequence</Label>
        <Input
          value={createProductionSequence}
          onChange={(_, d) => setCreateProductionSequence(d.value)}
          type="number"
          step="0.1"
          placeholder="e.g. 4.5"
        />
        <Label>Data Entry Type</Label>
        <Dropdown
          value={createDataEntryType || 'None'}
          selectedOptions={[createDataEntryType]}
          onOptionSelect={(_, d) => setCreateDataEntryType(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {DATA_ENTRY_TYPES.map(t => <Option key={t} value={t}>{t}</Option>)}
        </Dropdown>
        {createDataEntryType.startsWith('MatQueue') && (
          <>
            <Label>Material Queue For WC</Label>
            <Dropdown
              value={allWcOptions.find(w => w.id === createMqId)?.name ?? 'None'}
              selectedOptions={[createMqId]}
              onOptionSelect={(_, d) => setCreateMqId(d.optionValue ?? '')}
            >
              <Option value="">None</Option>
              {allWcOptions.map(w => (
                <Option key={w.id} value={w.id} text={w.name}>{w.name}</Option>
              ))}
            </Dropdown>
          </>
        )}
        <Switch
          label="Enable Hold Tag Entry"
          checked={createIsHoldTagEnabled}
          onChange={(_, d) => setCreateIsHoldTagEnabled(d.checked)}
        />
      </AdminModal>
    </AdminLayout>
  );
}
