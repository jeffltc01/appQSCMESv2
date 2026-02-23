import { useState, useEffect, useCallback } from 'react';
import { Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { coverageApi } from '../../api/endpoints.ts';
import type { CoverageSummary, CoverageLayerSummary } from '../../types/api.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './TestCoverageScreen.module.css';

type Layer = 'backend' | 'frontend';

function rateClass(rate: number): string {
  if (rate >= 80) return styles.metricValueGood;
  if (rate >= 50) return styles.metricValueWarn;
  return styles.metricValueLow;
}

function SummaryCard({ title, data }: { title: string; data: CoverageLayerSummary }) {
  if (data.error) {
    return (
      <div className={styles.summaryCard}>
        <h3>{title}</h3>
        <p className={styles.errorState}>{data.error}</p>
      </div>
    );
  }

  return (
    <div className={styles.summaryCard}>
      <h3>{title}</h3>
      <div className={styles.metricsGrid}>
        <div className={styles.metric}>
          <span className={styles.metricLabel}>Line Coverage</span>
          <span className={`${styles.metricValue} ${rateClass(data.lineRate)}`}>
            {data.lineRate}%
          </span>
          <span className={styles.metricSub}>
            {data.linesCovered.toLocaleString()} / {data.linesValid.toLocaleString()} lines
          </span>
        </div>
        <div className={styles.metric}>
          <span className={styles.metricLabel}>Branch Coverage</span>
          <span className={`${styles.metricValue} ${rateClass(data.branchRate)}`}>
            {data.branchRate}%
          </span>
          <span className={styles.metricSub}>
            {data.branchesCovered.toLocaleString()} / {data.branchesValid.toLocaleString()} branches
          </span>
        </div>
      </div>
    </div>
  );
}

export function TestCoverageScreen() {
  const [summary, setSummary] = useState<CoverageSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<Layer>('backend');
  const [reportHtml, setReportHtml] = useState<string | null>(null);
  const [reportLoading, setReportLoading] = useState(false);

  useEffect(() => {
    coverageApi.getSummary()
      .then(setSummary)
      .catch((err) => setError(err?.message ?? 'Failed to load coverage summary'))
      .finally(() => setLoading(false));
  }, []);

  const loadReport = useCallback(async (layer: Layer) => {
    setReportLoading(true);
    setReportHtml(null);
    try {
      const html = await coverageApi.getReportHtml(layer);
      setReportHtml(html);
    } catch {
      setReportHtml('<html><body><p style="padding:24px;color:#868e96;">Failed to load the coverage report.</p></body></html>');
    } finally {
      setReportLoading(false);
    }
  }, []);

  useEffect(() => {
    if (summary && !summary.backend.error && !summary.frontend.error) {
      loadReport(activeTab);
    }
  }, [activeTab, summary, loadReport]);

  const handleTabClick = (layer: Layer) => {
    if (layer !== activeTab) {
      setActiveTab(layer);
    }
  };

  return (
    <AdminLayout title="Test Coverage">
      {loading && (
        <div className={styles.loadingState}><Spinner size="medium" /></div>
      )}

      {!loading && error && (
        <div className={styles.errorState}>{error}</div>
      )}

      {!loading && summary && (
        <div className={styles.wrapper}>
          <div className={styles.summaryRow}>
            <SummaryCard title="Backend (.NET)" data={summary.backend} />
            <SummaryCard title="Frontend (React)" data={summary.frontend} />
          </div>

          {summary.generatedAt && (
            <div className={styles.timestamp}>
              Last updated: {formatDateTime(summary.generatedAt)}
            </div>
          )}

          <div className={styles.tabBar}>
            <button
              className={`${styles.tab} ${activeTab === 'backend' ? styles.tabActive : ''}`}
              onClick={() => handleTabClick('backend')}
            >
              Backend Report
            </button>
            <button
              className={`${styles.tab} ${activeTab === 'frontend' ? styles.tabActive : ''}`}
              onClick={() => handleTabClick('frontend')}
            >
              Frontend Report
            </button>
          </div>

          <div className={styles.iframeContainer}>
            {reportLoading && (
              <div className={styles.loadingState}><Spinner size="medium" /></div>
            )}
            {!reportLoading && reportHtml && (
              <iframe
                srcDoc={reportHtml}
                title={`${activeTab} coverage report`}
                sandbox="allow-same-origin"
              />
            )}
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
