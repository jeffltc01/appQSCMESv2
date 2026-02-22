import { useState, useEffect, useCallback } from 'react';
import { Button, Label, Dropdown, Option, Checkbox, Spinner, Badge } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminControlPlanApi, adminCharacteristicApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminControlPlan, AdminCharacteristic } from '../../types/domain.ts';
import type { WorkCenterProductionLine } from '../../types/domain.ts';
import { adminWorkCenterApi } from '../../api/endpoints.ts';
import styles from './CardList.module.css';

const RESULT_TYPES = ['PassFail', 'AcceptReject', 'GoNoGo', 'NumericInt', 'NumericDecimal', 'Text'];

export function ControlPlansScreen() {
  const { user } = useAuth();
  const isAdmin = (user?.roleTier ?? 99) <= 1;
  const [items, setItems] = useState<AdminControlPlan[]>([]);
  const [characteristics, setCharacteristics] = useState<AdminCharacteristic[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminControlPlan | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [characteristicId, setCharacteristicId] = useState('');
  const [wcplId, setWcplId] = useState('');
  const [isEnabled, setIsEnabled] = useState(true);
  const [resultType, setResultType] = useState('PassFail');
  const [isGateCheck, setIsGateCheck] = useState(false);
  const [codeRequired, setCodeRequired] = useState(false);
  const [wcpls, setWcpls] = useState<WorkCenterProductionLine[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [plans, chars] = await Promise.all([
        adminControlPlanApi.getAll(),
        adminCharacteristicApi.getAll(),
      ]);
      setItems(plans);
      setCharacteristics(chars.filter(c => c.isActive));
    } catch { setError('Failed to load control plans.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const loadWcpls = useCallback(async () => {
    try {
      const wcs = await adminWorkCenterApi.getAll();
      const allWcpls: WorkCenterProductionLine[] = [];
      for (const wc of wcs) {
        try {
          const configs = await adminWorkCenterApi.getProductionLineConfigs(wc.id);
          allWcpls.push(...configs);
        } catch { /* skip */ }
      }
      setWcpls(allWcpls);
    } catch { /* skip */ }
  }, []);

  const openCreate = () => {
    setEditing(null); setIsCreating(true);
    setCharacteristicId(''); setWcplId('');
    setIsEnabled(true); setResultType('PassFail');
    setIsGateCheck(false); setCodeRequired(false);
    setError(''); setModalOpen(true);
    loadWcpls();
  };

  const openEdit = (item: AdminControlPlan) => {
    setEditing(item); setIsCreating(false);
    setIsEnabled(item.isEnabled);
    setResultType(item.resultType);
    setIsGateCheck(item.isGateCheck);
    setCodeRequired(item.codeRequired);
    setError(''); setModalOpen(true);
  };

  const handleCodeRequiredChange = (checked: boolean) => {
    setCodeRequired(checked);
    if (checked) setIsGateCheck(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (isCreating) {
        if (!characteristicId || !wcplId) { setError('Characteristic and Work Center are required.'); setSaving(false); return; }
        const created = await adminControlPlanApi.create({
          characteristicId, workCenterProductionLineId: wcplId,
          isEnabled, resultType, isGateCheck, codeRequired,
        });
        setItems(prev => [...prev, created]);
      } else if (editing) {
        const updated = await adminControlPlanApi.update(editing.id, {
          isEnabled, resultType, isGateCheck, codeRequired, isActive: editing.isActive,
        });
        setItems(prev => prev.map(cp => cp.id === updated.id ? updated : cp));
      }
      setModalOpen(false);
    } catch { setError('Failed to save control plan.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (item: AdminControlPlan) => {
    try {
      const updated = await adminControlPlanApi.remove(item.id);
      setItems(prev => prev.map(cp => cp.id === updated.id ? updated : cp));
    } catch { setError('Failed to deactivate control plan.'); }
  };

  return (
    <AdminLayout title="Control Plans" onAdd={isAdmin ? openCreate : undefined}>
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No control plans found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card} style={!item.isActive ? { opacity: 0.6 } : undefined}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.characteristicName}</span>
                <div className={styles.cardActions}>
                  {!item.isActive && <Badge appearance="filled" color="danger" size="small">Inactive</Badge>}
                  {isAdmin && (
                    <>
                      <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                      {item.isActive && (
                        <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item)} />
                      )}
                    </>
                  )}
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{item.workCenterName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Production Line</span>
                <span className={styles.cardFieldValue}>{item.productionLineName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Result Type</span>
                <span className={styles.cardFieldValue}>{item.resultType}</span>
              </div>
              <div style={{ display: 'flex', gap: 6, marginTop: 4, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${item.isEnabled ? styles.badgeGreen : styles.badgeRed}`}>
                  {item.isEnabled ? 'Enabled' : 'Disabled'}
                </span>
                {item.isGateCheck && (
                  <span className={`${styles.badge} ${styles.badgeBlue}`}>Gate Check</span>
                )}
                {item.codeRequired && (
                  <span className={`${styles.badge} ${styles.badgeBlue}`}>Code Required</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={isCreating ? 'Add Control Plan' : `Edit Control Plan: ${editing?.characteristicName ?? ''}`}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={error}
      >
        {isCreating && (
          <>
            <Label>Characteristic</Label>
            <Dropdown
              value={characteristics.find(c => c.id === characteristicId)?.name ?? ''}
              selectedOptions={[characteristicId]}
              onOptionSelect={(_, d) => setCharacteristicId(d.optionValue ?? '')}
            >
              {characteristics.map(c => <Option key={c.id} value={c.id}>{c.name}</Option>)}
            </Dropdown>
            <Label>Work Center / Production Line</Label>
            <Dropdown
              value={wcpls.find(w => w.id === wcplId)?.displayName ?? ''}
              selectedOptions={[wcplId]}
              onOptionSelect={(_, d) => setWcplId(d.optionValue ?? '')}
            >
              {wcpls.map(w => <Option key={w.id} value={w.id}>{w.displayName}</Option>)}
            </Dropdown>
          </>
        )}
        <Checkbox label="Enabled" checked={isEnabled} onChange={(_, d) => setIsEnabled(!!d.checked)} />
        <Label>Result Type</Label>
        <Dropdown value={resultType} selectedOptions={[resultType]} onOptionSelect={(_, d) => { if (d.optionValue) setResultType(d.optionValue); }}>
          {RESULT_TYPES.map(rt => <Option key={rt} value={rt}>{rt}</Option>)}
        </Dropdown>
        <Checkbox
          label="Gate Check"
          checked={isGateCheck}
          disabled={codeRequired}
          onChange={(_, d) => setIsGateCheck(!!d.checked)}
        />
        <Checkbox
          label="Code Required (ASME)"
          checked={codeRequired}
          onChange={(_, d) => handleCodeRequiredChange(!!d.checked)}
        />
      </AdminModal>
    </AdminLayout>
  );
}
