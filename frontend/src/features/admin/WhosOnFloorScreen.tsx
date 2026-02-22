import { useState, useEffect, useCallback } from 'react';
import { Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { activeSessionApi } from '../../api/endpoints.ts';
import type { ActiveSession } from '../../types/domain.ts';
import { formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './CardList.module.css';
import floorStyles from './WhosOnFloor.module.css';

export function WhosOnFloorScreen() {
  const { user } = useAuth();
  const [sessions, setSessions] = useState<ActiveSession[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!user?.defaultSiteId) return;
    setLoading(true);
    try { setSessions(await activeSessionApi.getBySite(user.defaultSiteId)); }
    catch { /* ignore */ }
    finally { setLoading(false); }
  }, [user?.defaultSiteId]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    const interval = setInterval(load, 30000);
    return () => clearInterval(interval);
  }, [load]);

  const grouped = sessions.reduce<Record<string, ActiveSession[]>>((acc, s) => {
    const key = s.productionLineName;
    if (!acc[key]) acc[key] = [];
    acc[key].push(s);
    return acc;
  }, {});


  return (
    <AdminLayout title="Who's On the Floor">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : sessions.length === 0 ? (
        <div className={styles.emptyState}>No active sessions at this site.</div>
      ) : (
        Object.entries(grouped).map(([lineName, lineSessions]) => (
          <div key={lineName} className={floorStyles.lineGroup}>
            <h3 className={floorStyles.lineTitle}>{lineName}</h3>
            <div className={styles.grid}>
              {lineSessions.map(s => (
                <div key={s.id} className={`${styles.card} ${s.isStale ? styles.cardInactive : ''}`}>
                  <div className={styles.cardHeader}>
                    <span className={styles.cardTitle}>{s.workCenterName}</span>
                    {s.isStale && (
                      <span className={`${styles.badge} ${styles.badgeGray}`}>Stale</span>
                    )}
                  </div>
                  <div className={styles.cardField}>
                    <span className={styles.cardFieldLabel}>Operator</span>
                    <span className={styles.cardFieldValue}>{s.userDisplayName}</span>
                  </div>
                  <div className={styles.cardField}>
                    <span className={styles.cardFieldLabel}>Emp #</span>
                    <span className={styles.cardFieldValue}>{s.employeeNumber}</span>
                  </div>
                  <div className={styles.cardField}>
                    <span className={styles.cardFieldLabel}>Login</span>
                    <span className={styles.cardFieldValue}>{formatTimeOnly(s.loginDateTime)}</span>
                  </div>
                  {s.isStale && (
                    <div className={styles.cardField}>
                      <span className={styles.cardFieldLabel}>Last Seen</span>
                      <span className={styles.cardFieldValue}>{formatTimeOnly(s.lastHeartbeatDateTime)}</span>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </AdminLayout>
  );
}
