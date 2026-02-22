import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Spinner, Dropdown, Option } from '@fluentui/react-components';
import { EditRegular, DeleteRegular, AddRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { downtimeReasonCategoryApi, downtimeReasonApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { DowntimeReasonCategory, DowntimeReason, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';
import panelStyles from './DowntimeReasonsScreen.module.css';

export function DowntimeReasonsScreen() {
  const { user } = useAuth();
  const canEdit = (user?.roleTier ?? 99) <= 3;

  const [plants, setPlants] = useState<Plant[]>([]);
  const [selectedPlantId, setSelectedPlantId] = useState(user?.defaultSiteId ?? '');
  const [categories, setCategories] = useState<DowntimeReasonCategory[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const [catModalOpen, setCatModalOpen] = useState(false);
  const [editingCat, setEditingCat] = useState<DowntimeReasonCategory | null>(null);
  const [catName, setCatName] = useState('');
  const [catSortOrder, setCatSortOrder] = useState(0);
  const [catSaving, setCatSaving] = useState(false);
  const [catError, setCatError] = useState('');
  const [deleteCatTarget, setDeleteCatTarget] = useState<DowntimeReasonCategory | null>(null);
  const [deletingCat, setDeletingCat] = useState(false);

  const [reasonModalOpen, setReasonModalOpen] = useState(false);
  const [editingReason, setEditingReason] = useState<DowntimeReason | null>(null);
  const [reasonName, setReasonName] = useState('');
  const [reasonSortOrder, setReasonSortOrder] = useState(0);
  const [reasonSaving, setReasonSaving] = useState(false);
  const [reasonError, setReasonError] = useState('');
  const [deleteReasonTarget, setDeleteReasonTarget] = useState<DowntimeReason | null>(null);
  const [deletingReason, setDeletingReason] = useState(false);

  const loadPlants = useCallback(async () => {
    try {
      const data = await siteApi.getSites();
      setPlants(data);
      if (!selectedPlantId && data.length > 0) {
        setSelectedPlantId(user?.defaultSiteId ?? data[0].id);
      }
    } catch { /* plants load failure is non-critical */ }
  }, [user?.defaultSiteId, selectedPlantId]);

  const loadCategories = useCallback(async () => {
    if (!selectedPlantId) return;
    setLoading(true);
    try {
      const data = await downtimeReasonCategoryApi.getAll(selectedPlantId);
      setCategories(data);
      if (data.length > 0 && (!selectedCategoryId || !data.find(c => c.id === selectedCategoryId))) {
        setSelectedCategoryId(data[0].id);
      } else if (data.length === 0) {
        setSelectedCategoryId(null);
      }
    } catch { setCatError('Failed to load categories.'); }
    finally { setLoading(false); }
  }, [selectedPlantId, selectedCategoryId]);

  useEffect(() => { loadPlants(); }, [loadPlants]);
  useEffect(() => { loadCategories(); }, [loadCategories]);

  const selectedCategory = categories.find(c => c.id === selectedCategoryId);
  const reasons = selectedCategory?.reasons ?? [];

  // ---- Category handlers ----

  const openAddCat = () => {
    setEditingCat(null);
    setCatName(''); setCatSortOrder(categories.length);
    setCatError(''); setCatModalOpen(true);
  };

  const openEditCat = (cat: DowntimeReasonCategory) => {
    setEditingCat(cat);
    setCatName(cat.name); setCatSortOrder(cat.sortOrder);
    setCatError(''); setCatModalOpen(true);
  };

  const handleSaveCat = async () => {
    setCatSaving(true); setCatError('');
    try {
      if (editingCat) {
        await downtimeReasonCategoryApi.update(editingCat.id, {
          name: catName, sortOrder: catSortOrder, isActive: editingCat.isActive
        });
      } else {
        await downtimeReasonCategoryApi.create({
          plantId: selectedPlantId, name: catName, sortOrder: catSortOrder
        });
      }
      setCatModalOpen(false);
      await loadCategories();
    } catch { setCatError('Failed to save category.'); }
    finally { setCatSaving(false); }
  };

  const handleDeleteCat = async () => {
    if (!deleteCatTarget) return;
    setDeletingCat(true);
    try {
      await downtimeReasonCategoryApi.delete(deleteCatTarget.id);
      setDeleteCatTarget(null);
      await loadCategories();
    } catch { alert('Failed to deactivate category.'); }
    finally { setDeletingCat(false); }
  };

  // ---- Reason handlers ----

  const openAddReason = () => {
    setEditingReason(null);
    setReasonName(''); setReasonSortOrder(reasons.length);
    setReasonError(''); setReasonModalOpen(true);
  };

  const openEditReason = (reason: DowntimeReason) => {
    setEditingReason(reason);
    setReasonName(reason.name); setReasonSortOrder(reason.sortOrder);
    setReasonError(''); setReasonModalOpen(true);
  };

  const handleSaveReason = async () => {
    setReasonSaving(true); setReasonError('');
    try {
      if (editingReason) {
        await downtimeReasonApi.update(editingReason.id, {
          name: reasonName, sortOrder: reasonSortOrder, isActive: editingReason.isActive
        });
      } else {
        await downtimeReasonApi.create({
          downtimeReasonCategoryId: selectedCategoryId!,
          name: reasonName, sortOrder: reasonSortOrder
        });
      }
      setReasonModalOpen(false);
      await loadCategories();
    } catch { setReasonError('Failed to save reason.'); }
    finally { setReasonSaving(false); }
  };

  const handleDeleteReason = async () => {
    if (!deleteReasonTarget) return;
    setDeletingReason(true);
    try {
      await downtimeReasonApi.delete(deleteReasonTarget.id);
      setDeleteReasonTarget(null);
      await loadCategories();
    } catch { alert('Failed to deactivate reason.'); }
    finally { setDeletingReason(false); }
  };

  const showPlantSelector = (user?.roleTier ?? 99) <= 2;

  return (
    <AdminLayout title="Downtime Reasons">
      <div className={panelStyles.filterBar}>
        {showPlantSelector && plants.length > 0 && (
          <Dropdown
            value={plants.find(p => p.id === selectedPlantId)?.name ?? ''}
            selectedOptions={[selectedPlantId]}
            onOptionSelect={(_, d) => { if (d.optionValue) setSelectedPlantId(d.optionValue); setSelectedCategoryId(null); }}
            style={{ minWidth: 180 }}
          >
            {plants.map(p => <Option key={p.id} value={p.id}>{p.name}</Option>)}
          </Dropdown>
        )}
      </div>

      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={panelStyles.twoPanelLayout}>
          {/* Left: Categories */}
          <div className={panelStyles.leftPanel}>
            <div className={panelStyles.panelHeader}>
              <span className={panelStyles.panelTitle}>Categories</span>
              {canEdit && (
                <Button appearance="subtle" icon={<AddRegular />} size="small" onClick={openAddCat}>Add</Button>
              )}
            </div>
            {categories.length === 0 && (
              <div className={panelStyles.emptyPanel}>No categories defined for this plant.</div>
            )}
            {categories.map(cat => (
              <div
                key={cat.id}
                className={`${panelStyles.listItem} ${cat.id === selectedCategoryId ? panelStyles.listItemSelected : ''} ${!cat.isActive ? panelStyles.listItemInactive : ''}`}
                onClick={() => setSelectedCategoryId(cat.id)}
              >
                <div className={panelStyles.listItemContent}>
                  <span className={panelStyles.listItemName}>{cat.name}</span>
                  <span className={panelStyles.listItemMeta}>{cat.reasons.length} reason{cat.reasons.length !== 1 ? 's' : ''}</span>
                </div>
                {canEdit && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={(e) => { e.stopPropagation(); openEditCat(cat); }} />
                    <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={(e) => { e.stopPropagation(); setDeleteCatTarget(cat); }} />
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Right: Reasons */}
          <div className={panelStyles.rightPanel}>
            <div className={panelStyles.panelHeader}>
              <span className={panelStyles.panelTitle}>
                {selectedCategory ? `Reasons — ${selectedCategory.name}` : 'Reasons'}
              </span>
              {canEdit && selectedCategoryId && (
                <Button appearance="subtle" icon={<AddRegular />} size="small" onClick={openAddReason}>Add</Button>
              )}
            </div>
            {!selectedCategoryId ? (
              <div className={panelStyles.emptyPanel}>Select a category to view reasons.</div>
            ) : reasons.length === 0 ? (
              <div className={panelStyles.emptyPanel}>No reasons in this category.</div>
            ) : (
              <div className={styles.grid} style={{ gridTemplateColumns: 'repeat(2, 1fr)' }}>
                {reasons.map(reason => (
                  <div key={reason.id} className={`${styles.card} ${!reason.isActive ? styles.cardInactive : ''}`}>
                    <div className={styles.cardHeader}>
                      <span className={styles.cardTitle}>{reason.name}</span>
                      {canEdit && (
                        <div className={styles.cardActions}>
                          <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEditReason(reason)} />
                          <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteReasonTarget(reason)} />
                        </div>
                      )}
                    </div>
                    <div className={styles.cardField}>
                      <span className={styles.cardFieldLabel}>Sort Order</span>
                      <span className={styles.cardFieldValue}>{reason.sortOrder}</span>
                    </div>
                    <span className={`${styles.badge} ${reason.isActive ? styles.badgeGreen : styles.badgeGray}`}>
                      {reason.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Category Modal */}
      <AdminModal
        open={catModalOpen}
        title={editingCat ? 'Edit Category' : 'Add Category'}
        onConfirm={handleSaveCat}
        onCancel={() => setCatModalOpen(false)}
        confirmLabel={editingCat ? 'Save' : 'Add'}
        loading={catSaving}
        error={catError}
        confirmDisabled={!catName.trim()}
      >
        <Label>Name</Label>
        <Input value={catName} onChange={(_, d) => setCatName(d.value)} />
        <Label>Sort Order</Label>
        <Input type="number" value={String(catSortOrder)} onChange={(_, d) => setCatSortOrder(Number(d.value) || 0)} />
      </AdminModal>

      {/* Reason Modal */}
      <AdminModal
        open={reasonModalOpen}
        title={editingReason ? 'Edit Reason' : 'Add Reason'}
        onConfirm={handleSaveReason}
        onCancel={() => setReasonModalOpen(false)}
        confirmLabel={editingReason ? 'Save' : 'Add'}
        loading={reasonSaving}
        error={reasonError}
        confirmDisabled={!reasonName.trim()}
      >
        <Label>Name</Label>
        <Input value={reasonName} onChange={(_, d) => setReasonName(d.value)} />
        <Label>Sort Order</Label>
        <Input type="number" value={String(reasonSortOrder)} onChange={(_, d) => setReasonSortOrder(Number(d.value) || 0)} />
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteCatTarget}
        itemName={deleteCatTarget?.name ?? ''}
        onConfirm={handleDeleteCat}
        onCancel={() => setDeleteCatTarget(null)}
        loading={deletingCat}
      />
      <ConfirmDeleteDialog
        open={!!deleteReasonTarget}
        itemName={deleteReasonTarget?.name ?? ''}
        onConfirm={handleDeleteReason}
        onCancel={() => setDeleteReasonTarget(null)}
        loading={deletingReason}
      />
    </AdminLayout>
  );
}
