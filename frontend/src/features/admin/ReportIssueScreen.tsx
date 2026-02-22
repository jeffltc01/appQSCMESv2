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
import { ArrowLeftRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { issueRequestApi } from '../../api/endpoints.ts';
import { IssueRequestType, IssueRequestStatus } from '../../types/api.ts';
import type { IssueRequestDto, CreateIssueRequestDto } from '../../types/api.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
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

function statusLabel(s: number): string {
  switch (s) {
    case IssueRequestStatus.Pending: return 'Pending';
    case IssueRequestStatus.Approved: return 'Approved';
    case IssueRequestStatus.Rejected: return 'Rejected';
    default: return 'Unknown';
  }
}

function statusBadge(s: number): string {
  switch (s) {
    case IssueRequestStatus.Pending: return styles.badgeBlue;
    case IssueRequestStatus.Approved: return styles.badgeGreen;
    case IssueRequestStatus.Rejected: return styles.badgeRed;
    default: return styles.badgeGray;
  }
}

export function ReportIssueScreen() {
  const { user } = useAuth();
  const [items, setItems] = useState<IssueRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [view, setView] = useState<'list' | 'form'>('list');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

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
    try {
      const list = await issueRequestApi.getMine(user.id);
      setItems(list);
    } catch { setError('Failed to load issue requests.'); }
    finally { setLoading(false); }
  }, [user?.id]);

  useEffect(() => { load(); }, [load]);

  const resetForm = () => {
    setTitle(''); setArea(''); setError('');
    setBugDescription(''); setBugSteps(''); setBugExpected(''); setBugActual('');
    setBugScreenshots(''); setBugBrowser(''); setBugSeverity('');
    setFeatureProblem(''); setFeatureSolution(''); setFeatureAlternatives('');
    setFeaturePriority(''); setFeatureContext('');
    setQuestionText(''); setQuestionContext('');
  };

  const openForm = () => { resetForm(); setIssueType(IssueRequestType.Bug); setView('form'); };

  const buildBodyJson = (): string => {
    switch (issueType) {
      case IssueRequestType.Bug:
        return JSON.stringify({ description: bugDescription, steps: bugSteps, expected: bugExpected,
          actual: bugActual, screenshots: bugScreenshots, browser: bugBrowser, severity: bugSeverity });
      case IssueRequestType.FeatureRequest:
        return JSON.stringify({ problem: featureProblem, solution: featureSolution,
          alternatives: featureAlternatives, priority: featurePriority, context: featureContext });
      case IssueRequestType.GeneralQuestion:
        return JSON.stringify({ question: questionText, context: questionContext });
      default: return '{}';
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
    setSubmitting(true); setError('');
    try {
      const dto: CreateIssueRequestDto = {
        type: issueType, title: title.trim(), area, bodyJson: buildBodyJson(),
        submittedByUserId: user?.id ?? '', submitterRoleTier: user?.roleTier ?? 99,
      };
      await issueRequestApi.submit(dto);
      setView('list'); resetForm(); await load();
    } catch { setError('Failed to submit issue request.'); }
    finally { setSubmitting(false); }
  };

  if (view === 'form') {
    return (
      <AdminLayout title={`New ${TYPE_LABELS[issueType]}`}>
        <div style={{ maxWidth: 700, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {[IssueRequestType.Bug, IssueRequestType.FeatureRequest, IssueRequestType.GeneralQuestion].map(t => (
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
            <Dropdown value={area} selectedOptions={area ? [area] : []}
              onOptionSelect={(_, d) => { if (d.optionValue) setArea(d.optionValue); }} placeholder="Select area...">
              {AREA_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
            </Dropdown>
          </div>

          {issueType === IssueRequestType.Bug && (
            <>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Describe the Bug</Label>
                <Textarea value={bugDescription} onChange={(_, d) => setBugDescription(d.value)} rows={3} resize="vertical"
                  placeholder="A clear description of what went wrong." />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Steps to Reproduce</Label>
                <Textarea value={bugSteps} onChange={(_, d) => setBugSteps(d.value)} rows={3} resize="vertical"
                  placeholder="1. Go to...&#10;2. Click on...&#10;3. See error" />
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
                  {BROWSER_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
                </Dropdown>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Label required>Severity</Label>
                <Dropdown value={bugSeverity} selectedOptions={bugSeverity ? [bugSeverity] : []}
                  onOptionSelect={(_, d) => { if (d.optionValue) setBugSeverity(d.optionValue); }}>
                  {SEVERITY_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
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
                  {PRIORITY_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
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
            <Button appearance="secondary" icon={<ArrowLeftRegular />}
              onClick={() => { setView('list'); setError(''); }} disabled={submitting}>Back</Button>
            <Button appearance="primary" onClick={handleSubmit} disabled={submitting}>
              {submitting ? <Spinner size="tiny" /> : 'Submit'}
            </Button>
          </div>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout title="Report Issue" onAdd={openForm} addLabel="New Issue">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No issue requests submitted yet.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
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
                <span className={styles.cardFieldLabel}>Submitted</span>
                <span className={styles.cardFieldValue}>{new Date(item.submittedAt).toLocaleDateString()}</span>
              </div>
              {item.gitHubIssueUrl && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>GitHub</span>
                  <a href={item.gitHubIssueUrl} target="_blank" rel="noopener noreferrer"
                    style={{ color: '#0078d4', fontSize: 13 }}>
                    #{item.gitHubIssueNumber}
                  </a>
                </div>
              )}
              {item.reviewerNotes && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Notes</span>
                  <span className={styles.cardFieldValue}>{item.reviewerNotes}</span>
                </div>
              )}
              <span className={`${styles.badge} ${statusBadge(item.status)}`}>
                {statusLabel(item.status)}
              </span>
            </div>
          ))}
        </div>
      )}
    </AdminLayout>
  );
}
