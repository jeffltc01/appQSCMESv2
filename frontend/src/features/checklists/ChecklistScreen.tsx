import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, CardHeader, Input, Spinner, Textarea } from '@fluentui/react-components';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { checklistApi } from '../../api/endpoints.ts';
import type { ChecklistEntry, ChecklistTemplate } from '../../types/domain.ts';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import styles from './ChecklistScreen.module.css';

type ChecklistScreenProps = WorkCenterProps & {
  checklistType?: string;
  onChecklistCompleted?: () => void;
};

function getErrorMessage(error: unknown): string {
  if (typeof error === 'object' && error !== null && 'message' in error) {
    const message = (error as { message?: unknown }).message;
    if (typeof message === 'string') return message;
  }
  return '';
}

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
  const [responses, setResponses] = useState<Record<string, string>>({});
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
    } catch (err) {
      const message = getErrorMessage(err).toLowerCase();
      if (message.includes('no checklist template found')) {
        setError(`Unable to load checklist template for this work center. No active ${selectedChecklistType} template is effective for this site/work center. Check checklist type, scope, effective dates, and active status.`);
      } else {
        setError('Unable to load checklist template for this work center.');
      }
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

  const validationError = useMemo(() => {
    if (!template) return '';
    for (const item of template.items) {
      if (!item.id) continue;
      const answer = responses[item.id]?.trim();
      if (!answer && item.isRequired) {
        return 'All checklist items must be answered.';
      }

      if (!answer) continue;
      if (item.responseType === 'Checkbox' && answer !== 'true' && answer !== 'false') {
        return 'Checkbox responses must be yes or no.';
      }
      if (item.responseType === 'Number' || item.responseType === 'Dimension') {
        if (Number.isNaN(Number(answer))) {
          return 'Number and Dimension responses must be numeric.';
        }
      }
      if (item.responseType === 'Datetime') {
        const date = new Date(answer);
        if (Number.isNaN(date.getTime())) {
          return 'Datetime responses must be valid date/time values.';
        }
      }
    }
    return '';
  }, [responses, template]);

  const groupedItems = useMemo(() => {
    if (!template) return [];
    const map = new Map<string, ChecklistTemplate['items']>();
    template.items.forEach((item) => {
      const key = (item.section ?? '').trim();
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    });
    const keys = Array.from(map.keys()).sort((a, b) => {
      if (!a) return -1;
      if (!b) return 1;
      return a.localeCompare(b);
    });
    return keys.map((key) => ({
      section: key,
      items: map.get(key)!,
    }));
  }, [template]);

  const setResponse = (itemId: string, value: string) => {
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
      }
    } catch {
      setError('Failed to submit checklist.');
    } finally {
      setSaving(false);
    }
  }, [entry, loadChecklist, props, responses, template, validationError]);

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

      {groupedItems.map((group) => (
        <div key={group.section || 'unsectioned'}>
          <h3>{group.section || 'Unsectioned'}</h3>
          {group.items.map((item) => (
            <Card key={item.id} className={styles.itemCard}>
              <CardHeader header={<div className={styles.prompt}>{item.prompt}</div>} />
              {item.responseType === 'Checkbox' && (
                <div className={styles.responses}>
                  <Button
                    className={responses[item.id!] === 'true' ? styles.selectedPass : ''}
                    appearance="secondary"
                    onClick={() => setResponse(item.id!, 'true')}
                  >
                    Yes
                  </Button>
                  <Button
                    className={responses[item.id!] === 'false' ? styles.selectedFail : ''}
                    appearance="secondary"
                    onClick={() => setResponse(item.id!, 'false')}
                  >
                    No
                  </Button>
                </div>
              )}
              {item.responseType === 'Datetime' && (
                <Input
                  type="datetime-local"
                  value={responses[item.id!] ?? ''}
                  onChange={(_, data) => setResponse(item.id!, data.value)}
                />
              )}
              {item.responseType === 'Number' && (
                <Input
                  type="number"
                  value={responses[item.id!] ?? ''}
                  onChange={(_, data) => setResponse(item.id!, data.value)}
                />
              )}
              {item.responseType === 'Image' && (
                <Textarea
                  value={responses[item.id!] ?? ''}
                  onChange={(_, data) => setResponse(item.id!, data.value)}
                  rows={2}
                  placeholder="Enter image attachment reference"
                />
              )}
              {item.responseType === 'Dimension' && (
                <div>
                  <Input
                    type="number"
                    value={responses[item.id!] ?? ''}
                    onChange={(_, data) => setResponse(item.id!, data.value)}
                  />
                  <div className={styles.meta}>
                    Target: {item.dimensionTarget} | Lower: {item.dimensionLowerLimit} | Upper: {item.dimensionUpperLimit} | Unit: {item.dimensionUnitOfMeasure}
                  </div>
                </div>
              )}
              {item.responseType === 'Score' && (
                <div className={`${styles.responses} ${styles.scoreResponses}`}>
                  {(item.scoreOptions ?? []).map((option) => {
                    const optionValue = option.id ?? option.description;
                    const isSelected = responses[item.id!] === optionValue;
                    return (
                      <Button
                        key={option.id ?? `${option.sortOrder}-${option.score}`}
                        className={`${styles.scoreResponseButton} ${isSelected ? styles.selectedScore : ''}`.trim()}
                        appearance={isSelected ? 'primary' : 'secondary'}
                        onClick={() => setResponse(item.id!, optionValue)}
                      >
                        {option.description}
                      </Button>
                    );
                  })}
                </div>
              )}
              {!item.responseType && <div className={styles.error}>Missing response type.</div>}
            </Card>
          ))}
        </div>
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
