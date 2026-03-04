import { useEffect, useMemo, useState } from 'react';
import { Button, Dropdown, Input, Label, Option, Switch, Textarea } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { workflowApi } from '../../api/endpoints.ts';
import type { WorkflowDefinition, WorkflowStepDefinition } from '../../types/domain.ts';
import type { UpsertWorkflowDefinitionRequest } from '../../types/api.ts';
import styles from './CardList.module.css';

const WORKFLOW_TYPES = ['HoldTag', 'Ncr'];

export function WorkflowDefinitionsScreen() {
  const [workflowType, setWorkflowType] = useState('HoldTag');
  const [items, setItems] = useState<WorkflowDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [editor, setEditor] = useState<UpsertWorkflowDefinitionRequest>({
    workflowType: 'HoldTag',
    isActive: true,
    startStepCode: 'Start',
    steps: [],
  });
  const [validation, setValidation] = useState<string[]>([]);

  const sorted = useMemo(() => [...items].sort((a, b) => b.version - a.version), [items]);

  useEffect(() => {
    setLoading(true);
    setError('');
    workflowApi.getDefinitions(workflowType)
      .then(setItems)
      .catch(() => setError('Failed to load workflow definitions.'))
      .finally(() => setLoading(false));
  }, [workflowType]);

  const addStep = () => {
    setEditor((prev) => ({
      ...prev,
      steps: [...prev.steps, {
        stepCode: `Step${prev.steps.length + 1}`,
        stepName: `Step ${prev.steps.length + 1}`,
        sequence: prev.steps.length + 1,
        requiredFields: [],
        requiredChecklistTemplateIds: [],
        approvalMode: 'None',
        approvalAssignments: [],
        allowReject: true,
      }],
    }));
  };

  const updateStep = (index: number, patch: Partial<WorkflowStepDefinition>) => {
    setEditor((prev) => ({
      ...prev,
      steps: prev.steps.map((s, i) => i === index ? { ...s, ...patch } : s),
    }));
  };

  return (
    <AdminLayout title="Workflow Definitions">
      <div style={{ display: 'grid', gap: 12, gridTemplateColumns: '1fr 1fr' }}>
        <div>
          <Label>Workflow Type</Label>
          <Dropdown value={workflowType} selectedOptions={[workflowType]} onOptionSelect={(_, d) => setWorkflowType(d.optionValue ?? 'HoldTag')}>
            {WORKFLOW_TYPES.map((t) => <Option key={t} value={t}>{t}</Option>)}
          </Dropdown>
          {loading && <div>Loading...</div>}
          {error && <div style={{ color: '#dc3545' }}>{error}</div>}
          <div className={styles.grid} style={{ marginTop: 10 }}>
            {sorted.map((item) => (
              <div key={item.id} className={styles.card}>
                <div className={styles.cardHeader}>
                  <span className={styles.cardTitle}>{item.workflowType} v{item.version}</span>
                </div>
                <div className={styles.cardField}><span className={styles.cardFieldLabel}>Start</span><span className={styles.cardFieldValue}>{item.startStepCode}</span></div>
                <div className={styles.cardField}><span className={styles.cardFieldLabel}>Active</span><span className={styles.cardFieldValue}>{item.isActive ? 'Yes' : 'No'}</span></div>
              </div>
            ))}
          </div>
        </div>

        <div>
          <Label>New Version Editor</Label>
          <Input value={editor.workflowType} onChange={(_, d) => setEditor((prev) => ({ ...prev, workflowType: d.value }))} />
          <Label>Start Step Code</Label>
          <Input value={editor.startStepCode} onChange={(_, d) => setEditor((prev) => ({ ...prev, startStepCode: d.value }))} />
          <Switch label="Active" checked={editor.isActive} onChange={(_, d) => setEditor((prev) => ({ ...prev, isActive: d.checked }))} />
          <Button onClick={addStep}>Add Step</Button>
          <div style={{ marginTop: 8, display: 'grid', gap: 8 }}>
            {editor.steps.map((step, index) => (
              <div key={`${step.stepCode}-${index}`} style={{ border: '1px solid #ddd', padding: 8 }}>
                <Input value={step.stepCode} onChange={(_, d) => updateStep(index, { stepCode: d.value })} />
                <Input value={step.stepName} onChange={(_, d) => updateStep(index, { stepName: d.value })} />
                <Dropdown value={step.approvalMode} selectedOptions={[step.approvalMode]} onOptionSelect={(_, d) => updateStep(index, { approvalMode: (d.optionValue as 'None' | 'AnyOne' | 'All') ?? 'None' })}>
                  <Option value="None">None</Option>
                  <Option value="AnyOne">AnyOne</Option>
                  <Option value="All">All</Option>
                </Dropdown>
                <Label>Assignments (CSV: role:3,user:{'{guid}'})</Label>
                <Textarea value={step.approvalAssignments.join(',')} onChange={(_, d) => updateStep(index, { approvalAssignments: d.value.split(',').map(x => x.trim()).filter(Boolean) })} />
                <Input placeholder="Approve next step code" value={step.onApproveNextStepCode ?? ''} onChange={(_, d) => updateStep(index, { onApproveNextStepCode: d.value || undefined })} />
                <Input placeholder="Reject target step code" value={step.onRejectTargetStepCode ?? ''} onChange={(_, d) => updateStep(index, { onRejectTargetStepCode: d.value || undefined })} />
              </div>
            ))}
          </div>
          {validation.length > 0 && (
            <div style={{ color: '#dc3545', marginTop: 6 }}>
              {validation.map((v) => <div key={v}>{v}</div>)}
            </div>
          )}
          <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
            <Button onClick={() => workflowApi.validateDefinition(editor).then((r) => setValidation(r.errors)).catch(() => setValidation(['Validation failed']))}>Validate</Button>
            <Button appearance="primary" onClick={() => workflowApi.upsertDefinition(editor).then(() => workflowApi.getDefinitions(workflowType).then(setItems))}>Save New Version</Button>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
