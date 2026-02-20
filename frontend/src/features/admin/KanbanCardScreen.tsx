import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Spinner } from '@fluentui/react-components';
import { DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminKanbanCardApi } from '../../api/endpoints.ts';
import type { AdminBarcodeCard } from '../../types/domain.ts';
import styles from './CardList.module.css';

const COLOR_MAP: Record<string, string> = {
  Red: '#ff0000', Yellow: '#ffd700', Blue: '#0066ff',
  Green: '#00aa00', Orange: '#ff8c00', Purple: '#8800cc',
};

export function KanbanCardScreen() {
  const [items, setItems] = useState<AdminBarcodeCard[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [cardValue, setCardValue] = useState('');
  const [color, setColor] = useState('');
  const [description, setDescription] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try { setItems(await adminKanbanCardApi.getAll()); }
    catch { setError('Failed to load kanban cards.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setCardValue(''); setColor(''); setDescription('');
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const created = await adminKanbanCardApi.create({
        cardValue, color: color || undefined, description: description || undefined,
      });
      setItems(prev => [...prev, created]);
      setModalOpen(false);
    } catch { setError('Failed to create card.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Remove this kanban card?')) return;
    try { await adminKanbanCardApi.remove(id); setItems(prev => prev.filter(c => c.id !== id)); }
    catch { alert('Failed to remove card.'); }
  };

  return (
    <AdminLayout title="Kanban Card Management" onAdd={openAdd} addLabel="Add Card">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No kanban cards found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>
                  {item.color && (
                    <span
                      className={styles.colorSwatch}
                      style={{ backgroundColor: COLOR_MAP[item.color] ?? item.color }}
                    />
                  )}
                  Card {item.cardValue}
                </span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              {item.color && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Color</span>
                  <span className={styles.cardFieldValue}>{item.color}</span>
                </div>
              )}
              {item.description && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Description</span>
                  <span className={styles.cardFieldValue}>{item.description}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title="Add Kanban Card"
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Add"
        loading={saving}
        error={error}
        confirmDisabled={!cardValue}
      >
        <Label>Card Value (barcode)</Label>
        <Input value={cardValue} onChange={(_, d) => setCardValue(d.value)} placeholder="e.g. 06" />
        <Label>Color Name</Label>
        <Input value={color} onChange={(_, d) => setColor(d.value)} placeholder="e.g. Red, Blue" />
        <Label>Description (optional)</Label>
        <Input value={description} onChange={(_, d) => setDescription(d.value)} />
      </AdminModal>
    </AdminLayout>
  );
}
