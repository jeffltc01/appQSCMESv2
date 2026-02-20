import { useState, useEffect, useCallback } from 'react';
import { Button, Label, Dropdown, Option, Checkbox, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminControlPlanApi } from '../../api/endpoints.ts';
import type { AdminControlPlan } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function ControlPlansScreen() {
  const [items, setItems] = useState<AdminControlPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminControlPlan | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [isEnabled, setIsEnabled] = useState(true);
  const [resultType, setResultType] = useState('PassFail');
  const [isGateCheck, setIsGateCheck] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try { setItems(await adminControlPlanApi.getAll()); }
    catch { setError('Failed to load control plans.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openEdit = (item: AdminControlPlan) => {
    setEditing(item);
    setIsEnabled(item.isEnabled); setResultType(item.resultType); setIsGateCheck(item.isGateCheck);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true); setError('');
    try {
      const updated = await adminControlPlanApi.update(editing.id, { isEnabled, resultType, isGateCheck });
      setItems(prev => prev.map(cp => cp.id === updated.id ? updated : cp));
      setModalOpen(false);
    } catch { setError('Failed to save control plan.'); }
    finally { setSaving(false); }
  };

  return (
    <AdminLayout title="Control Plans">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No control plans found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.characteristicName}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{item.workCenterName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Result Type</span>
                <span className={styles.cardFieldValue}>{item.resultType}</span>
              </div>
              <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>
                <span className={`${styles.badge} ${item.isEnabled ? styles.badgeGreen : styles.badgeRed}`}>
                  {item.isEnabled ? 'Enabled' : 'Disabled'}
                </span>
                {item.isGateCheck && (
                  <span className={`${styles.badge} ${styles.badgeBlue}`}>Gate Check</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={`Edit Control Plan: ${editing?.characteristicName ?? ''}`}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={error}
      >
        <Checkbox label="Enabled" checked={isEnabled} onChange={(_, d) => setIsEnabled(!!d.checked)} />
        <Label>Result Type</Label>
        <Dropdown value={resultType} selectedOptions={[resultType]} onOptionSelect={(_, d) => { if (d.optionValue) setResultType(d.optionValue); }}>
          <Option value="PassFail">PassFail</Option>
        </Dropdown>
        <Checkbox label="Gate Check" checked={isGateCheck} onChange={(_, d) => setIsGateCheck(!!d.checked)} />
      </AdminModal>
    </AdminLayout>
  );
}
