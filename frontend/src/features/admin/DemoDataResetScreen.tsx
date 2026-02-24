import { useState } from 'react';
import { Button, Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { demoDataAdminApi } from '../../api/endpoints.ts';
import type { DemoDataRefreshDatesResult, DemoDataResetSeedResult } from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './DemoDataResetScreen.module.css';

const RESET_CONFIRM_PHRASE = 'RESET DEMO DATA';

export function DemoDataResetScreen() {
  const [confirmText, setConfirmText] = useState('');
  const [isResetting, setIsResetting] = useState(false);
  const [isRefreshingDates, setIsRefreshingDates] = useState(false);
  const [error, setError] = useState('');
  const [resetResult, setResetResult] = useState<DemoDataResetSeedResult | null>(null);
  const [refreshResult, setRefreshResult] = useState<DemoDataRefreshDatesResult | null>(null);

  const handleResetSeed = async () => {
    setError('');
    setResetResult(null);
    setIsResetting(true);
    try {
      const result = await demoDataAdminApi.resetSeed();
      setResetResult(result);
      setConfirmText('');
    } catch (err) {
      const message = typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message?: unknown }).message ?? '')
        : '';
      setError(message || 'Reset + seed failed.');
    } finally {
      setIsResetting(false);
    }
  };

  const handleRefreshDates = async () => {
    setError('');
    setRefreshResult(null);
    setIsRefreshingDates(true);
    try {
      const result = await demoDataAdminApi.refreshDates();
      setRefreshResult(result);
    } catch (err) {
      const message = typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message?: unknown }).message ?? '')
        : '';
      setError(message || 'Date refresh failed.');
    } finally {
      setIsRefreshingDates(false);
    }
  };

  return (
    <AdminLayout title="Demo Data Tools">
      <div className={styles.wrapper}>
        <section className={styles.card}>
          <h3>Button 1: Reset + Seed Demo Data</h3>
          <p className={styles.warning}>
            Destructive action. This clears application data and inserts deterministic demo baseline data.
          </p>
          <p className={styles.confirmLabel}>
            Type <strong>{RESET_CONFIRM_PHRASE}</strong> to enable this button:
          </p>
          <input
            className={styles.confirmInput}
            value={confirmText}
            onChange={(e) => setConfirmText(e.target.value)}
            placeholder={RESET_CONFIRM_PHRASE}
          />
          <Button
            appearance="primary"
            disabled={confirmText !== RESET_CONFIRM_PHRASE || isResetting || isRefreshingDates}
            onClick={handleResetSeed}
          >
            {isResetting ? <Spinner size="tiny" /> : 'Run Reset + Seed'}
          </Button>

          {resetResult && (
            <div className={styles.result}>
              <div className={styles.resultTitle}>Reset + Seed Completed</div>
              <div className={styles.resultMeta}>Executed: {formatDateTime(resetResult.executedAtUtc)}</div>
              <div className={styles.resultColumns}>
                <div>
                  <h4>Deleted</h4>
                  <ul>
                    {resetResult.deleted.map((row) => (
                      <li key={`deleted-${row.table}`}>{row.table}: {row.count}</li>
                    ))}
                  </ul>
                </div>
                <div>
                  <h4>Inserted</h4>
                  <ul>
                    {resetResult.inserted.map((row) => (
                      <li key={`inserted-${row.table}`}>{row.table}: {row.count}</li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          )}
        </section>

        <section className={styles.card}>
          <h3>Button 2: Refresh Dates To Current Windows</h3>
          <p className={styles.warning}>
            Re-times demo timestamps so day/week widgets and trend windows look current.
          </p>
          <Button
            appearance="primary"
            disabled={isResetting || isRefreshingDates}
            onClick={handleRefreshDates}
          >
            {isRefreshingDates ? <Spinner size="tiny" /> : 'Run Date Refresh'}
          </Button>

          {refreshResult && (
            <div className={styles.result}>
              <div className={styles.resultTitle}>Date Refresh Completed</div>
              <div className={styles.resultMeta}>Executed: {formatDateTime(refreshResult.executedAtUtc)}</div>
              <div className={styles.resultMeta}>Applied Delta (hours): {refreshResult.appliedDeltaHours}</div>
              <h4>Updated</h4>
              <ul>
                {refreshResult.updated.map((row) => (
                  <li key={`updated-${row.table}`}>{row.table}: {row.count}</li>
                ))}
              </ul>
            </div>
          )}
        </section>

        {error && <div className={styles.error}>{error}</div>}
      </div>
    </AdminLayout>
  );
}
