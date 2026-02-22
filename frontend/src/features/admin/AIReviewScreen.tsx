import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Button,
  Spinner,
  Dropdown,
  Option,
  Textarea,
  Label,
  Checkbox,
} from '@fluentui/react-components';
import { CheckmarkCircleRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { workCenterApi, aiReviewApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { WorkCenter, AIReviewRecord } from '../../types/domain.ts';
import { todayISOString, formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './AIReviewScreen.module.css';

const REFRESH_INTERVAL_MS = 30_000;

export function AIReviewScreen() {
  const { user } = useAuth();
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [selectedWcId, setSelectedWcId] = useState('');
  const [selectedWcName, setSelectedWcName] = useState('');
  const [records, setRecords] = useState<AIReviewRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitMessage, setSubmitMessage] = useState('');
  const refreshTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    workCenterApi.getWorkCenters().then(setWorkCenters).catch(() => {});
  }, []);


  const loadRecords = useCallback(async () => {
    if (!selectedWcId || !user?.defaultSiteId) return;
    setLoading(true);
    try {
      const data = await aiReviewApi.getRecords(selectedWcId, user.defaultSiteId, todayISOString());
      setRecords(data);
      setCheckedIds((prev) => {
        const validIds = new Set(data.filter((r) => !r.alreadyReviewed).map((r) => r.id));
        return new Set([...prev].filter((id) => validIds.has(id)));
      });
    } catch {
      setRecords([]);
    } finally {
      setLoading(false);
    }
  }, [selectedWcId, user?.defaultSiteId]);

  useEffect(() => {
    if (selectedWcId) {
      loadRecords();
    } else {
      setRecords([]);
      setCheckedIds(new Set());
    }
  }, [selectedWcId, loadRecords]);

  useEffect(() => {
    if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    if (selectedWcId) {
      refreshTimerRef.current = setInterval(loadRecords, REFRESH_INTERVAL_MS);
    }
    return () => {
      if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    };
  }, [selectedWcId, loadRecords]);

  const handleWcChange = (_: unknown, data: { optionValue?: string; optionText?: string }) => {
    const wcId = data.optionValue ?? '';
    setSelectedWcId(wcId);
    setSelectedWcName(data.optionText ?? '');
    setCheckedIds(new Set());
    setComment('');
    setSubmitMessage('');
  };

  const toggleCheck = (id: string) => {
    setCheckedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleSubmit = async () => {
    if (checkedIds.size === 0) return;
    setSubmitting(true);
    setSubmitMessage('');
    try {
      const result = await aiReviewApi.submitReview({
        productionRecordIds: [...checkedIds],
        comment: comment.trim() || undefined,
      });
      setSubmitMessage(`${result.annotationsCreated} record(s) marked as AI Reviewed.`);
      setCheckedIds(new Set());
      setComment('');
      await loadRecords();
    } catch {
      setSubmitMessage('Failed to submit review. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };


  return (
    <AdminLayout title="AI Review">
      <div className={styles.toolbar}>
        <div className={styles.toolbarField}>
          <Label weight="semibold">Work Center</Label>
          <Dropdown
            placeholder="Select a work center..."
            value={selectedWcName}
            selectedOptions={selectedWcId ? [selectedWcId] : []}
            onOptionSelect={handleWcChange}
            style={{ minWidth: 260 }}
          >
            {workCenters.map((wc) => (
              <Option key={wc.id} value={wc.id} text={wc.name}>
                {wc.name}
              </Option>
            ))}
          </Dropdown>
        </div>
        {selectedWcId && (
          <div className={styles.countLabel}>
            {records.length} record(s) today &middot; {records.filter((r) => r.alreadyReviewed).length} already reviewed
          </div>
        )}
        {selectedWcId && <span className={styles.refreshNote}>Auto-refreshes every 30s</span>}
      </div>

      {loading && !records.length ? (
        <div className={styles.emptyState}>
          <Spinner size="medium" label="Loading records..." />
        </div>
      ) : !selectedWcId ? (
        <div className={styles.emptyState}>Select a work center to begin reviewing records.</div>
      ) : records.length === 0 ? (
        <div className={styles.emptyState}>No production records today at this work center.</div>
      ) : (
        <>
          <table className={styles.table}>
            <thead>
              <tr>
                <th style={{ width: 40 }}></th>
                <th>Time</th>
                <th>Serial / Shell Code</th>
                <th>Size</th>
                <th>Operator</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {records.map((r) => (
                <tr
                  key={r.id}
                  className={r.alreadyReviewed ? styles.rowReviewed : undefined}
                >
                  <td>
                    <Checkbox
                      checked={checkedIds.has(r.id)}
                      onChange={() => toggleCheck(r.id)}
                      disabled={r.alreadyReviewed}
                    />
                  </td>
                  <td>{formatTimeOnly(r.timestamp)}</td>
                  <td>{r.serialOrIdentifier}</td>
                  <td>{r.tankSize ?? '—'}</td>
                  <td>{r.operatorName}</td>
                  <td>
                    {r.alreadyReviewed && (
                      <span className={styles.badge}>
                        <CheckmarkCircleRegular fontSize={14} /> AI Reviewed
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className={styles.submitArea}>
            <div className={styles.commentField}>
              <Label weight="semibold">Comment (optional)</Label>
              <Textarea
                value={comment}
                onChange={(_, d) => setComment(d.value)}
                placeholder="Add a note about this review..."
                rows={2}
                resize="vertical"
                style={{ width: '100%' }}
              />
            </div>
            <Button
              appearance="primary"
              icon={<CheckmarkCircleRegular />}
              onClick={handleSubmit}
              disabled={checkedIds.size === 0 || submitting}
            >
              {submitting ? <Spinner size="tiny" /> : `Mark ${checkedIds.size} as Reviewed`}
            </Button>
          </div>

          {submitMessage && (
            <div
              style={{
                marginTop: 12,
                padding: '8px 12px',
                borderRadius: 4,
                fontSize: 13,
                background: submitMessage.includes('Failed') ? '#fde7e9' : '#d3f9d8',
                color: submitMessage.includes('Failed') ? '#d13438' : '#2b8a3e',
              }}
            >
              {submitMessage}
            </div>
          )}
        </>
      )}
    </AdminLayout>
  );
}
