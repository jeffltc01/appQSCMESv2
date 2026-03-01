import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Spinner,
  Input,
  Textarea,
  Label,
  Dropdown,
  Option,
  Switch,
} from '@fluentui/react-components';
import { ArrowLeftRegular, CheckmarkRegular, EyeRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { issueRequestApi } from '../../api/endpoints.ts';
import { IssueRequestStatus, IssueRequestType } from '../../types/api.ts';
import type { ApproveIssueRequestDto, CreateIssueRequestDto, IssueRequestDto } from '../../types/api.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import { formatDateOnly } from '../../utils/dateFormat.ts';
import styles from './CardList.module.css';

const AREA_OPTIONS = [
  'Login / Authentication',
  'Menu / Navigation',
  'Fitup Queue',
  'Long Seam',
  'Round Seam',
  'Rolls / Material',
  'Scan Overlay',
  'Admin - Users',
  'Admin - Products',
  'Admin - Production Lines',
  'Admin - Annotation Types',
  'New feature / Not in the app yet',
  'Other',
];

const SEVERITY_OPTIONS = [
  'Low - Minor inconvenience, I can work around it',
  'Medium - It slows me down but I can still work',
  'High - I can\'t complete an important task',
  'Critical - The app is completely unusable',
];

const PRIORITY_OPTIONS = [
  'Nice to have - Would be a welcome improvement',
  'Important - Would significantly help my workflow',
  'Critical - I really need this to do my job effectively',
];

const BROWSER_OPTIONS = ['Chrome', 'Edge', 'Firefox', 'Safari', 'Other'];

const TYPE_LABELS: Record<number, string> = {
  [IssueRequestType.Bug]: 'Bug Report',
  [IssueRequestType.FeatureRequest]: 'Feature Request',
  [IssueRequestType.GeneralQuestion]: 'General Question',
};

const STATUS_LABELS: Record<number, string> = {
  [IssueRequestStatus.Pending]: 'Pending',
  [IssueRequestStatus.Approved]: 'Approved',
  [IssueRequestStatus.Rejected]: 'Rejected',
};

function statusBadge(status: number): string {
  switch (status) {
    case IssueRequestStatus.Pending: return styles.badgeBlue;
    case IssueRequestStatus.Approved: return styles.badgeGreen;
    case IssueRequestStatus.Rejected: return styles.badgeRed;
    default: return styles.badgeGray;
  }
}

function parseBodyFields(bodyJson: string): Record<string, string> {
  try { return JSON.parse(bodyJson); } catch { return {}; }
}

function formatFieldLabel(key: string): string {
  const labels: Record<string, string> = {
    description: 'Description',
    steps: 'Steps to Reproduce',
    expected: 'Expected Behavior',
    actual: 'Actual Behavior',
    screenshots: 'Screenshots',
    browser: 'Browser',
    severity: 'Severity',
    problem: 'Problem',
    solution: 'Desired Feature',
    alternatives: 'Alternatives',
    priority: 'Priority',
    context: 'Additional Context',
    question: 'Question',
  };
  return labels[key] ?? key;
}

function getPrimaryDetail(bodyJson: string): string {
  const fields = parseBodyFields(bodyJson);
  const orderedKeys = ['description', 'problem', 'question', 'actual', 'expected', 'solution', 'context', 'steps'];
  for (const key of orderedKeys) {
    const value = fields[key];
    if (typeof value === 'string' && value.trim()) {
      return value.trim();
    }
  }
  return '';
}

export function IssuesScreen() {
  const { user } = useAuth();
  const canApprove = (user?.roleTier ?? 99) <= 3;

  const [mineItems, setMineItems] = useState<IssueRequestDto[]>([]);
  const [pendingItems, setPendingItems] = useState<IssueRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [view, setView] = useState<'list' | 'form'>('list');
  const [submitting, setSubmitting] = useState(false);

  const [search, setSearch] = useState('');
  const [filterType, setFilterType] = useState<string>('all');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [needsApprovalOnly, setNeedsApprovalOnly] = useState(false);

  const [selectedItem, setSelectedItem] = useState<IssueRequestDto | null>(null);
  const [reviewerNotes, setReviewerNotes] = useState('');
  const [reviewActionLoading, setReviewActionLoading] = useState(false);
  const [reviewError, setReviewError] = useState('');

  const closeDetailsModal = () => {
    setSelectedItem(null);
    setReviewerNotes('');
    setReviewError('');
  };

  const openDetailsModal = (item: IssueRequestDto) => {
    setSelectedItem(item);
    setReviewerNotes('');
    setReviewError('');
  };

  const [issueType, setIssueType] = useState<number>(IssueRequestType.Bug);
  const [title, setTitle] = useState('');
  const [area, setArea] = useState('');
  const [bugDescription, setBugDescription] = useState('');
  const [bugSteps, setBugSteps] = useState('');
  const [bugExpected, setBugExpected] = useState('');
  const [bugActual, setBugActual] = useState('');
  const [bugScreenshots, setBugScreenshots] = useState('');
  const [bugBrowser, setBugBrowser] = useState('');
  const [bugSeverity, setBugSeverity] = useState('');
  const [featureProblem, setFeatureProblem] = useState('');
  const [featureSolution, setFeatureSolution] = useState('');
  const [featureAlternatives, setFeatureAlternatives] = useState('');
  const [featurePriority, setFeaturePriority] = useState('');
  const [featureContext, setFeatureContext] = useState('');
  const [questionText, setQuestionText] = useState('');
  const [questionContext, setQuestionContext] = useState('');

  const load = useCallback(async () => {
    if (!user?.id) return;
    setLoading(true);
    setError('');
    try {
      const minePromise = issueRequestApi.getMine(user.id);
      const pendingPromise = canApprove ? issueRequestApi.getPending() : Promise.resolve([]);
      const [mine, pending] = await Promise.all([minePromise, pendingPromise]);
      setMineItems(mine);
      setPendingItems(pending);
    } catch {
      setError('Failed to load issues.');
    } finally {
      setLoading(false);
    }
  }, [canApprove, user?.id]);

  useEffect(() => { load(); }, [load]);

  const baseList = needsApprovalOnly && canApprove ? pendingItems : mineItems;

  const filteredItems = useMemo(() => {
    return baseList.filter((item) => {
      const typeOk = filterType === 'all' || String(item.type) === filterType;
      const statusOk = filterStatus === 'all' || String(item.status) === filterStatus;
      const searchText = search.trim().toLowerCase();
      const searchOk = !searchText
        || item.title.toLowerCase().includes(searchText)
        || item.area.toLowerCase().includes(searchText)
        || item.submittedByName.toLowerCase().includes(searchText);
      return typeOk && statusOk && searchOk;
    });
  }, [baseList, filterStatus, filterType, search]);

  const resetForm = () => {
    setTitle('');
    setArea('');
    setError('');
    setBugDescription('');
    setBugSteps('');
    setBugExpected('');
    setBugActual('');
    setBugScreenshots('');
    setBugBrowser('');
    setBugSeverity('');
    setFeatureProblem('');
    setFeatureSolution('');
    setFeatureAlternatives('');
    setFeaturePriority('');
    setFeatureContext('');
    setQuestionText('');
    setQuestionContext('');
  };

  const openForm = () => {
    resetForm();
    setIssueType(IssueRequestType.Bug);
    setView('form');
  };

  const buildBodyJson = (): string => {
    switch (issueType) {
      case IssueRequestType.Bug:
        return JSON.stringify({
          description: bugDescription,
          steps: bugSteps,
          expected: bugExpected,
          actual: bugActual,
          screenshots: bugScreenshots,
          browser: bugBrowser,
          severity: bugSeverity,
        });
      case IssueRequestType.FeatureRequest:
        return JSON.stringify({
          problem: featureProblem,
          solution: featureSolution,
          alternatives: featureAlternatives,
          priority: featurePriority,
          context: featureContext,
        });
      case IssueRequestType.GeneralQuestion:
        return JSON.stringify({
          question: questionText,
          context: questionContext,
        });
      default:
        return '{}';
    }
  };

  const validate = (): boolean => {
    if (!title.trim()) { setError('Title is required.'); return false; }
    if (!area) { setError('Area is required.'); return false; }
    if (issueType === IssueRequestType.Bug) {
      if (!bugDescription.trim()) { setError('Bug description is required.'); return false; }
      if (!bugSteps.trim()) { setError('Steps to reproduce are required.'); return false; }
      if (!bugExpected.trim()) { setError('Expected behavior is required.'); return false; }
      if (!bugActual.trim()) { setError('Actual behavior is required.'); return false; }
      if (!bugBrowser) { setError('Browser is required.'); return false; }
      if (!bugSeverity) { setError('Severity is required.'); return false; }
    } else if (issueType === IssueRequestType.FeatureRequest) {
      if (!featureProblem.trim()) { setError('Problem description is required.'); return false; }
      if (!featureSolution.trim()) { setError('Feature description is required.'); return false; }
      if (!featurePriority) { setError('Priority is required.'); return false; }
    } else if (issueType === IssueRequestType.GeneralQuestion) {
      if (!questionText.trim()) { setError('Question is required.'); return false; }
    }
    return true;
  };

  const handleSubmit = async () => {
    if (!validate()) return;
    setSubmitting(true);
    setError('');
    try {
      const dto: CreateIssueRequestDto = {
        type: issueType,
        title: title.trim(),
        area,
        bodyJson: buildBodyJson(),
        submittedByUserId: user?.id ?? '',
        submitterRoleTier: user?.roleTier ?? 99,
      };
      await issueRequestApi.submit(dto);
      setView('list');
      resetForm();
      await load();
    } catch {
      setError('Failed to submit issue request.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleApprove = async () => {
    if (!selectedItem || !user?.id) return;
    setReviewActionLoading(true);
    setReviewError('');
    try {
      const dto: ApproveIssueRequestDto = { reviewerUserId: user.id };
      await issueRequestApi.approve(selectedItem.id, dto);
      closeDetailsModal();
      await load();
    } catch {
      setReviewError('Failed to approve issue request.');
    } finally {
      setReviewActionLoading(false);
    }
  };

  const handleDeny = async () => {
    if (!selectedItem || !user?.id) return;
    setReviewActionLoading(true);
    setReviewError('');
    try {
      await issueRequestApi.reject(selectedItem.id, {
        reviewerUserId: user.id,
        notes: reviewerNotes || undefined,
      });
      closeDetailsModal();
      await load();
    } catch {
      setReviewError('Failed to deny issue request.');
    } finally {
      setReviewActionLoading(false);
    }
  };

  const canReviewSelected = !!selectedItem && canApprove && selectedItem.status === IssueRequestStatus.Pending;

  if (view === 'form') {
    return (
      <AdminLayout title={`New ${TYPE_LABELS[issueType]}`}>
        <div style={{ maxWidth: 700, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {[IssueRequestType.Bug, IssueRequestType.FeatureRequest, IssueRequestType.GeneralQuestion].map((t) => (
              <Button key={t} appearance={issueType === t ? 'primary' : 'secondary'} onClick={() => setIssueType(t)}>
                {TYPE_LABELS[t]}
              </Button>
            ))}
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <Label required>Title</Label>
            <Input value={title} onChange={(_, d) => setTitle(d.value)} placeholder="Brief summary" autoFocus />
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <Label required>Area of the Application</Label>
            <Dropdown
              value={area}
              selectedOptions={area ? [area] : []}
              onOptionSelect={(_, d) => { if (d.optionValue) setArea(d.optionValue); }}
              placeholder="Select area..."
            >
              {AREA_OPTIONS.map((opt) => <Option key={opt} value={opt}>{opt}</Option>)}
            </Dropdown>
          </div>

          {issueType === IssueRequestType.Bug && (
            <>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Describe the Bug</Label>
                <Textarea value={bugDescription} onChange={(_, d) => setBugDescription(d.value)} rows={3} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Steps to Reproduce</Label>
                <Textarea value={bugSteps} onChange={(_, d) => setBugSteps(d.value)} rows={3} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Expected Behavior</Label>
                <Textarea value={bugExpected} onChange={(_, d) => setBugExpected(d.value)} rows={2} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Actual Behavior</Label>
                <Textarea value={bugActual} onChange={(_, d) => setBugActual(d.value)} rows={2} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label>Screenshots or Error Messages</Label>
                <Textarea value={bugScreenshots} onChange={(_, d) => setBugScreenshots(d.value)} rows={2} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Browser</Label>
                <Dropdown value={bugBrowser} selectedOptions={bugBrowser ? [bugBrowser] : []}
                  onOptionSelect={(_, d) => { if (d.optionValue) setBugBrowser(d.optionValue); }}>
                  {BROWSER_OPTIONS.map((opt) => <Option key={opt} value={opt}>{opt}</Option>)}
                </Dropdown>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Severity</Label>
                <Dropdown value={bugSeverity} selectedOptions={bugSeverity ? [bugSeverity] : []}
                  onOptionSelect={(_, d) => { if (d.optionValue) setBugSeverity(d.optionValue); }}>
                  {SEVERITY_OPTIONS.map((opt) => <Option key={opt} value={opt}>{opt}</Option>)}
                </Dropdown>
              </div>
            </>
          )}

          {issueType === IssueRequestType.FeatureRequest && (
            <>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>What Problem Does This Solve?</Label>
                <Textarea value={featureProblem} onChange={(_, d) => setFeatureProblem(d.value)} rows={3} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Describe the Feature You'd Like</Label>
                <Textarea value={featureSolution} onChange={(_, d) => setFeatureSolution(d.value)} rows={3} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label>Alternatives or Workarounds</Label>
                <Textarea value={featureAlternatives} onChange={(_, d) => setFeatureAlternatives(d.value)} rows={2} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>How Important Is This to You?</Label>
                <Dropdown value={featurePriority} selectedOptions={featurePriority ? [featurePriority] : []}
                  onOptionSelect={(_, d) => { if (d.optionValue) setFeaturePriority(d.optionValue); }}>
                  {PRIORITY_OPTIONS.map((opt) => <Option key={opt} value={opt}>{opt}</Option>)}
                </Dropdown>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label>Additional Context</Label>
                <Textarea value={featureContext} onChange={(_, d) => setFeatureContext(d.value)} rows={2} resize="vertical" />
              </div>
            </>
          )}

          {issueType === IssueRequestType.GeneralQuestion && (
            <>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Your Question</Label>
                <Textarea value={questionText} onChange={(_, d) => setQuestionText(d.value)} rows={4} resize="vertical" />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label>Additional Context</Label>
                <Textarea value={questionContext} onChange={(_, d) => setQuestionContext(d.value)} rows={2} resize="vertical" />
              </div>
            </>
          )}

          {error && <div style={{ color: '#d13438', fontSize: 13, padding: '8px 12px', background: '#fde7e9', borderRadius: 4 }}>{error}</div>}

          <div style={{ display: 'flex', gap: 8 }}>
            <Button appearance="secondary" icon={<ArrowLeftRegular />} onClick={() => { setView('list'); setError(''); }} disabled={submitting}>
              Back
            </Button>
            <Button appearance="primary" onClick={handleSubmit} disabled={submitting}>
              {submitting ? <Spinner size="tiny" /> : 'Submit'}
            </Button>
          </div>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout title="Issues" onAdd={(user?.roleTier ?? 99) <= 5 ? openForm : undefined} addLabel="New Issue">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr auto', gap: 8, marginBottom: 14, alignItems: 'center' }}>
            <Input placeholder="Search title, area, submitter..." value={search} onChange={(_, d) => setSearch(d.value)} />
            <Dropdown
              value={filterType === 'all' ? 'All types' : TYPE_LABELS[Number(filterType)]}
              selectedOptions={[filterType]}
              onOptionSelect={(_, d) => setFilterType(d.optionValue ?? 'all')}
            >
              <Option value="all">All types</Option>
              <Option value={String(IssueRequestType.Bug)}>{TYPE_LABELS[IssueRequestType.Bug]}</Option>
              <Option value={String(IssueRequestType.FeatureRequest)}>{TYPE_LABELS[IssueRequestType.FeatureRequest]}</Option>
              <Option value={String(IssueRequestType.GeneralQuestion)}>{TYPE_LABELS[IssueRequestType.GeneralQuestion]}</Option>
            </Dropdown>
            <Dropdown
              value={filterStatus === 'all' ? 'All statuses' : STATUS_LABELS[Number(filterStatus)]}
              selectedOptions={[filterStatus]}
              onOptionSelect={(_, d) => setFilterStatus(d.optionValue ?? 'all')}
            >
              <Option value="all">All statuses</Option>
              <Option value={String(IssueRequestStatus.Pending)}>{STATUS_LABELS[IssueRequestStatus.Pending]}</Option>
              <Option value={String(IssueRequestStatus.Approved)}>{STATUS_LABELS[IssueRequestStatus.Approved]}</Option>
              <Option value={String(IssueRequestStatus.Rejected)}>{STATUS_LABELS[IssueRequestStatus.Rejected]}</Option>
            </Dropdown>
            {canApprove && (
              <Switch
                label="Needs Approval Only"
                checked={needsApprovalOnly}
                onChange={(_, d) => {
                  setNeedsApprovalOnly(d.checked);
                  if (d.checked) setFilterStatus(String(IssueRequestStatus.Pending));
                }}
              />
            )}
          </div>

          {error && <div style={{ color: '#d13438', fontSize: 13, padding: '8px 12px', background: '#fde7e9', borderRadius: 4 }}>{error}</div>}

          <div className={styles.grid}>
            {filteredItems.length === 0 && (
              <div className={styles.emptyState}>
                {needsApprovalOnly ? 'No pending issue requests.' : 'No issue requests match your filters.'}
              </div>
            )}
            {filteredItems.map((item) => (
              <div key={item.id} className={styles.card}>
                <div className={styles.cardHeader}>
                  <span className={styles.cardTitle}>{item.title}</span>
                  <div className={styles.cardActions}>
                    <Button
                      appearance="subtle"
                      icon={<EyeRegular />}
                      size="small"
                      aria-label={`View details for ${item.title}`}
                      onClick={() => openDetailsModal(item)}
                    />
                    {canApprove && item.status === IssueRequestStatus.Pending && (
                      <Button
                        appearance="subtle"
                        icon={<CheckmarkRegular />}
                        size="small"
                        aria-label={`Review ${item.title}`}
                        onClick={() => openDetailsModal(item)}
                      />
                    )}
                  </div>
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
                  <span className={styles.cardFieldLabel}>Submitted</span>
                  <span className={styles.cardFieldValue}>{formatDateOnly(item.submittedAt)}</span>
                </div>
                {getPrimaryDetail(item.bodyJson) && (
                  <div className={styles.cardPreview}>{getPrimaryDetail(item.bodyJson)}</div>
                )}
                {item.gitHubIssueUrl && (
                  <div className={styles.cardField}>
                    <span className={styles.cardFieldLabel}>GitHub</span>
                    <a href={item.gitHubIssueUrl} target="_blank" rel="noopener noreferrer" style={{ color: '#0078d4', fontSize: 13 }}>
                      #{item.gitHubIssueNumber}
                    </a>
                  </div>
                )}
                <span className={`${styles.badge} ${statusBadge(item.status)}`}>{STATUS_LABELS[item.status] ?? 'Unknown'}</span>
              </div>
            ))}
          </div>
        </>
      )}

      <AdminModal
        open={!!selectedItem}
        title={selectedItem ? `Issue Details: ${selectedItem.title}` : 'Issue Details'}
        onConfirm={canReviewSelected ? handleApprove : closeDetailsModal}
        onCancel={closeDetailsModal}
        confirmLabel={canReviewSelected ? 'Approve' : 'Close'}
        loading={reviewActionLoading}
        error={reviewError}
        hideCancel={!canReviewSelected}
      >
        {selectedItem && (
          <>
            <div className={styles.cardField}>
              <span className={styles.cardFieldLabel}>Type</span>
              <span className={styles.cardFieldValue}>{TYPE_LABELS[selectedItem.type] ?? 'Unknown'}</span>
            </div>
            <div className={styles.cardField}>
              <span className={styles.cardFieldLabel}>Area</span>
              <span className={styles.cardFieldValue}>{selectedItem.area}</span>
            </div>
            <div className={styles.cardField}>
              <span className={styles.cardFieldLabel}>Submitted By</span>
              <span className={styles.cardFieldValue}>{selectedItem.submittedByName}</span>
            </div>
            <div className={styles.cardField}>
              <span className={styles.cardFieldLabel}>Submitted</span>
              <span className={styles.cardFieldValue}>{formatDateOnly(selectedItem.submittedAt)}</span>
            </div>
            <div className={styles.cardField}>
              <span className={styles.cardFieldLabel}>Status</span>
              <span className={styles.cardFieldValue}>{STATUS_LABELS[selectedItem.status] ?? 'Unknown'}</span>
            </div>
            {selectedItem.reviewedByName && (
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Reviewed By</span>
                <span className={styles.cardFieldValue}>{selectedItem.reviewedByName}</span>
              </div>
            )}
            {selectedItem.reviewedAt && (
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Reviewed</span>
                <span className={styles.cardFieldValue}>{formatDateOnly(selectedItem.reviewedAt)}</span>
              </div>
            )}
            {selectedItem.reviewerNotes && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label>Reviewer Notes</Label>
                <div style={{ whiteSpace: 'pre-wrap', fontSize: 13 }}>{selectedItem.reviewerNotes}</div>
              </div>
            )}
            {selectedItem.gitHubIssueUrl && (
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>GitHub</span>
                <a href={selectedItem.gitHubIssueUrl} target="_blank" rel="noopener noreferrer" style={{ color: '#0078d4', fontSize: 13 }}>
                  #{selectedItem.gitHubIssueNumber}
                </a>
              </div>
            )}
            {Object.entries(parseBodyFields(selectedItem.bodyJson)).map(([key, value]) => (
              value ? (
                <div key={key} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <Label>{formatFieldLabel(key)}</Label>
                  <div style={{ whiteSpace: 'pre-wrap', fontSize: 13 }}>{value}</div>
                </div>
              ) : null
            ))}
            {canReviewSelected && (
              <>
                <Label>Deny Notes (optional)</Label>
                <Textarea
                  value={reviewerNotes}
                  onChange={(_, d) => setReviewerNotes(d.value)}
                  rows={3}
                  resize="vertical"
                  placeholder="Reason for denial..."
                />
                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button appearance="secondary" onClick={handleDeny} disabled={reviewActionLoading}>
                    Deny
                  </Button>
                </div>
              </>
            )}
          </>
        )}
      </AdminModal>
    </AdminLayout>
  );
}
