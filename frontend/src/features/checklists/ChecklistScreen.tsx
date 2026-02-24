import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, CardHeader, Spinner, Textarea } from '@fluentui/react-components';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { checklistApi } from '../../api/endpoints.ts';
import type { ChecklistEntry, ChecklistTemplate } from '../../types/domain.ts';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import styles from './ChecklistScreen.module.css';

type ResponseValue = 'Pass' | 'Fail' | 'N/A';

type ChecklistScreenProps = WorkCenterProps & {
  checklistType?: string;
  onChecklistCompleted?: () => void;
};

function getChecklistTypeCandidates(selectedType: string): string[] {
  switch (selectedType) {
    case 'OpsPreShift':
      return ['OpsPreShift', 'SafetyPreShift'];
    case 'SafetyPreShift':
      return ['SafetyPreShift', 'OpsPreShift'];
    case 'OpsChangeover':
      return ['OpsChangeover'];
    default:
      return [selectedType];
  }
}

export function ChecklistScreen(props: ChecklistScreenProps) {
  const [loading, setLoading] = useState(true);
  const [template, setTemplate] = useState<ChecklistTemplate | null>(null);
  const [entry, setEntry] = useState<ChecklistEntry | null>(null);
  const [responses, setResponses] = useState<Record<string, ResponseValue>>({});
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const selectedChecklistType = props.checklistType ?? 'SafetyPreShift';

  const loadChecklist = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const candidateTypes = getChecklistTypeCandidates(selectedChecklistType);
      let loaded = false;

      for (const candidateType of candidateTypes) {
        try {
          const resolvedTemplate = await checklistApi.resolveTemplate({
            checklistType: candidateType,
            siteId: props.plantId,
            workCenterId: props.workCenterId,
            productionLineId: props.productionLineId || undefined,
          });
          setTemplate(resolvedTemplate);

          const createdEntry = await checklistApi.createEntry({
            checklistType: candidateType,
            siteId: props.plantId,
            workCenterId: props.workCenterId,
            productionLineId: props.productionLineId || undefined,
            operatorUserId: props.operatorId,
          });
          setEntry(createdEntry);
          loaded = true;
          break;
        } catch (candidateError) {
          const message = (candidateError as { message?: string } | undefined)?.message?.toLowerCase() ?? '';
          const isNotFound = message.includes('404') || message.includes('not found');
          if (!isNotFound) throw candidateError;
        }
      }

      if (!loaded) {
        throw new Error('No checklist template found for selected type.');
      }
    } catch {
      setError('Unable to load checklist template for this work center.');
    } finally {
      setLoading(false);
    }
  }, [props.operatorId, props.plantId, props.productionLineId, props.workCenterId, selectedChecklistType]);

  useEffect(() => {
    void loadChecklist();
  }, [loadChecklist]);

  useEffect(() => {
    props.registerBarcodeHandler((bc: ParsedBarcode | null, raw: string) => {
      if (!bc) return;
      if (bc.prefix === 'INP' && bc.value === '3') {
        void handleSubmit();
      } else if (bc.prefix === 'INP' && bc.value === '4') {
        props.showScanResult({ type: 'error', message: 'Use on-screen checklist buttons for item answers.' });
      } else {
        props.showScanResult({ type: 'error', message: `Unsupported barcode for checklist: ${raw}` });
      }
    });
  }, [props]);

  const requiresFailNote = useCallback((itemId: string) => {
    const item = template?.items.find((i) => i.id === itemId);
    if (!item) return false;
    return item.requireFailNote || template?.requireFailNote || template?.isSafetyProfile;
  }, [template]);

  const canUseNa = useCallback((itemId: string) => {
    const item = template?.items.find((i) => i.id === itemId);
    if (item?.responseType && item.responseType !== 'PassFail') {
      return false;
    }
    const mode = item?.responseMode ?? template?.responseMode ?? 'PFNA';
    return mode === 'PFNA';
  }, [template]);

  const isPassFailQuestion = useCallback((itemId: string) => {
    const item = template?.items.find((i) => i.id === itemId);
    return !item?.responseType || item.responseType === 'PassFail';
  }, [template]);

  const validationError = useMemo(() => {
    if (!template) return '';
    for (const item of template.items) {
      if (!item.id) continue;
      if (!isPassFailQuestion(item.id)) {
        return 'This checklist includes response types not yet supported in operator capture.';
      }
      const answer = responses[item.id];
      if (!answer) {
        return 'All checklist items must be answered.';
      }
      if (answer === 'Fail' && requiresFailNote(item.id) && !notes[item.id]?.trim()) {
        return 'A note is required for failed checklist items.';
      }
    }
    return '';
  }, [isPassFailQuestion, notes, requiresFailNote, responses, template]);

  const setResponse = (itemId: string, value: ResponseValue) => {
    setResponses((prev) => ({ ...prev, [itemId]: value }));
  };

  const handleSubmit = useCallback(async () => {
    if (!entry || !template) return;
    if (validationError) {
      setError(validationError);
      return;
    }

    setSaving(true);
    setError('');
    try {
      const payload = template.items
        .filter((item): item is ChecklistTemplate['items'][number] & { id: string } => !!item.id)
        .map((item) => ({
          checklistTemplateItemId: item.id,
          responseValue: responses[item.id],
          note: notes[item.id]?.trim() || undefined,
        }));

      await checklistApi.submitResponses(entry.id, { responses: payload });
      await checklistApi.completeEntry(entry.id);
      props.showScanResult({ type: 'success', message: 'Checklist completed' });
      props.refreshHistory();
      if (props.onChecklistCompleted) {
        props.onChecklistCompleted();
      } else {
        await loadChecklist();
        setResponses({});
        setNotes({});
      }
    } catch {
      setError('Failed to submit checklist.');
    } finally {
      setSaving(false);
    }
  }, [entry, loadChecklist, notes, props, responses, template, validationError]);

  if (loading) {
    return <div className={styles.loading}><Spinner label="Loading checklist..." /></div>;
  }

  if (!template || !entry) {
    return <div className={styles.error}>{error || 'No checklist template is configured.'}</div>;
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <h2>{template.title}</h2>
        <div className={styles.meta}>
          <span>{template.checklistType}</span>
          <span>Template {template.templateCode} v{template.versionNo}</span>
          <span>Resolved from {template.scopeLevel}</span>
        </div>
      </div>

      {template.items.map((item) => (
        <Card key={item.id} className={styles.itemCard}>
          <CardHeader header={<div className={styles.prompt}>{item.prompt}</div>} />
          {isPassFailQuestion(item.id!) ? (
            <>
              <div className={styles.responses}>
                <Button
                  className={responses[item.id!] === 'Pass' ? styles.selectedPass : ''}
                  appearance="secondary"
                  onClick={() => setResponse(item.id!, 'Pass')}
                >
                  Pass
                </Button>
                <Button
                  className={responses[item.id!] === 'Fail' ? styles.selectedFail : ''}
                  appearance="secondary"
                  onClick={() => setResponse(item.id!, 'Fail')}
                >
                  Fail
                </Button>
                {canUseNa(item.id!) && (
                  <Button
                    className={responses[item.id!] === 'N/A' ? styles.selectedNa : ''}
                    appearance="secondary"
                    onClick={() => setResponse(item.id!, 'N/A')}
                  >
                    N/A
                  </Button>
                )}
              </div>
              {responses[item.id!] === 'Fail' && (
                <div className={styles.noteWrap}>
                  <Textarea
                    value={notes[item.id!] ?? ''}
                    onChange={(_, data) => setNotes((prev) => ({ ...prev, [item.id!]: data.value }))}
                    placeholder={requiresFailNote(item.id!) ? 'Failure note required' : 'Optional note'}
                    rows={2}
                  />
                </div>
              )}
            </>
          ) : (
            <div className={styles.error}>Unsupported response type: {item.responseType}</div>
          )}
        </Card>
      ))}

      {error && <div className={styles.error}>{error}</div>}
      {validationError && !error && <div className={styles.error}>{validationError}</div>}

      <div className={styles.actions}>
        <Button appearance="primary" size="large" disabled={saving} onClick={() => void handleSubmit()}>
          {saving ? 'Submitting...' : 'Submit Checklist'}
        </Button>
      </div>
    </div>
  );
}
