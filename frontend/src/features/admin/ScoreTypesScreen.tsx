import { useCallback, useEffect, useState } from 'react';
import { Button, Checkbox, Input, Label, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { checklistApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { ScoreType } from '../../types/domain.ts';
import type { UpsertScoreTypeRequest } from '../../types/api.ts';
import styles from './CardList.module.css';

type EditableValue = { id?: string; score: string; description: string };

export function ScoreTypesScreen() {
  const { user } = useAuth();
  const canManage = (user?.roleTier ?? 99) <= 2;

  const [items, setItems] = useState<ScoreType[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [editing, setEditing] = useState<ScoreType | null>(null);
  const [name, setName] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [values, setValues] = useState<EditableValue[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await checklistApi.getScoreTypes(true);
      setItems(data);
    } catch {
      setError('Failed to load score types.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const openCreate = () => {
    setEditing(null);
    setName('');
    setIsActive(true);
    setValues([{ score: '', description: '' }]);
    setError('');
    setModalOpen(true);
  };

  const openEdit = (item: ScoreType) => {
    setEditing(item);
    setName(item.name);
    setIsActive(item.isActive);
    setValues(item.values.map((value) => ({ id: value.id, score: String(value.score), description: value.description })));
    setError('');
    setModalOpen(true);
  };

  const updateValue = (index: number, patch: Partial<EditableValue>) => {
    setValues((prev) => prev.map((value, i) => (i === index ? { ...value, ...patch } : value)));
  };

  const addValue = () => setValues((prev) => [...prev, { score: '', description: '' }]);
  const removeValue = (index: number) => setValues((prev) => prev.filter((_, i) => i !== index));

  const handleArchive = async (item: ScoreType) => {
    setError('');
    try {
      const payload: UpsertScoreTypeRequest = {
        id: item.id,
        name: item.name,
        isActive: false,
        values: item.values.map((value, index) => ({
          id: value.id,
          score: value.score,
          description: value.description,
          sortOrder: index + 1,
        })),
      };
      await checklistApi.upsertScoreType(payload);
      await load();
    } catch {
      setError('Failed to archive score type.');
    }
  };

  const handleSave = async () => {
    const trimmedName = name.trim();
    const normalizedValues = values
      .map((value) => ({
        id: value.id,
        score: Number(value.score),
        description: value.description.trim(),
      }))
      .filter((value) => value.description);

    if (!trimmedName) {
      setError('Name is required.');
      return;
    }
    if (!normalizedValues.length || normalizedValues.some((value) => Number.isNaN(value.score))) {
      setError('At least one score value with numeric score is required.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      await checklistApi.upsertScoreType({
        id: editing?.id,
        name: trimmedName,
        isActive,
        values: normalizedValues.map((value, index) => ({
          id: value.id,
          score: value.score,
          description: value.description,
          sortOrder: index + 1,
        })),
      });
      setModalOpen(false);
      await load();
    } catch {
      setError('Failed to save score type.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <AdminLayout title="Checklist Score Types" onAdd={canManage ? openCreate : undefined} addLabel="Add Score Type">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No score types found.</div>}
          {items.map((item) => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                {canManage && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  </div>
                )}
              </div>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${item.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                  {item.isActive ? 'Active' : 'Archived'}
                </span>
                <span className={`${styles.badge} ${styles.badgeBlue}`}>{item.values.length} values</span>
              </div>
              <div>
                {item.values.map((value) => (
                  <div key={value.id ?? `${item.id}-${value.sortOrder}`} className={styles.cardField}>
                    <span className={styles.cardFieldLabel}>{value.score}</span>
                    <span className={styles.cardFieldValue}>{value.description}</span>
                  </div>
                ))}
              </div>
              {canManage && item.isActive && (
                <Button appearance="secondary" size="small" onClick={() => handleArchive(item)}>
                  Archive
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Score Type' : 'Add Score Type'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name.trim()}
        wide
      >
        <Label>Name</Label>
        <Input value={name} onChange={(_, data) => setName(data.value)} />
        <Checkbox label="Active" checked={isActive} onChange={(_, data) => setIsActive(!!data.checked)} />
        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8 }}>
          <Label>Values</Label>
          <Button size="small" onClick={addValue}>Add Value</Button>
        </div>
        {values.map((value, index) => (
          <div key={value.id ?? `value-${index}`} style={{ border: '1px solid #dee2e6', borderRadius: 6, padding: 8, marginTop: 8 }}>
            <div className={styles.formGrid}>
              <div className={styles.formColumn}>
                <Label>Score</Label>
                <Input type="number" value={value.score} onChange={(_, data) => updateValue(index, { score: data.value })} />
              </div>
              <div className={styles.formColumn}>
                <Label>Description</Label>
                <Input value={value.description} onChange={(_, data) => updateValue(index, { description: data.value })} />
              </div>
            </div>
            <Button appearance="subtle" size="small" onClick={() => removeValue(index)} disabled={values.length <= 1}>
              Remove Value
            </Button>
          </div>
        ))}
      </AdminModal>
    </AdminLayout>
  );
}
