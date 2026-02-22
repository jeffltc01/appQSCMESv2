import { useState, useEffect } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  Textarea,
  Label,
  Spinner,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import { adminAnnotationTypeApi, logViewerApi } from '../../api/endpoints.ts';
import type { AdminAnnotationType } from '../../types/domain.ts';
import styles from './AnnotationDialog.module.css';

interface AnnotationDialogProps {
  open: boolean;
  onClose: () => void;
  productionRecordId: string;
  serialOrIdentifier: string;
  operatorId: string;
  onCreated: () => void;
}

interface CannedMessage {
  label: string;
  switchToType?: string;
}

const CANNED_MESSAGES: CannedMessage[] = [
  { label: 'Data entry error', switchToType: 'Correction Needed' },
  { label: 'Defective material identified', switchToType: 'Defect' },
  { label: 'Wrong Shell or Tank scanned', switchToType: 'Correction Needed' },
  { label: 'See me for note', switchToType: 'Note' },
];

const CANNED_LABELS = new Set(CANNED_MESSAGES.map((m) => m.label));

const CANNED_TYPE_NAMES = new Set(
  CANNED_MESSAGES.map((m) => m.switchToType).filter(Boolean) as string[],
);

export function AnnotationDialog({
  open,
  onClose,
  productionRecordId,
  serialOrIdentifier,
  operatorId,
  onCreated,
}: AnnotationDialogProps) {
  const [allTypes, setAllTypes] = useState<AdminAnnotationType[]>([]);
  const [visibleTypes, setVisibleTypes] = useState<AdminAnnotationType[]>([]);
  const [selectedTypeId, setSelectedTypeId] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setNotes('');
    setError('');
    setSubmitting(false);
    setLoading(true);

    adminAnnotationTypeApi
      .getAll()
      .then((all) => {
        setAllTypes(all);
        const visible = all.filter(
          (t) => t.operatorCanCreate || CANNED_TYPE_NAMES.has(t.name),
        );
        setVisibleTypes(visible);
        const correctionNeeded = visible.find(
          (t) => t.name.toLowerCase().includes('correction'),
        );
        setSelectedTypeId(correctionNeeded?.id ?? visible[0]?.id ?? '');
      })
      .catch(() => setError('Failed to load annotation types.'))
      .finally(() => setLoading(false));
  }, [open]);

  const selectedType = visibleTypes.find((t) => t.id === selectedTypeId);

  const handleCannedClick = (cm: CannedMessage) => {
    setNotes((prev) => {
      const trimmed = prev.trim();
      if (!trimmed || CANNED_LABELS.has(trimmed)) return cm.label;
      return `${prev}\n${cm.label}`;
    });
    if (cm.switchToType) {
      const target = allTypes.find(
        (t) => t.name.toLowerCase() === cm.switchToType!.toLowerCase(),
      );
      if (target) setSelectedTypeId(target.id);
    }
  };

  const handleSubmit = async () => {
    if (!selectedTypeId) {
      setError('Select an annotation type.');
      return;
    }
    if (!notes.trim()) {
      setError('Enter or select a message.');
      return;
    }

    setSubmitting(true);
    setError('');
    try {
      await logViewerApi.createAnnotation({
        productionRecordId,
        annotationTypeId: selectedTypeId,
        notes: notes.trim(),
        initiatedByUserId: operatorId,
      });
      onCreated();
      onClose();
    } catch (err: unknown) {
      const msg = (err && typeof err === 'object' && 'message' in err)
        ? (err as { message: string }).message
        : 'Failed to create annotation.';
      setError(msg);
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
            Create Annotation
          </DialogTitle>

          <DialogContent className={styles.content}>
            {loading ? (
              <div className={styles.centered}><Spinner size="medium" label="Loading..." /></div>
            ) : (
              <>
                <div className={styles.recordLabel}>
                  Record: <strong>{serialOrIdentifier}</strong>
                </div>

                <div className={styles.field}>
                  <Label required>Annotation Type</Label>
                  <Dropdown
                    value={selectedType?.name ?? ''}
                    selectedOptions={selectedTypeId ? [selectedTypeId] : []}
                    onOptionSelect={(_, d) => { if (d.optionValue) setSelectedTypeId(d.optionValue); }}
                  >
                    {visibleTypes.map((t) => (
                      <Option key={t.id} value={t.id} text={t.name}>
                        <span className={styles.typeOption}>
                          <span
                            className={styles.colorDot}
                            style={{ background: t.displayColor ?? '#ccc' }}
                          />
                          {t.name}
                        </span>
                      </Option>
                    ))}
                  </Dropdown>
                </div>

                <div className={styles.field}>
                  <Label>Quick Messages</Label>
                  <div className={styles.cannedRow}>
                    {CANNED_MESSAGES.map((cm) => (
                      <Button
                        key={cm.label}
                        appearance="outline"
                        size="small"
                        className={styles.cannedBtn}
                        onClick={() => handleCannedClick(cm)}
                      >
                        {cm.label}
                      </Button>
                    ))}
                  </div>
                </div>

                <div className={styles.field}>
                  <Label required>Notes</Label>
                  <Textarea
                    value={notes}
                    onChange={(_, d) => setNotes(d.value)}
                    placeholder="Type a message or select a quick message above..."
                    rows={3}
                    resize="vertical"
                  />
                </div>

                {error && <div className={styles.error}>{error}</div>}
              </>
            )}
          </DialogContent>

          <DialogActions>
            <Button appearance="secondary" onClick={onClose} disabled={submitting}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleSubmit} disabled={submitting || loading}>
              {submitting ? <Spinner size="tiny" /> : 'Create Annotation'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
