import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { checklistApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type {
  ChecklistFilterOption,
  ChecklistQuestionResponses,
  ChecklistQuestionSummary,
  ChecklistReviewSummary,
  Plant,
} from '../../types/domain.ts';
import styles from './ChecklistResponseReviewScreen.module.css';

const CHECKLIST_TYPES = ['SafetyPreShift', 'SafetyPeriodic', 'OpsPreShift', 'OpsChangeover'];

function toUtcRange(fromDate: string, toDate: string): { fromUtc: string; toUtc: string } {
  return {
    fromUtc: `${fromDate}T00:00:00.000Z`,
    toUtc: `${toDate}T23:59:59.999Z`,
  };
}

function toDateInputValue(value: Date): string {
  return value.toISOString().slice(0, 10);
}

function bucketPercent(count: number, total: number): number {
  if (!total) return 0;
  return Math.round((count / total) * 100);
}

function getDistributionChipClass(label: string): string {
  const normalized = label.trim().toLowerCase();
  if (normalized === 'pass') return styles.distributionChipPass;
  if (normalized === 'fail') return styles.distributionChipFail;
  if (normalized === 'n/a') return styles.distributionChipNa;
  return styles.distributionChipOther;
}

function getSummarySegmentClass(label: string, index: number): string {
  const normalized = label.trim().toLowerCase();
  if (normalized.includes('pass')) return styles.passBar;
  if (normalized.includes('fail')) return styles.failBar;
  if (normalized.includes('n/a')) return styles.naBar;
  const palette = [
    styles.otherBar,
    styles.altBar1,
    styles.altBar2,
    styles.altBar3,
    styles.altBar4,
  ];
  return palette[index % palette.length];
}

export function ChecklistResponseReviewScreen() {
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const canCrossSite = roleTier <= 2;

  const today = useMemo(() => new Date(), []);
  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [fromDate, setFromDate] = useState(toDateInputValue(new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)));
  const [toDate, setToDate] = useState(toDateInputValue(today));
  const [selectedChecklistType, setSelectedChecklistType] = useState('');
  const [chipChecklistType, setChipChecklistType] = useState('');

  const [summary, setSummary] = useState<ChecklistReviewSummary | null>(null);
  const [selectedQuestion, setSelectedQuestion] = useState<ChecklistQuestionSummary | null>(null);
  const [questionResponses, setQuestionResponses] = useState<ChecklistQuestionResponses | null>(null);

  const [loadingSummary, setLoadingSummary] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [error, setError] = useState('');

  const effectiveChecklistType = selectedChecklistType || chipChecklistType;
  const checklistFilterOptions = useMemo<ChecklistFilterOption[]>(() => {
    if (!summary) return [];
    if (summary.checklistFiltersFound && summary.checklistFiltersFound.length > 0) {
      return summary.checklistFiltersFound;
    }
    return summary.checklistTypesFound.map((type) => ({ checklistType: type, checklistName: type }));
  }, [summary]);

  const siteLabelById = useMemo(
    () => new Map(sites.map((s) => [s.id, `${s.name} (${s.code})`])),
    [sites],
  );

  const fetchQuestionResponses = useCallback(async (
    question: ChecklistQuestionSummary,
    filters: { siteId: string; fromDate: string; toDate: string; checklistType?: string },
  ) => {
    const { fromUtc, toUtc } = toUtcRange(filters.fromDate, filters.toDate);
    setLoadingDetail(true);
    try {
      const detail = await checklistApi.getQuestionResponses({
        siteId: filters.siteId,
        fromUtc,
        toUtc,
        checklistTemplateItemId: question.checklistTemplateItemId,
        checklistType: filters.checklistType,
      });
      setQuestionResponses(detail);
    } catch (err) {
      const message = typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message?: unknown }).message ?? '')
        : 'Failed to load question responses.';
      setError(message || 'Failed to load question responses.');
      setQuestionResponses(null);
    } finally {
      setLoadingDetail(false);
    }
  }, []);

  const fetchSummary = useCallback(async (filters: {
    siteId: string;
    fromDate: string;
    toDate: string;
    checklistType?: string;
  }) => {
    const { fromUtc, toUtc } = toUtcRange(filters.fromDate, filters.toDate);
    setLoadingSummary(true);
    setError('');
    try {
      const data = await checklistApi.getReviewSummary({
        siteId: filters.siteId,
        fromUtc,
        toUtc,
        checklistType: filters.checklistType,
      });
      setSummary(data);
      const initialQuestion = data.questions[0] ?? null;
      setSelectedQuestion(initialQuestion);
      if (initialQuestion) {
        await fetchQuestionResponses(initialQuestion, filters);
      } else {
        setQuestionResponses(null);
      }
    } catch (err) {
      const message = typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message?: unknown }).message ?? '')
        : 'Failed to load checklist review summary.';
      setError(message || 'Failed to load checklist review summary.');
      setSummary(null);
      setSelectedQuestion(null);
      setQuestionResponses(null);
    } finally {
      setLoadingSummary(false);
    }
  }, [fetchQuestionResponses]);

  useEffect(() => {
    let isCancelled = false;
    async function loadInitial() {
      try {
        const siteData = await siteApi.getSites();
        if (isCancelled) return;
        setSites(siteData);
      } catch {
        if (!isCancelled) {
          setError('Failed to load plants.');
        }
      }
    }
    void loadInitial();
    return () => {
      isCancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!siteId && user?.defaultSiteId) {
      setSiteId(user.defaultSiteId);
    }
  }, [siteId, user?.defaultSiteId]);

  useEffect(() => {
    if (!siteId) return;
    void fetchSummary({
      siteId,
      fromDate,
      toDate,
      checklistType: effectiveChecklistType || undefined,
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleApplyFilters = async () => {
    if (!siteId) {
      setError('Plant is required.');
      return;
    }
    if (!fromDate || !toDate) {
      setError('Date range is required.');
      return;
    }
    if (fromDate > toDate) {
      setError('From date cannot be after To date.');
      return;
    }
    setChipChecklistType('');
    await fetchSummary({
      siteId,
      fromDate,
      toDate,
      checklistType: selectedChecklistType || undefined,
    });
  };

  const handleChipClick = async (checklistType: string) => {
    setChipChecklistType(checklistType);
    await fetchSummary({
      siteId,
      fromDate,
      toDate,
      checklistType,
    });
  };

  const handleSelectQuestion = async (question: ChecklistQuestionSummary) => {
    setSelectedQuestion(question);
    await fetchQuestionResponses(question, {
      siteId,
      fromDate,
      toDate,
      checklistType: effectiveChecklistType || undefined,
    });
  };

  return (
    <AdminLayout title="Checklist Response Review">
      <section className={styles.filterPanel}>
        <div className={styles.summaryContext}>
          <strong>Checklist Response Review</strong>
          {summary && (
            <span>
              Plant: {siteLabelById.get(siteId) ?? 'Unknown'} | Date Range: {fromDate} to {toDate} | Submissions: {summary.totalEntries}
            </span>
          )}
        </div>
        <div className={styles.filterRow}>
          <label className={styles.filterLabel}>
            Plant
            <Dropdown
              value={siteLabelById.get(siteId) ?? 'Select plant'}
              selectedOptions={[siteId]}
              onOptionSelect={(_, data) => setSiteId(data.optionValue ?? '')}
              disabled={!canCrossSite}
            >
              {sites.map((site) => (
                <Option key={site.id} value={site.id} text={`${site.name} (${site.code})`}>
                  {site.name} ({site.code})
                </Option>
              ))}
            </Dropdown>
          </label>
          <label className={styles.filterLabel}>
            From
            <input className={styles.dateInput} type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </label>
          <label className={styles.filterLabel}>
            To
            <input className={styles.dateInput} type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </label>
          <label className={styles.filterLabel}>
            Checklist (optional)
            <Dropdown
              value={selectedChecklistType || 'All checklists'}
              selectedOptions={[selectedChecklistType]}
              onOptionSelect={(_, data) => setSelectedChecklistType(data.optionValue ?? '')}
            >
              <Option value="">All checklists</Option>
              {CHECKLIST_TYPES.map((type) => (
                <Option key={type} value={type}>{type}</Option>
              ))}
            </Dropdown>
          </label>
          <Button appearance="primary" onClick={() => void handleApplyFilters()}>
            Apply Filters
          </Button>
        </div>

        {!selectedChecklistType && summary && checklistFilterOptions.length > 0 && (
          <div className={styles.chipsRow}>
            <span className={styles.chipsLabel}>Checklists Found:</span>
            {checklistFilterOptions.map((option) => (
              <button
                key={option.checklistType}
                type="button"
                className={`${styles.chipButton} ${chipChecklistType === option.checklistType ? styles.chipButtonActive : ''}`}
                onClick={() => void handleChipClick(option.checklistType)}
              >
                {option.checklistName}
              </button>
            ))}
          </div>
        )}

        {effectiveChecklistType && (
          <div className={styles.currentViewTag}>Current view: {effectiveChecklistType} checklist</div>
        )}
      </section>

      {error && <div className={styles.error}>{error}</div>}

      {loadingSummary ? (
        <div className={styles.loading}><Spinner label="Loading checklist summary..." /></div>
      ) : (
        <section className={styles.splitPanel}>
          <article className={styles.chartCard}>
            <div className={styles.cardTitleRow}>
              <h3 className={styles.cardTitle}>Question Results Summary</h3>
              <span className={styles.inlineHint}>Click chart item - responses for that question</span>
            </div>
            {summary && summary.questions.length > 0 ? (
              <div className={styles.questionList}>
                {summary.questions.map((question) => {
                  const total = question.responseCount || question.responseBuckets.reduce((sum, b) => sum + b.count, 0) || 1;
                  return (
                    <button
                      key={question.checklistTemplateItemId}
                      type="button"
                      className={`${styles.questionBarRow} ${selectedQuestion?.checklistTemplateItemId === question.checklistTemplateItemId ? styles.questionBarRowActive : ''}`}
                      onClick={() => void handleSelectQuestion(question)}
                    >
                      <span className={styles.questionPrompt}>{question.prompt}</span>
                      <div className={styles.stackedBar}>
                        {question.responseBuckets.map((bucket, idx) => (
                          <span
                            key={`${question.checklistTemplateItemId}-${bucket.value}-${idx}`}
                            className={getSummarySegmentClass(bucket.label, idx)}
                            style={{ width: `${bucketPercent(bucket.count, total)}%` }}
                            title={`${bucket.label}: ${bucket.count}`}
                          />
                        ))}
                      </div>
                      <span className={styles.responseCount}>{question.responseCount}</span>
                    </button>
                  );
                })}
              </div>
            ) : (
              <div className={styles.empty}>No checklist responses found for this filter.</div>
            )}
          </article>

          <article className={styles.detailCard}>
            <h3 className={styles.cardTitle}>Question Responses</h3>
            {loadingDetail ? (
              <div className={styles.loading}><Spinner label="Loading question responses..." /></div>
            ) : questionResponses ? (
              <>
                <div className={styles.selectedPrompt}>{questionResponses.prompt}</div>
                <div className={styles.distributionRow}>
                  {questionResponses.responseBuckets.map((bucket) => (
                    <div key={bucket.value} className={`${styles.distributionChip} ${getDistributionChipClass(bucket.label)}`}>
                      <span>{bucket.label}</span>
                      <strong>{bucket.count}</strong>
                    </div>
                  ))}
                </div>
                <div className={styles.tableWrap}>
                  <table className={styles.table}>
                    <thead>
                      <tr>
                        <th>Time</th>
                        <th>Operator</th>
                        <th>Response</th>
                        <th>Note</th>
                      </tr>
                    </thead>
                    <tbody>
                      {questionResponses.rows.map((row) => (
                        <tr key={`${row.checklistEntryId}-${row.respondedAtUtc}`}>
                          <td>{new Date(row.respondedAtUtc).toLocaleString()}</td>
                          <td>{row.operatorDisplayName}</td>
                          <td>{row.responseLabel}</td>
                          <td>{row.note || '-'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            ) : (
              <div className={styles.empty}>Select a question to see detailed responses.</div>
            )}
          </article>
        </section>
      )}
    </AdminLayout>
  );
}
