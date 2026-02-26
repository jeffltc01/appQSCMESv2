import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';
import { AdminLayout } from './AdminLayout.tsx';
import { checklistApi, siteApi, workCenterApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { ChecklistTemplate, Plant, WorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

const CHECKLIST_TYPES = ['SafetyPreShift', 'SafetyPeriodic', 'OpsPreShift', 'OpsChangeover'];

function getErrorMessage(err: unknown, fallback: string): string {
  if (typeof err === 'object' && err !== null && 'message' in err) {
    const msg = (err as { message?: unknown }).message;
    if (typeof msg === 'string' && msg.trim()) return msg;
  }
  return fallback;
}

export function ChecklistTemplatesScreen() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const canManage = roleTier <= 4;
  const canCrossSite = roleTier <= 2;

  const [templates, setTemplates] = useState<ChecklistTemplate[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [siteFilter, setSiteFilter] = useState<string>(canCrossSite ? '' : (user?.defaultSiteId ?? ''));
  const [checklistTypeFilter, setChecklistTypeFilter] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const siteIdQuery = canCrossSite ? (siteFilter || undefined) : user?.defaultSiteId;
      const [templatesData, siteData, wcData] = await Promise.all([
        checklistApi.getTemplates(siteIdQuery, checklistTypeFilter || undefined),
        siteApi.getSites(),
        workCenterApi.getWorkCenters(),
      ]);
      setTemplates(templatesData);
      setSites(siteData);
      setWorkCenters(wcData);
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to load checklist templates.'));
    } finally {
      setLoading(false);
    }
  }, [canCrossSite, siteFilter, checklistTypeFilter, user?.defaultSiteId]);

  useEffect(() => {
    void load();
  }, [load]);

  const siteNameById = useMemo(
    () => new Map(sites.map((s) => [s.id, `${s.name} (${s.code})`])),
    [sites],
  );
  const wcNameById = useMemo(
    () => new Map(workCenters.map((w) => [w.id, w.name])),
    [workCenters],
  );

  const openCreate = () => navigate('/menu/checklists/new');
  const openEdit = (template: ChecklistTemplate) => navigate(`/menu/checklists/${template.id}`);

  return (
    <AdminLayout title="Checklist Templates" onAdd={canManage ? openCreate : undefined} addLabel="Add Template">
      {(canCrossSite || checklistTypeFilter) && (
        <div className={styles.filterBar}>
          {canCrossSite && (
            <>
              <label style={{ fontSize: 12, fontWeight: 600 }}>Site</label>
              <Dropdown
                value={siteNameById.get(siteFilter) ?? 'All Sites'}
                selectedOptions={[siteFilter]}
                onOptionSelect={(_, data) => setSiteFilter(data.optionValue ?? '')}
              >
                <Option value="">All Sites</Option>
                {sites.map((site) => (
                  <Option key={site.id} value={site.id} text={`${site.name} (${site.code})`}>
                    {site.name} ({site.code})
                  </Option>
                ))}
              </Dropdown>
            </>
          )}
          <label style={{ fontSize: 12, fontWeight: 600 }}>Type</label>
          <Dropdown
            value={checklistTypeFilter || 'All Types'}
            selectedOptions={[checklistTypeFilter]}
            onOptionSelect={(_, data) => setChecklistTypeFilter(data.optionValue ?? '')}
          >
            <Option value="">All Types</Option>
            {CHECKLIST_TYPES.map((type) => (
              <Option key={type} value={type}>{type}</Option>
            ))}
          </Dropdown>
        </div>
      )}
      {error && <div style={{ color: '#c92a2a', marginBottom: 10 }}>{error}</div>}

      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {templates.length === 0 && <div className={styles.emptyState}>No checklist templates found.</div>}
          {templates.map((template) => (
            <div key={template.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{template.title}</span>
                {canManage && (
                  <div className={styles.cardActions}>
                    <Button
                      appearance="subtle"
                      icon={<EditRegular />}
                      size="small"
                      aria-label={`Edit ${template.title}`}
                      onClick={() => openEdit(template)}
                    />
                  </div>
                )}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Code</span>
                <span className={styles.cardFieldValue}>{template.templateCode} v{template.versionNo}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{template.checklistType}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Scope</span>
                <span className={styles.cardFieldValue}>{template.scopeLevel}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Site</span>
                <span className={styles.cardFieldValue}>{template.siteId ? siteNameById.get(template.siteId) : 'Global'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{template.workCenterId ? wcNameById.get(template.workCenterId) : 'Default'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Line</span>
                <span className={styles.cardFieldValue}>{template.productionLineId ?? 'Any'}</span>
              </div>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${template.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                  {template.isActive ? 'Active' : 'Inactive'}
                </span>
                <span className={`${styles.badge} ${styles.badgeBlue}`}>{template.responseMode}</span>
                {template.requireFailNote && <span className={`${styles.badge} ${styles.badgeBlue}`}>Fail Note Required</span>}
                {template.isSafetyProfile && <span className={`${styles.badge} ${styles.badgeBlue}`}>Safety Profile</span>}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Items</span>
                <span className={styles.cardFieldValue}>{template.items.length}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </AdminLayout>
  );
}
