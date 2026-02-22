import { useState, useCallback, useEffect } from 'react';
import { Button, Select, Spinner } from '@fluentui/react-components';
import { SearchRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, sellableTankStatusApi } from '../../api/endpoints.ts';
import type { Plant, SellableTankStatus } from '../../types/domain.ts';
import { todayISOString } from '../../utils/dateFormat.ts';
import styles from './SellableTankStatusScreen.module.css';

export function SellableTankStatusScreen() {
  const { user } = useAuth();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [date, setDate] = useState(todayISOString());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<SellableTankStatus[]>([]);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    if (canChangeSite) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [canChangeSite]);

  const handleSearch = useCallback(async () => {
    if (!siteId || !date) return;
    setLoading(true);
    setError('');
    setData([]);
    setHasSearched(true);
    try {
      const result = await sellableTankStatusApi.getStatus(siteId, date);
      setData(result);
    } catch {
      setError('Failed to load sellable tank status.');
    } finally {
      setLoading(false);
    }
  }, [siteId, date]);

  function renderResult(val: string | null) {
    if (!val) return <span className={styles.dash}>—</span>;
    if (val.toLowerCase() === 'accept') return <span className={styles.accept}>Accept</span>;
    if (val.toLowerCase() === 'reject') return <span className={styles.reject}>Reject</span>;
    return <span>{val}</span>;
  }

  return (
    <AdminLayout title="Sellable Tank Daily Status">
      <div className={styles.controls}>
        {canChangeSite && (
          <div className={styles.fieldGroup}>
            <label className={styles.fieldLabel}>Site</label>
            <Select value={siteId} onChange={(_e, d) => setSiteId(d.value)}>
              {sites.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
              ))}
              {sites.length === 0 && siteId && (
                <option value={siteId}>{user?.plantName ?? siteId}</option>
              )}
            </Select>
          </div>
        )}

        <div className={styles.fieldGroup}>
          <label className={styles.fieldLabel}>Date</label>
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>

        <Button
          appearance="primary"
          icon={<SearchRegular />}
          onClick={handleSearch}
          disabled={loading || !siteId || !date}
        >
          Search
        </Button>
      </div>

      {loading && (
        <div className={styles.emptyState}>
          <Spinner size="medium" label="Loading..." />
        </div>
      )}

      {error && <div className={styles.errorMsg}>{error}</div>}

      {!loading && hasSearched && data.length === 0 && !error && (
        <div className={styles.emptyState}>No sellable tanks found for this date and site.</div>
      )}

      {data.length > 0 && (
        <table className={styles.statusTable}>
          <thead>
            <tr>
              <th>Serial Number</th>
              <th>Product Number</th>
              <th>Tank Size</th>
              <th>RT X-ray</th>
              <th>Spot X-ray</th>
              <th>Hydro</th>
            </tr>
          </thead>
          <tbody>
            {data.map((row) => (
              <tr key={row.serialNumber}>
                <td>{row.serialNumber}</td>
                <td>{row.productNumber}</td>
                <td>{row.tankSize}</td>
                <td>{renderResult(row.rtXrayResult)}</td>
                <td>{renderResult(row.spotXrayResult)}</td>
                <td>{renderResult(row.hydroResult)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </AdminLayout>
  );
}
