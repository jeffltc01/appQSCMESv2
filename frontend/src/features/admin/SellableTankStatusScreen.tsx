import { useState, useCallback, useEffect, useRef } from 'react';
import { Button, Select, Spinner } from '@fluentui/react-components';
import {
  SearchRegular,
  CheckmarkCircleFilled,
  DismissCircleFilled,
  SubtractCircleFilled,
} from '@fluentui/react-icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, sellableTankStatusApi } from '../../api/endpoints.ts';
import type { Plant, SellableTankStatus } from '../../types/domain.ts';
import { todayISOString } from '../../utils/dateFormat.ts';
import styles from './SellableTankStatusScreen.module.css';

function GateIcon({ result }: { result: string | null }) {
  if (!result) {
    return (
      <span className={styles.gateIcon}>
        <SubtractCircleFilled className={styles.gateNone} />
      </span>
    );
  }
  const lower = result.toLowerCase();
  const isReject = lower.includes('reject') || lower.includes('fail');
  return (
    <span className={styles.gateIcon}>
      {isReject
        ? <DismissCircleFilled className={styles.gateFail} />
        : <CheckmarkCircleFilled className={styles.gatePass} />}
    </span>
  );
}

function GateCheckLegend() {
  return (
    <div className={styles.legend} data-testid="gate-legend">
      <strong>Gate Check</strong>
      <span className={styles.legendItem}>
        <CheckmarkCircleFilled className={styles.gatePass} />
        Passed
      </span>
      <span className={styles.legendItem}>
        <DismissCircleFilled className={styles.gateFail} />
        Rejected
      </span>
      <span className={styles.legendItem}>
        <SubtractCircleFilled className={styles.gateNone} />
        No Record
      </span>
    </div>
  );
}

export function SellableTankStatusScreen() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;
  const autoSearched = useRef(false);

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(searchParams.get('siteId') ?? user?.defaultSiteId ?? '');
  const [date, setDate] = useState(searchParams.get('date') ?? todayISOString());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<SellableTankStatus[]>([]);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    if (canChangeSite) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [canChangeSite]);

  const handleSearch = useCallback(async (overrideSiteId?: string, overrideDate?: string) => {
    const sid = overrideSiteId ?? siteId;
    const dt = overrideDate ?? date;
    if (!sid || !dt) return;
    setSearchParams({ siteId: sid, date: dt }, { replace: true });
    setLoading(true);
    setError('');
    setData([]);
    setHasSearched(true);
    try {
      const result = await sellableTankStatusApi.getStatus(sid, dt);
      setData(result);
    } catch {
      setError('Failed to load sellable tank status.');
    } finally {
      setLoading(false);
    }
  }, [siteId, date, setSearchParams]);

  useEffect(() => {
    const qsSiteId = searchParams.get('siteId');
    const qsDate = searchParams.get('date');
    if (qsSiteId && qsDate && !autoSearched.current) {
      autoSearched.current = true;
      handleSearch(qsSiteId, qsDate);
    }
  }, [searchParams, handleSearch]);

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
          onClick={() => handleSearch()}
          disabled={loading || !siteId || !date}
        >
          Search
        </Button>

        <GateCheckLegend />
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
        <>
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
                  <td>
                    <button
                      className={styles.serialLink}
                      onClick={() => navigate(`/menu/serial-lookup?serial=${encodeURIComponent(row.serialNumber)}&from=sellable-status`)}
                      data-testid={`serial-link-${row.serialNumber}`}
                    >
                      {row.serialNumber}
                    </button>
                  </td>
                  <td>{row.productNumber}</td>
                  <td>{row.tankSize}</td>
                  <td><GateIcon result={row.rtXrayResult} /></td>
                  <td><GateIcon result={row.spotXrayResult} /></td>
                  <td><GateIcon result={row.hydroResult} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </AdminLayout>
  );
}
