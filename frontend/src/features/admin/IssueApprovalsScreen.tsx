import { useState, useEffect, useCallback } from 'react';
import {
  Button,
  Spinner,
  Input,
  Textarea,
  Label,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { CheckmarkRegular, DismissRegular, EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { issueRequestApi } from '../../api/endpoints.ts';
import { IssueRequestType } from '../../types/api.ts';
import type { IssueRequestDto } from '../../types/api.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import { formatDateOnly } from '../../utils/dateFormat.ts';
import styles from './CardList.module.css';

const TYPE_LABELS: Record<number, string> = {
  [IssueRequestType.Bug]: 'Bug Report',
  [IssueRequestType.FeatureRequest]: 'Feature Request',
  [IssueRequestType.GeneralQuestion]: 'General Question',
};

const AREA_OPTIONS = [
  'Login / Authentication', 'Menu / Navigation', 'Fitup Queue', 'Long Seam',
  'Round Seam', 'Rolls / Material', 'Scan Overlay', 'Admin - Users',
  'Admin - Products', 'Admin - Production Lines', 'Admin - Annotation Types',
  'New feature / Not in the app yet', 'Other',
];

function parseBodyFields(bodyJson: string): Record<string, string> {
  try { return JSON.parse(bodyJson); } catch { return {}; }
}

function formatFieldLabel(key: string): string {
  const labels: Record<string, string> = {
    description: 'Description', steps: 'Steps to Reproduce', expected: 'Expected Behavior',
    actual: 'Actual Behavior', screenshots: 'Screenshots', browser: 'Browser', severity: 'Severity',
    problem: 'Problem', solution: 'Desired Feature', alternatives: 'Alternatives',
    priority: 'Priority', context: 'Additional Context', question: 'Question',
  };
  return labels[key] ?? key;
}

export function IssueApprovalsScreen() {
  const { user } = useAuth();
  const [items, setItems] = useState<IssueRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [, setError] = useState('');

  const [reviewItem, setReviewItem] = useState<IssueRequestDto | null>(null);
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState('');
  const [editArea, setEditArea] = useState('');
  const [editBodyFields, setEditBodyFields] = useState<Record<string, string>>({});
  const [rejectNotes, setRejectNotes] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [actionError, setActionError] = useState('');

  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<IssueRequestDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const list = await issueRequestApi.getPending();
      setItems(list);
    } catch { setError('Failed to load pending issue requests.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openReview = (item: IssueRequestDto) => {
    setReviewItem(item);
    setEditing(false);
    setEditTitle(item.title);
    setEditArea(item.area);
    setEditBodyFields(parseBodyFields(item.bodyJson));
    setActionError('');
  };

  const handleApprove = async () => {
    if (!reviewItem || !user?.id) return;
    setActionLoading(true);
    setActionError('');
    try {
      const dto = editing
        ? { reviewerUserId: user.id, title: editTitle, area: editArea, bodyJson: JSON.stringify(editBodyFields) }
        : { reviewerUserId: user.id };
      await issueRequestApi.approve(reviewItem.id, dto);
      setReviewItem(null);
      await load();
    } catch {
      setActionError('Failed to approve. The GitHub issue may not have been created — check your token configuration.');
    } finally { setActionLoading(false); }
  };

  const openRejectDialog = (item: IssueRequestDto) => {
    setRejectTarget(item);
    setRejectNotes('');
    setRejectDialogOpen(true);
  };

  const handleReject = async () => {
    if (!rejectTarget || !user?.id) return;
    setActionLoading(true);
    try {
      await issueRequestApi.reject(rejectTarget.id, { reviewerUserId: user.id, notes: rejectNotes || undefined });
      setRejectDialogOpen(false);
      setRejectTarget(null);
      setReviewItem(null);
      await load();
    } catch { setActionError('Failed to reject issue request.'); }
    finally { setActionLoading(false); }
  };

  const bodyFields = reviewItem ? parseBodyFields(reviewItem.bodyJson) : {};

  return (
    <AdminLayout title="Issue Approvals">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : reviewItem ? (
        <div style={{ maxWidth: 800, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <Button appearance="secondary" onClick={() => setReviewItem(null)}>Back to List</Button>
            <span className={`${styles.badge} ${styles.badgeBlue}`}>
              {TYPE_LABELS[reviewItem.type] ?? 'Unknown'}
            </span>
            <span style={{ fontSize: 13, color: '#666' }}>
              Submitted by {reviewItem.submittedByName} on {formatDateOnly(reviewItem.submittedAt)}
            </span>
            <div style={{ marginLeft: 'auto', display: 'flex', gap: 8 }}>
              <Button appearance="subtle" icon={<EditRegular />}
                onClick={() => setEditing(!editing)}>
                {editing ? 'Stop Editing' : 'Edit'}
              </Button>
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Label weight="semibold">Title</Label>
              {editing ? (
                <Input value={editTitle} onChange={(_, d) => setEditTitle(d.value)} />
              ) : (
                <span>{reviewItem.title}</span>
              )}
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Label weight="semibold">Area</Label>
              {editing ? (
                <Dropdown value={editArea} selectedOptions={editArea ? [editArea] : []}
                  onOptionSelect={(_, d) => { if (d.optionValue) setEditArea(d.optionValue); }}>
                  {AREA_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
                </Dropdown>
              ) : (
                <span>{reviewItem.area}</span>
              )}
            </div>

            {Object.entries(editing ? editBodyFields : bodyFields).map(([key, value]) => (
              value ? (
                <div key={key} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <Label weight="semibold">{formatFieldLabel(key)}</Label>
                  {editing ? (
                    <Textarea
                      value={editBodyFields[key] ?? ''}
                      onChange={(_, d) => setEditBodyFields(prev => ({ ...prev, [key]: d.value }))}
                      rows={2}
                      resize="vertical"
                    />
                  ) : (
                    <span style={{ whiteSpace: 'pre-wrap' }}>{value}</span>
                  )}
                </div>
              ) : null
            ))}
          </div>

          {actionError && (
            <div style={{ color: '#d13438', fontSize: 13, padding: '8px 12px', background: '#fde7e9', borderRadius: 4 }}>
              {actionError}
            </div>
          )}

          <div style={{ display: 'flex', gap: 8 }}>
            <Button appearance="primary" icon={<CheckmarkRegular />}
              onClick={handleApprove} disabled={actionLoading}>
              {actionLoading ? <Spinner size="tiny" /> : 'Approve & Create GitHub Issue'}
            </Button>
            <Button appearance="secondary" icon={<DismissRegular />}
              onClick={() => openRejectDialog(reviewItem)} disabled={actionLoading}
              style={{ color: '#d13438' }}>
              Reject
            </Button>
          </div>
        </div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No pending issue requests.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card} style={{ cursor: 'pointer' }}
              onClick={() => openReview(item)}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.title}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{TYPE_LABELS[item.type] ?? 'Unknown'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Area</span>
                <span className={styles.cardFieldValue}>{item.area}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Submitted By</span>
                <span className={styles.cardFieldValue}>{item.submittedByName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Date</span>
                <span className={styles.cardFieldValue}>{formatDateOnly(item.submittedAt)}</span>
              </div>
              <span className={`${styles.badge} ${styles.badgeBlue}`}>Pending Review</span>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={rejectDialogOpen}
        title="Reject Issue Request"
        onConfirm={handleReject}
        onCancel={() => { setRejectDialogOpen(false); setRejectTarget(null); }}
        confirmLabel="Reject"
        loading={actionLoading}
        error={actionError}
      >
        <Label>Rejection Notes (optional)</Label>
        <Textarea
          value={rejectNotes}
          onChange={(_, d) => setRejectNotes(d.value)}
          placeholder="Reason for rejection..."
          rows={3}
          resize="vertical"
        />
      </AdminModal>
    </AdminLayout>
  );
}
