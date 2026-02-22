import { useState, useEffect, useCallback } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  Input,
  Textarea,
  Label,
  Spinner,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { DismissRegular, AddRegular, ArrowLeftRegular } from '@fluentui/react-icons';
import { issueRequestApi } from '../../api/endpoints.ts';
import { IssueRequestType, IssueRequestStatus } from '../../types/api.ts';
import type { IssueRequestDto, CreateIssueRequestDto } from '../../types/api.ts';
import styles from './IssueRequestDialog.module.css';

interface IssueRequestDialogProps {
  open: boolean;
  onClose: () => void;
  userId: string;
  roleTier: number;
}

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

function statusClass(s: number): string {
  switch (s) {
    case IssueRequestStatus.Pending: return styles.statusPending;
    case IssueRequestStatus.Approved: return styles.statusApproved;
    case IssueRequestStatus.Rejected: return styles.statusRejected;
    default: return '';
  }
}

export function IssueRequestDialog({ open, onClose, userId, roleTier }: IssueRequestDialogProps) {
  const [view, setView] = useState<'list' | 'form'>('list');
  const [requests, setRequests] = useState<IssueRequestDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [issueType, setIssueType] = useState<number>(IssueRequestType.Bug);
  const [title, setTitle] = useState('');
  const [area, setArea] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Bug fields
  const [bugDescription, setBugDescription] = useState('');
  const [bugSteps, setBugSteps] = useState('');
  const [bugExpected, setBugExpected] = useState('');
  const [bugActual, setBugActual] = useState('');
  const [bugScreenshots, setBugScreenshots] = useState('');
  const [bugBrowser, setBugBrowser] = useState('');
  const [bugSeverity, setBugSeverity] = useState('');

  // Feature fields
  const [featureProblem, setFeatureProblem] = useState('');
  const [featureSolution, setFeatureSolution] = useState('');
  const [featureAlternatives, setFeatureAlternatives] = useState('');
  const [featurePriority, setFeaturePriority] = useState('');
  const [featureContext, setFeatureContext] = useState('');

  // Question fields
  const [questionText, setQuestionText] = useState('');
  const [questionContext, setQuestionContext] = useState('');

  const loadData = useCallback(async () => {
    if (!userId) return;
    setLoading(true);
    setError('');
    try {
      const list = await issueRequestApi.getMine(userId);
      setRequests(list);
    } catch {
      setError('Failed to load issue requests.');
    } finally {
      setLoading(false);
    }
  }, [userId]);

  useEffect(() => {
    if (open) {
      setView('list');
      loadData();
    }
  }, [open, loadData]);

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

  const handleAddClick = () => {
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

  const validateForm = (): boolean => {
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
    if (!validateForm()) return;

    setSubmitting(true);
    setError('');
    try {
      const dto: CreateIssueRequestDto = {
        type: issueType,
        title: title.trim(),
        area,
        bodyJson: buildBodyJson(),
        submittedByUserId: userId,
        submitterRoleTier: roleTier,
      };

      await issueRequestApi.submit(dto);
      setView('list');
      resetForm();
      await loadData();
    } catch {
      setError('Failed to submit issue request.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <DialogTitle
            action={
              <Button appearance="subtle" aria-label="close" icon={<DismissRegular />} onClick={onClose} />
            }
          >
            {view === 'list' ? 'Issue Requests' : `New ${TYPE_LABELS[issueType]}`}
          </DialogTitle>

          <DialogContent className={styles.content}>
            {view === 'list' ? (
              <>
                <div className={styles.toolbar}>
                  <Button appearance="primary" icon={<AddRegular />} onClick={handleAddClick} disabled={loading}>
                    Report Issue
                  </Button>
                </div>

                {loading ? (
                  <div className={styles.emptyState}><Spinner size="medium" label="Loading..." /></div>
                ) : requests.length === 0 ? (
                  <div className={styles.emptyState}>No issue requests submitted yet.</div>
                ) : (
                  <div style={{ overflowX: 'auto' }}>
                    <table className={styles.table}>
                      <thead>
                        <tr>
                          <th>Type</th>
                          <th>Title</th>
                          <th>Status</th>
                          <th>Submitted</th>
                          <th>GitHub</th>
                        </tr>
                      </thead>
                      <tbody>
                        {requests.map(req => (
                          <tr key={req.id}>
                            <td>{TYPE_LABELS[req.type] ?? 'Unknown'}</td>
                            <td>{req.title}</td>
                            <td className={statusClass(req.status)}>{statusLabel(req.status)}</td>
                            <td>{new Date(req.submittedAt).toLocaleDateString()}</td>
                            <td>
                              {req.gitHubIssueUrl ? (
                                <a
                                  href={req.gitHubIssueUrl}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  className={styles.ghLink}
                                >
                                  #{req.gitHubIssueNumber}
                                </a>
                              ) : '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {error && <div className={styles.error}>{error}</div>}
              </>
            ) : (
              <div className={styles.formGrid}>
                <div className={styles.formField}>
                  <Label>Issue Type</Label>
                  <div className={styles.typeSelector}>
                    {[IssueRequestType.Bug, IssueRequestType.FeatureRequest, IssueRequestType.GeneralQuestion].map(t => (
                      <Button
                        key={t}
                        appearance={issueType === t ? 'primary' : 'secondary'}
                        className={issueType === t ? styles.typeBtnActive : styles.typeBtn}
                        onClick={() => setIssueType(t)}
                      >
                        {TYPE_LABELS[t]}
                      </Button>
                    ))}
                  </div>
                </div>

                <div className={styles.formField}>
                  <Label required>Title</Label>
                  <Input
                    value={title}
                    onChange={(_, d) => setTitle(d.value)}
                    placeholder="Brief summary of the issue"
                    autoFocus
                  />
                </div>

                <div className={styles.formField}>
                  <Label required>Area of the Application</Label>
                  <Dropdown
                    value={area}
                    selectedOptions={area ? [area] : []}
                    onOptionSelect={(_, d) => { if (d.optionValue) setArea(d.optionValue); }}
                    placeholder="Select area..."
                  >
                    {AREA_OPTIONS.map(opt => (
                      <Option key={opt} value={opt}>{opt}</Option>
                    ))}
                  </Dropdown>
                </div>

                {issueType === IssueRequestType.Bug && (
                  <>
                    <div className={styles.formField}>
                      <Label required>Describe the Bug</Label>
                      <Textarea value={bugDescription} onChange={(_, d) => setBugDescription(d.value)}
                        placeholder="A clear description of what went wrong." rows={3} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>Steps to Reproduce</Label>
                      <Textarea value={bugSteps} onChange={(_, d) => setBugSteps(d.value)}
                        placeholder="1. Go to...&#10;2. Click on...&#10;3. See error" rows={3} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>Expected Behavior</Label>
                      <Textarea value={bugExpected} onChange={(_, d) => setBugExpected(d.value)}
                        placeholder="What did you expect to happen?" rows={2} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>Actual Behavior</Label>
                      <Textarea value={bugActual} onChange={(_, d) => setBugActual(d.value)}
                        placeholder="What actually happened instead?" rows={2} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label>Screenshots or Error Messages</Label>
                      <Textarea value={bugScreenshots} onChange={(_, d) => setBugScreenshots(d.value)}
                        placeholder="Paste any error messages or describe screenshots." rows={2} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>Browser</Label>
                      <Dropdown value={bugBrowser} selectedOptions={bugBrowser ? [bugBrowser] : []}
                        onOptionSelect={(_, d) => { if (d.optionValue) setBugBrowser(d.optionValue); }}
                        placeholder="Select browser...">
                        {BROWSER_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
                      </Dropdown>
                    </div>
                    <div className={styles.formField}>
                      <Label required>Severity</Label>
                      <Dropdown value={bugSeverity} selectedOptions={bugSeverity ? [bugSeverity] : []}
                        onOptionSelect={(_, d) => { if (d.optionValue) setBugSeverity(d.optionValue); }}
                        placeholder="Select severity...">
                        {SEVERITY_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
                      </Dropdown>
                    </div>
                  </>
                )}

                {issueType === IssueRequestType.FeatureRequest && (
                  <>
                    <div className={styles.formField}>
                      <Label required>What Problem Does This Solve?</Label>
                      <Textarea value={featureProblem} onChange={(_, d) => setFeatureProblem(d.value)}
                        placeholder="Describe the problem or frustration this would address." rows={3} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>Describe the Feature You'd Like</Label>
                      <Textarea value={featureSolution} onChange={(_, d) => setFeatureSolution(d.value)}
                        placeholder="A clear description of what you want to happen." rows={3} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label>Alternatives or Workarounds</Label>
                      <Textarea value={featureAlternatives} onChange={(_, d) => setFeatureAlternatives(d.value)}
                        placeholder="Have you tried any workarounds?" rows={2} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label required>How Important Is This to You?</Label>
                      <Dropdown value={featurePriority} selectedOptions={featurePriority ? [featurePriority] : []}
                        onOptionSelect={(_, d) => { if (d.optionValue) setFeaturePriority(d.optionValue); }}
                        placeholder="Select priority...">
                        {PRIORITY_OPTIONS.map(opt => <Option key={opt} value={opt}>{opt}</Option>)}
                      </Dropdown>
                    </div>
                    <div className={styles.formField}>
                      <Label>Additional Context</Label>
                      <Textarea value={featureContext} onChange={(_, d) => setFeatureContext(d.value)}
                        placeholder="Any other details, screenshots, or mockups." rows={2} resize="vertical" />
                    </div>
                  </>
                )}

                {issueType === IssueRequestType.GeneralQuestion && (
                  <>
                    <div className={styles.formField}>
                      <Label required>Your Question</Label>
                      <Textarea value={questionText} onChange={(_, d) => setQuestionText(d.value)}
                        placeholder="What would you like to know?" rows={4} resize="vertical" />
                    </div>
                    <div className={styles.formField}>
                      <Label>Additional Context</Label>
                      <Textarea value={questionContext} onChange={(_, d) => setQuestionContext(d.value)}
                        placeholder="Any background or context that helps explain your question." rows={2} resize="vertical" />
                    </div>
                  </>
                )}

                {error && <div className={styles.error}>{error}</div>}
              </div>
            )}
          </DialogContent>

          <DialogActions>
            {view === 'list' ? (
              <Button appearance="secondary" onClick={onClose}>Close</Button>
            ) : (
              <>
                <Button appearance="secondary" icon={<ArrowLeftRegular />}
                  onClick={() => { setView('list'); setError(''); }} disabled={submitting}>
                  Back
                </Button>
                <Button appearance="primary" onClick={handleSubmit} disabled={submitting}>
                  {submitting ? <Spinner size="tiny" /> : 'Submit'}
                </Button>
              </>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
