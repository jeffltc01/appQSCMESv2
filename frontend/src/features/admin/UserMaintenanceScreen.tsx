import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Checkbox, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminUserApi, siteApi } from '../../api/endpoints.ts';
import type { AdminUser, RoleOption, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function UserMaintenanceScreen() {
  const [items, setItems] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [employeeNumber, setEmployeeNumber] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [roleName, setRoleName] = useState('');
  const [roleTier, setRoleTier] = useState(6);
  const [defaultSiteId, setDefaultSiteId] = useState('');
  const [isCertifiedWelder, setIsCertifiedWelder] = useState(false);
  const [requirePinForLogin, setRequirePinForLogin] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [users, roleList, siteList] = await Promise.all([
        adminUserApi.getAll(), adminUserApi.getRoles(), siteApi.getSites()
      ]);
      setItems(users); setRoles(roleList); setSites(siteList);
    } catch { setError('Failed to load users.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setEmployeeNumber(''); setFirstName(''); setLastName(''); setDisplayName('');
    setRoleName('Operator'); setRoleTier(6); setDefaultSiteId('');
    setIsCertifiedWelder(false); setRequirePinForLogin(false);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminUser) => {
    setEditing(item);
    setEmployeeNumber(item.employeeNumber); setFirstName(item.firstName); setLastName(item.lastName);
    setDisplayName(item.displayName); setRoleName(item.roleName); setRoleTier(item.roleTier);
    setDefaultSiteId(item.defaultSiteId); setIsCertifiedWelder(item.isCertifiedWelder);
    setRequirePinForLogin(item.requirePinForLogin);
    setError(''); setModalOpen(true);
  };

  const handleRoleChange = (_: unknown, data: { optionValue?: string }) => {
    const role = roles.find(r => r.name === data.optionValue);
    if (role) { setRoleName(role.name); setRoleTier(role.tier); }
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminUserApi.update(editing.id, {
          firstName, lastName, displayName, roleTier, roleName,
          defaultSiteId, isCertifiedWelder, requirePinForLogin,
        });
        setItems(prev => prev.map(u => u.id === updated.id ? updated : u));
      } else {
        const created = await adminUserApi.create({
          employeeNumber, firstName, lastName, displayName, roleTier, roleName,
          defaultSiteId, isCertifiedWelder, requirePinForLogin,
        });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save user.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this user?')) return;
    try { await adminUserApi.remove(id); setItems(prev => prev.filter(u => u.id !== id)); }
    catch { alert('Failed to delete user.'); }
  };

  return (
    <AdminLayout title="User Maintenance" onAdd={openAdd} addLabel="Add User">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No users found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.displayName}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Emp #</span>
                <span className={styles.cardFieldValue}>{item.employeeNumber}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Role</span>
                <span className={styles.cardFieldValue}>{item.roleName} ({item.roleTier})</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Site</span>
                <span className={styles.cardFieldValue}>{item.defaultSiteName}</span>
              </div>
              {item.isCertifiedWelder && (
                <span className={`${styles.badge} ${styles.badgeBlue}`}>Welder</span>
              )}
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit User' : 'Add User'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!firstName || !lastName || !displayName || !defaultSiteId}
      >
        {!editing && (
          <><Label>Employee Number</Label>
          <Input value={employeeNumber} onChange={(_, d) => setEmployeeNumber(d.value)} /></>
        )}
        <Label>First Name</Label>
        <Input value={firstName} onChange={(_, d) => setFirstName(d.value)} />
        <Label>Last Name</Label>
        <Input value={lastName} onChange={(_, d) => setLastName(d.value)} />
        <Label>Display Name</Label>
        <Input value={displayName} onChange={(_, d) => setDisplayName(d.value)} />
        <Label>Role</Label>
        <Dropdown value={roleName} selectedOptions={[roleName]} onOptionSelect={handleRoleChange}>
          {roles.map(r => <Option key={r.name} value={r.name} text={`${r.name} (${r.tier})`}>{r.name} ({r.tier})</Option>)}
        </Dropdown>
        <Label>Default Site</Label>
        <Dropdown
          value={sites.find(s => s.id === defaultSiteId)?.name ?? ''}
          selectedOptions={[defaultSiteId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setDefaultSiteId(d.optionValue); }}
        >
          {sites.map(s => <Option key={s.id} value={s.id} text={`${s.name} (${s.code})`}>{s.name} ({s.code})</Option>)}
        </Dropdown>
        <Checkbox label="Certified Welder" checked={isCertifiedWelder} onChange={(_, d) => setIsCertifiedWelder(!!d.checked)} />
        <Checkbox label="Require PIN for Login" checked={requirePinForLogin} onChange={(_, d) => setRequirePinForLogin(!!d.checked)} />
      </AdminModal>
    </AdminLayout>
  );
}
