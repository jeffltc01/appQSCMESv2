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
import { limbleApi } from '../../api/endpoints.ts';
import type { LimbleTask, LimbleStatus } from '../../types/api.ts';
import styles from './MaintenanceRequestDialog.module.css';

interface MaintenanceRequestDialogProps {
  open: boolean;
  onClose: () => void;
  employeeNumber: string;
  displayName: string;
}

const PRIORITY_OPTIONS = [
  { value: '1', label: '1 - Low' },
  { value: '2', label: '2 - Medium' },
  { value: '3', label: '3 - High' },
  { value: '4', label: '4 - Critical' },
] as const;

function priorityLabel(p?: number): string {
  switch (p) {
    case 1: return 'Low';
    case 2: return 'Medium';
    case 3: return 'High';
    case 4: return 'Critical';
    default: return p != null ? String(p) : '--';
  }
}

function priorityClass(p?: number): string {
  switch (p) {
    case 1: return styles.priorityLow;
    case 2: return styles.priorityMed;
    case 3: return styles.priorityHigh;
    case 4: return styles.priorityCritical;
    default: return '';
  }
}

function formatUnixDate(ts?: number): string {
  if (!ts) return '--';
  const d = new Date(ts * 1000);
  return d.toLocaleDateString();
}

export function MaintenanceRequestDialog({ open, onClose, employeeNumber }: MaintenanceRequestDialogProps) {
  const [view, setView] = useState<'list' | 'form'>('list');
  const [requests, setRequests] = useState<LimbleTask[]>([]);
  const [statuses, setStatuses] = useState<LimbleStatus[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [subject, setSubject] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState('2');
  const [dueDate, setDueDate] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const statusMap = useCallback(() => {
    const map = new Map<number, string>();
    statuses.forEach(s => map.set(s.id, s.name));
    return map;
  }, [statuses]);

  const loadData = useCallback(async () => {
    if (!employeeNumber) return;
    setLoading(true);
    setError('');
    try {
      const [taskList, statusList] = await Promise.all([
        limbleApi.getMyRequests(employeeNumber),
        limbleApi.getStatuses(),
      ]);
      setRequests(taskList);
      setStatuses(statusList);
    } catch {
      setError('Failed to load maintenance requests.');
    } finally {
      setLoading(false);
    }
  }, [employeeNumber]);

  useEffect(() => {
    if (open) {
      setView('list');
      loadData();
    }
  }, [open, loadData]);

  const resetForm = () => {
    setSubject('');
    setDescription('');
    setPriority('2');
    setDueDate('');
    setError('');
  };

  const handleAddClick = () => {
    resetForm();
    setView('form');
  };

  const handleBackToList = () => {
    setView('list');
    setError('');
  };

  const handleSubmit = async () => {
    if (!subject.trim()) { setError('Subject is required.'); return; }
    if (!description.trim()) { setError('Description is required.'); return; }

    setSubmitting(true);
    setError('');
    try {
      let dueDateUnix: number | undefined;
      if (dueDate) {
        dueDateUnix = Math.floor(new Date(dueDate + 'T00:00:00').getTime() / 1000);
      }

      await limbleApi.createWorkRequest({
        subject: subject.trim(),
        description: description.trim(),
        priority: parseInt(priority, 10),
        requestedDueDate: dueDateUnix,
      });

      setView('list');
      resetForm();
      await loadData();
    } catch {
      setError('Failed to create work request.');
    } finally {
      setSubmitting(false);
    }
  };

  const sMap = statusMap();

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <DialogTitle
            action={
              <Button appearance="subtle" aria-label="close" icon={<DismissRegular />} onClick={onClose} />
            }
          >
            {view === 'list' ? 'Maintenance Requests' : 'New Maintenance Request'}
          </DialogTitle>

          <DialogContent className={styles.content}>
            {view === 'list' ? (
              <>
                <div className={styles.toolbar}>
                  <Button appearance="primary" icon={<AddRegular />} onClick={handleAddClick} disabled={loading}>
                    Add Request
                  </Button>
                </div>

                {loading ? (
                  <div className={styles.emptyState}><Spinner size="medium" label="Loading..." /></div>
                ) : requests.length === 0 ? (
                  <div className={styles.emptyState}>No maintenance requests found in the last 30 days.</div>
                ) : (
                  <div style={{ overflowX: 'auto' }}>
                    <table className={styles.table}>
                      <thead>
                        <tr>
                          <th>Subject</th>
                          <th>Status</th>
                          <th>Priority</th>
                          <th>Due Date</th>
                          <th>Created</th>
                        </tr>
                      </thead>
                      <tbody>
                        {requests.map(req => (
                          <tr key={req.id}>
                            <td>{req.name}</td>
                            <td>{req.statusId != null ? (sMap.get(req.statusId) ?? `Status ${req.statusId}`) : '--'}</td>
                            <td className={priorityClass(req.priority)}>{priorityLabel(req.priority)}</td>
                            <td>{formatUnixDate(req.dueDate)}</td>
                            <td>{formatUnixDate(req.createdDate)}</td>
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
                  <Label required>Subject</Label>
                  <Input
                    value={subject}
                    onChange={(_, d) => setSubject(d.value)}
                    placeholder="Brief description of the issue"
                    autoFocus
                  />
                </div>

                <div className={styles.formField}>
                  <Label required>Description</Label>
                  <Textarea
                    value={description}
                    onChange={(_, d) => setDescription(d.value)}
                    placeholder="Detailed description of the maintenance need..."
                    rows={4}
                    resize="vertical"
                  />
                </div>

                <div className={styles.formField}>
                  <Label>Priority</Label>
                  <Dropdown
                    value={PRIORITY_OPTIONS.find(o => o.value === priority)?.label ?? ''}
                    selectedOptions={[priority]}
                    onOptionSelect={(_, d) => { if (d.optionValue) setPriority(d.optionValue); }}
                  >
                    {PRIORITY_OPTIONS.map(opt => (
                      <Option key={opt.value} value={opt.value}>{opt.label}</Option>
                    ))}
                  </Dropdown>
                </div>

                <div className={styles.formField}>
                  <Label>Requested Due Date</Label>
                  <Input
                    type="date"
                    value={dueDate}
                    onChange={(_, d) => setDueDate(d.value)}
                  />
                </div>

                {error && <div className={styles.error}>{error}</div>}
              </div>
            )}
          </DialogContent>

          <DialogActions>
            {view === 'list' ? (
              <Button appearance="secondary" onClick={onClose}>Close</Button>
            ) : (
              <>
                <Button appearance="secondary" icon={<ArrowLeftRegular />} onClick={handleBackToList} disabled={submitting}>
                  Back
                </Button>
                <Button appearance="primary" onClick={handleSubmit} disabled={submitting}>
                  {submitting ? <Spinner size="tiny" /> : 'Submit Request'}
                </Button>
              </>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
