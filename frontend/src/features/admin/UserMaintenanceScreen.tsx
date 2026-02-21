import { useState, useEffect, useCallback, useMemo } from 'react';
import { Button, Input, Label, Dropdown, Option, Checkbox, Spinner, SearchBox } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminUserApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminUser, RoleOption, Plant } from '../../types/domain.ts';
import { UserType } from '../../types/domain.ts';
import styles from './CardList.module.css';

const userTypeOptions = [
  { value: UserType.Standard, label: 'Standard' },
  { value: UserType.AuthorizedInspector, label: 'Authorized Inspector (AI)' },
];

const isAI = (ut: number) => ut === UserType.AuthorizedInspector;
const stripAIPrefix = (empNo: string) => empNo.replace(/^AI/i, '');

export function UserMaintenanceScreen() {
  const { user: authUser } = useAuth();
  const isSiteScoped = (authUser?.roleTier ?? 99) > 2;

  const [items, setItems] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<AdminUser | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [search, setSearch] = useState('');

  const [employeeNumber, setEmployeeNumber] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [roleName, setRoleName] = useState('');
  const [roleTier, setRoleTier] = useState(6);
  const [defaultSiteId, setDefaultSiteId] = useState('');
  const [isCertifiedWelder, setIsCertifiedWelder] = useState(false);
  const [requirePinForLogin, setRequirePinForLogin] = useState(false);
  const [pin, setPin] = useState('');
  const [userType, setUserType] = useState<number>(UserType.Standard);
  const [isActive, setIsActive] = useState(true);

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
    setRoleName('Operator'); setRoleTier(6);
    setDefaultSiteId(isSiteScoped ? (authUser?.defaultSiteId ?? '') : '');
    setIsCertifiedWelder(false); setRequirePinForLogin(false); setPin('');
    setUserType(UserType.Standard); setIsActive(true); setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminUser) => {
    setEditing(item);
    const rawEmpNo = isAI(item.userType) ? stripAIPrefix(item.employeeNumber) : item.employeeNumber;
    setEmployeeNumber(rawEmpNo); setFirstName(item.firstName); setLastName(item.lastName);
    setDisplayName(item.displayName); setRoleName(item.roleName); setRoleTier(item.roleTier);
    setDefaultSiteId(item.defaultSiteId); setIsCertifiedWelder(item.isCertifiedWelder);
    setRequirePinForLogin(item.requirePinForLogin); setPin('');
    setUserType(item.userType); setIsActive(item.isActive); setError(''); setModalOpen(true);
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
          employeeNumber, firstName, lastName, displayName, roleTier, roleName,
          defaultSiteId, isCertifiedWelder, requirePinForLogin,
          pin: pin || undefined, userType, isActive,
        });
        setItems(prev => prev.map(u => u.id === updated.id ? updated : u));
      } else {
        const created = await adminUserApi.create({
          employeeNumber, firstName, lastName, displayName, roleTier, roleName,
          defaultSiteId, isCertifiedWelder, requirePinForLogin,
          pin: pin || undefined, userType,
        });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch (err: unknown) {
      const msg = (err as { message?: string })?.message;
      setError(msg ?? 'Failed to save user.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const updated = await adminUserApi.remove(deleteTarget.id);
      setItems(prev => prev.map(u => u.id === updated.id ? updated : u));
      setDeleteTarget(null);
    } catch { alert('Failed to deactivate user.'); }
    finally { setDeleting(false); }
  };

  const handleRequirePinChange = (_: unknown, d: { checked: boolean | 'mixed' }) => {
    const checked = !!d.checked;
    setRequirePinForLogin(checked);
    if (!checked) setPin('');
  };

  const getPinPlaceholder = () => {
    if (!editing) return '4-20 digit PIN';
    return editing.hasPin ? 'Leave blank to keep current' : '4-20 digit PIN (required)';
  };

  const visibleRoles = useMemo(() => {
    if (!isSiteScoped) return roles;
    return roles.filter(r => r.tier >= 4);
  }, [roles, isSiteScoped]);

  const visibleSites = useMemo(() => {
    if (!isSiteScoped) return sites;
    return sites.filter(s => s.id === authUser?.defaultSiteId);
  }, [sites, isSiteScoped, authUser?.defaultSiteId]);

  const filteredItems = items.filter(item => {
    if (isSiteScoped && item.defaultSiteId !== authUser?.defaultSiteId) return false;
    if (!search) return true;
    const q = search.toLowerCase();
    return item.displayName.toLowerCase().includes(q)
      || item.firstName.toLowerCase().includes(q)
      || item.lastName.toLowerCase().includes(q)
      || item.employeeNumber.toLowerCase().includes(q);
  });

  const aiPrefix = isAI(userType) ? (
    <span style={{ fontWeight: 600, color: '#1b6ec2', padding: '0 2px' }}>AI</span>
  ) : undefined;

  return (
    <AdminLayout title="User Maintenance" onAdd={openAdd} addLabel="Add User">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
        <div className={styles.filterBar}>
          <SearchBox
            placeholder="Search by name or employee #..."
            value={search}
            onChange={(_, d) => setSearch(d.value)}
          />
        </div>
        <div className={styles.grid}>
          {filteredItems.length === 0 && <div className={styles.emptyState}>No users found.</div>}
          {filteredItems.map(item => (
            <div key={item.id} className={`${styles.card} ${!item.isActive ? styles.cardInactive : ''}`}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.displayName}</span>
                {!(isSiteScoped && item.roleTier <= 2) && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                    <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                  </div>
                )}
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
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${item.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                  {item.isActive ? 'Active' : 'Inactive'}
                </span>
                {item.isCertifiedWelder && (
                  <span className={`${styles.badge} ${styles.badgeBlue}`}>Welder</span>
                )}
                {item.userType === UserType.AuthorizedInspector && (
                  <span className={`${styles.badge} ${styles.badgeGreen}`}>AI</span>
                )}
              </div>
            </div>
          ))}
        </div>
        </>
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
        wide
      >
        <div className={styles.formGrid}>
          <div className={styles.formColumn}>
            <Label>Employee Number</Label>
            <Input
              value={employeeNumber}
              onChange={(_, d) => setEmployeeNumber(d.value)}
              contentBefore={aiPrefix}
            />
            <Label>First Name</Label>
            <Input value={firstName} onChange={(_, d) => setFirstName(d.value)} />
            <Label>Last Name</Label>
            <Input value={lastName} onChange={(_, d) => setLastName(d.value)} />
            <Label>Display Name</Label>
            <Input value={displayName} onChange={(_, d) => setDisplayName(d.value)} />
            <Label>User Type</Label>
            <Dropdown
              value={userTypeOptions.find(o => o.value === userType)?.label ?? 'Standard'}
              selectedOptions={[String(userType)]}
              onOptionSelect={(_, d) => { if (d.optionValue) setUserType(Number(d.optionValue)); }}
            >
              {userTypeOptions.map(o => (
                <Option key={o.value} value={String(o.value)} text={o.label}>{o.label}</Option>
              ))}
            </Dropdown>
          </div>
          <div className={styles.formColumn}>
            <Label>Role</Label>
            <Dropdown value={roleName} selectedOptions={[roleName]} onOptionSelect={handleRoleChange}>
              {visibleRoles.map(r => <Option key={r.name} value={r.name} text={`${r.name} (${r.tier})`}>{r.name} ({r.tier})</Option>)}
            </Dropdown>
            <Label>Default Site</Label>
            <Dropdown
              value={visibleSites.find(s => s.id === defaultSiteId)?.name ?? ''}
              selectedOptions={[defaultSiteId]}
              onOptionSelect={(_, d) => { if (d.optionValue) setDefaultSiteId(d.optionValue); }}
              disabled={isSiteScoped}
            >
              {visibleSites.map(s => <Option key={s.id} value={s.id} text={`${s.name} (${s.code})`}>{s.name} ({s.code})</Option>)}
            </Dropdown>
            <Checkbox label="Certified Welder" checked={isCertifiedWelder} onChange={(_, d) => setIsCertifiedWelder(!!d.checked)} />
            <Checkbox label="Require PIN for Login" checked={requirePinForLogin} onChange={handleRequirePinChange} />
            {requirePinForLogin && (
              <>
                <Input
                  type="password"
                  inputMode="numeric"
                  maxLength={20}
                  value={pin}
                  onChange={(_, d) => setPin(d.value)}
                  placeholder={getPinPlaceholder()}
                />
                {editing && (
                  <span style={{ fontSize: 12, color: editing.hasPin ? '#2b8a3e' : '#e67700' }}>
                    {editing.hasPin ? 'PIN is set' : 'No PIN set'}
                  </span>
                )}
              </>
            )}
            {editing && (
              <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
            )}
          </div>
        </div>
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget?.displayName ?? ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
        loading={deleting}
      />
    </AdminLayout>
  );
}
