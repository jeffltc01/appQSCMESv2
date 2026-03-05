import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Input,
  Label,
  Option,
  Switch,
  Textarea,
} from '@fluentui/react-components';
import ReactFlow, {
  Background,
  Controls,
  Handle,
  MarkerType,
  MiniMap,
  Position,
  useNodesState,
  type Edge,
  type Node,
  type NodeTypes,
} from 'reactflow';
import 'reactflow/dist/style.css';
import { AdminLayout } from './AdminLayout.tsx';
import { workflowApi } from '../../api/endpoints.ts';
import type { WorkflowDefinition } from '../../types/domain.ts';
import type { UpsertWorkflowDefinitionRequest, WorkflowStepDefinitionRequest } from '../../types/api.ts';
import styles from './WorkflowDefinitionsScreen.module.css';

const WORKFLOW_TYPES = ['HoldTag', 'Ncr'];
const STEP_SPACING_X = 270;
const STEP_BASE_X = 60;
const STEP_BASE_Y = 160;
type StepLibraryType = 'Standard' | 'Approval' | 'Terminal';

type WorkflowBuilderNodeData = {
  stepCode: string;
  stepName: string;
  approvalMode: string;
  isApproval: boolean;
  isStart: boolean;
  isTerminal: boolean;
  isSelected: boolean;
};

function WorkflowBuilderNode({ data }: { data: WorkflowBuilderNodeData }) {
  if (data.isApproval) {
    return (
      <div className={`${styles.canvasDecisionWrap} ${data.isSelected ? styles.canvasNodeSelected : ''}`}>
        <Handle type="target" position={Position.Left} className={styles.nodeHandle} />
        <Handle id="approve" type="source" position={Position.Right} className={styles.nodeHandle} />
        <Handle id="reject" type="source" position={Position.Bottom} className={styles.nodeHandleReject} />
        <div className={`${styles.canvasDecision} ${data.isTerminal ? styles.canvasNodeTerminal : ''}`}>
          <div className={styles.canvasDecisionInner}>
            <div className={styles.canvasNodeHeader}>
              <span className={styles.canvasNodeTitle}>{data.stepCode}</span>
              {data.isStart && <span className={styles.nodeBadge}>Start</span>}
            </div>
            <div className={styles.canvasNodeSub}>{data.stepName}</div>
            <div className={styles.canvasNodeMeta}>Decision: {data.approvalMode}</div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`${styles.canvasNode} ${data.isSelected ? styles.canvasNodeSelected : ''} ${data.isTerminal ? styles.canvasNodeTerminal : ''}`}>
      <Handle type="target" position={Position.Left} className={styles.nodeHandle} />
      <div className={styles.canvasNodeHeader}>
        <span className={styles.canvasNodeTitle}>{data.stepCode}</span>
        {data.isStart && <span className={styles.nodeBadge}>Start</span>}
      </div>
      <div className={styles.canvasNodeSub}>{data.stepName}</div>
      <div className={styles.canvasNodeMeta}>Approval: {data.approvalMode}</div>
      <Handle type="source" position={Position.Right} className={styles.nodeHandle} />
    </div>
  );
}

const nodeTypes: NodeTypes = {
  workflowNode: WorkflowBuilderNode,
};

export function WorkflowDefinitionsScreen() {
  const navigate = useNavigate();
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
  const [sourceDefinitionId, setSourceDefinitionId] = useState<string>('');
  const [validation, setValidation] = useState<string[]>([]);
  const [saveError, setSaveError] = useState('');
  const [saveSuccess, setSaveSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [isPickerOpen, setIsPickerOpen] = useState(true);
  const [selectedStepIndex, setSelectedStepIndex] = useState<number>(0);
  const [draftPositionKey, setDraftPositionKey] = useState<string>('workflow-draft:new:HoldTag');
  const [nodes, setNodes, onNodesChange] = useNodesState<WorkflowBuilderNodeData>([]);

  const sorted = useMemo(() => [...items].sort((a, b) => b.version - a.version), [items]);
  const selectableWorkflowTypes = useMemo(
    () => Array.from(new Set([...WORKFLOW_TYPES, ...items.map((x) => x.workflowType)])).sort(),
    [items],
  );
  const definedStepCodes = useMemo(
    () => Array.from(new Set(editor.steps.map((s) => s.stepCode.trim()).filter(Boolean))),
    [editor.steps],
  );

  useEffect(() => {
    setLoading(true);
    setError('');
    workflowApi.getDefinitions(workflowType)
      .then(setItems)
      .catch(() => setError('Failed to load workflow definitions.'))
      .finally(() => setLoading(false));
  }, [workflowType]);

  const resetEditor = (selectedWorkflowType: string) => {
    setSourceDefinitionId('');
    setDraftPositionKey(`workflow-draft:new:${selectedWorkflowType}`);
    setEditor({
      workflowType: selectedWorkflowType,
      isActive: true,
      startStepCode: 'Start',
      steps: [],
    });
    setSelectedStepIndex(0);
    setValidation([]);
    setSaveError('');
    setSaveSuccess('');
  };

  const toEditorStep = (step: WorkflowDefinition['steps'][number], sequence: number): WorkflowStepDefinitionRequest => ({
    stepCode: step.stepCode,
    stepName: step.stepName,
    sequence,
    requiredFields: [...step.requiredFields],
    requiredChecklistTemplateIds: [...step.requiredChecklistTemplateIds],
    approvalMode: step.approvalMode,
    approvalAssignments: [...step.approvalAssignments],
    allowReject: step.allowReject,
    onApproveNextStepCode: step.onApproveNextStepCode,
    onRejectTargetStepCode: step.onRejectTargetStepCode,
  });

  const loadDefinition = (definition: WorkflowDefinition, draftActive: boolean) => {
    const normalizedSteps = [...definition.steps]
      .sort((a, b) => a.sequence - b.sequence)
      .map((step, index) => toEditorStep(step, index + 1));

    setSourceDefinitionId(definition.id);
    setDraftPositionKey(`workflow-draft:source:${definition.id}`);
    setEditor({
      sourceDefinitionIdForNewVersion: definition.id,
      workflowType: definition.workflowType,
      isActive: draftActive,
      startStepCode: definition.startStepCode,
      steps: normalizedSteps,
    });
    setSelectedStepIndex(0);
    setValidation([]);
    setSaveError('');
    setSaveSuccess('');
  };

  const addStep = (stepType: StepLibraryType = 'Standard') => {
    let newIndex = 0;
    setEditor((prev) => {
      newIndex = prev.steps.length;
      const nextSequence = prev.steps.length + 1;
      const isApproval = stepType === 'Approval';
      const isTerminal = stepType === 'Terminal';
      const nextStepCodePrefix = isApproval ? 'Approval' : isTerminal ? 'Terminal' : 'Step';
      return {
        ...prev,
        steps: [...prev.steps, {
          stepCode: `${nextStepCodePrefix}${nextSequence}`,
          stepName: `${stepType} ${nextSequence}`,
          sequence: nextSequence,
          requiredFields: [],
          requiredChecklistTemplateIds: [],
          approvalMode: isApproval ? 'AnyOne' : 'None',
          approvalAssignments: [],
          allowReject: isApproval,
          onApproveNextStepCode: isTerminal ? undefined : undefined,
          onRejectTargetStepCode: undefined,
        }],
      };
    });
    setSelectedStepIndex(newIndex);
  };

  const updateStep = (index: number, patch: Partial<WorkflowStepDefinitionRequest>) => {
    setEditor((prev) => ({
      ...prev,
      steps: prev.steps.map((s, i) => i === index ? { ...s, ...patch } : s),
    }));
  };

  const removeStep = (index: number) => {
    setEditor((prev) => ({
      ...prev,
      steps: prev.steps
        .filter((_, i) => i !== index)
        .map((step, i) => ({ ...step, sequence: i + 1 })),
    }));
    setSelectedStepIndex((prev) => Math.max(0, Math.min(prev, editor.steps.length - 2)));
  };

  const moveStep = (index: number, direction: -1 | 1) => {
    setEditor((prev) => {
      const targetIndex = index + direction;
      if (targetIndex < 0 || targetIndex >= prev.steps.length) {
        return prev;
      }

      const updated = [...prev.steps];
      const [step] = updated.splice(index, 1);
      updated.splice(targetIndex, 0, step);

      return {
        ...prev,
        steps: updated.map((s, i) => ({ ...s, sequence: i + 1 })),
      };
    });
    setSelectedStepIndex(index + direction);
  };

  const validateDefinition = async () => {
    setValidation([]);
    setSaveError('');
    setSaveSuccess('');
    try {
      const result = await workflowApi.validateDefinition(editor);
      setValidation(result.errors);
      if (result.isExecutable) {
        setSaveSuccess('Validation passed. Workflow is executable.');
      }
    } catch {
      setValidation(['Validation failed']);
    }
  };

  const saveDefinition = async () => {
    setSaving(true);
    setSaveError('');
    setSaveSuccess('');
    setValidation([]);
    try {
      await workflowApi.upsertDefinition(editor);
      const refreshed = await workflowApi.getDefinitions(workflowType);
      setItems(refreshed);
      setSaveSuccess('Workflow definition version saved.');
    } catch {
      setSaveError('Failed to save workflow definition.');
    } finally {
      setSaving(false);
    }
  };

  const selectedStep = editor.steps[selectedStepIndex];

  const graph = useMemo(() => {
    const codeToNodeId = new Map<string, string>();
    const nodes: Node<WorkflowBuilderNodeData>[] = editor.steps.map((step, index) => {
      const stepKey = step.stepCode.trim() || `idx-${index + 1}`;
      const nodeId = `step-${stepKey}`;
      if (!codeToNodeId.has(step.stepCode)) {
        codeToNodeId.set(step.stepCode, nodeId);
      }

      return {
        id: nodeId,
        type: 'workflowNode',
        position: { x: STEP_BASE_X + index * STEP_SPACING_X, y: STEP_BASE_Y },
        data: {
          stepCode: step.stepCode,
          stepName: step.stepName,
          approvalMode: step.approvalMode,
          isApproval: step.approvalMode !== 'None',
          isStart: editor.startStepCode === step.stepCode,
          isTerminal: !step.onApproveNextStepCode,
          isSelected: selectedStepIndex === index,
        },
      };
    });

    const edges: Edge[] = [];
    editor.steps.forEach((step, index) => {
      const sourceStepKey = step.stepCode.trim() || `idx-${index + 1}`;
      const sourceId = `step-${sourceStepKey}`;

      if (step.onApproveNextStepCode && codeToNodeId.has(step.onApproveNextStepCode)) {
        edges.push({
          id: `${sourceId}-approve-${step.onApproveNextStepCode}`,
          source: sourceId,
          sourceHandle: step.approvalMode !== 'None' ? 'approve' : undefined,
          target: codeToNodeId.get(step.onApproveNextStepCode)!,
          label: 'approve',
          type: 'smoothstep',
          markerEnd: { type: MarkerType.ArrowClosed, color: '#2b3b84' },
          style: { stroke: '#2b3b84', strokeWidth: 2 },
          labelStyle: { fill: '#2b3b84', fontWeight: 600, fontSize: 11 },
        });
      }

      if (step.onRejectTargetStepCode && codeToNodeId.has(step.onRejectTargetStepCode)) {
        edges.push({
          id: `${sourceId}-reject-${step.onRejectTargetStepCode}`,
          source: sourceId,
          sourceHandle: 'reject',
          target: codeToNodeId.get(step.onRejectTargetStepCode)!,
          label: 'reject',
          type: 'smoothstep',
          markerEnd: { type: MarkerType.ArrowClosed, color: '#dc3545' },
          style: { stroke: '#dc3545', strokeWidth: 2, strokeDasharray: '6 4' },
          labelStyle: { fill: '#dc3545', fontWeight: 600, fontSize: 11 },
        });
      }
    });

    return { nodes, edges };
  }, [editor.steps, editor.startStepCode, selectedStepIndex]);

  useEffect(() => {
    const previousPositionById = new Map(nodes.map((n) => [n.id, n.position]));
    const persistedPositionById = new Map<string, { x: number; y: number }>();

    try {
      const raw = localStorage.getItem(draftPositionKey);
      if (raw) {
        const parsed = JSON.parse(raw) as Record<string, { x: number; y: number }>;
        Object.entries(parsed).forEach(([id, position]) => {
          if (typeof position?.x === 'number' && typeof position?.y === 'number') {
            persistedPositionById.set(id, position);
          }
        });
      }
    } catch {
      // ignore malformed localStorage values
    }

    const mergedNodes = graph.nodes.map((node) => ({
      ...node,
      position: previousPositionById.get(node.id) ?? persistedPositionById.get(node.id) ?? node.position,
    }));
    setNodes(mergedNodes);
  }, [graph.nodes, draftPositionKey, setNodes]);

  const persistNodePositions = (currentNodes: Node<WorkflowBuilderNodeData>[]) => {
    try {
      const map = Object.fromEntries(currentNodes.map((node) => [node.id, node.position]));
      localStorage.setItem(draftPositionKey, JSON.stringify(map));
    } catch {
      // ignore storage quota or availability errors
    }
  };

  const onLibraryDragStart = (event: React.DragEvent<HTMLDivElement>, stepType: StepLibraryType) => {
    event.dataTransfer.setData('application/workflow-step-type', stepType);
    event.dataTransfer.effectAllowed = 'copy';
  };

  const onCanvasDragOver = (event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
  };

  const onCanvasDrop = (event: React.DragEvent) => {
    event.preventDefault();
    const stepType = event.dataTransfer.getData('application/workflow-step-type') as StepLibraryType | '';
    if (!stepType) return;
    addStep(stepType);
  };

  const closeScreen = () => {
    navigate('/menu');
  };

  return (
    <AdminLayout title="Workflow Definitions">
      <div className={styles.builderShell}>
        <div className={styles.topToolbar}>
          <div className={styles.topToolbarLeft}>
            <div className={styles.toolbarField}>
              <Label>Start Step</Label>
              <Dropdown
                value={editor.startStepCode}
                selectedOptions={[editor.startStepCode]}
                onOptionSelect={(_, d) => setEditor((prev) => ({ ...prev, startStepCode: d.optionValue ?? prev.startStepCode }))}
                disabled={definedStepCodes.length === 0}
              >
                {definedStepCodes.map((code) => <Option key={code} value={code}>{code}</Option>)}
              </Dropdown>
            </div>
            <Switch label="Active" checked={editor.isActive} onChange={(_, d) => setEditor((prev) => ({ ...prev, isActive: d.checked }))} />
          </div>
          <div className={styles.topToolbarActions}>
            <Button onClick={() => setIsPickerOpen(true)}>Select Workflow</Button>
            <Button onClick={validateDefinition} disabled={saving}>Validate</Button>
            <Button appearance="primary" onClick={saveDefinition} disabled={saving}>Save New Version</Button>
          </div>
        </div>

        {loading && <div>Loading...</div>}
        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.builderGrid}>
          <aside className={styles.leftPanel}>
            <div className={styles.panelTitle}>Step Library</div>
            <Button appearance="primary" onClick={() => addStep('Standard')}>+ Add Step</Button>
            <div className={styles.libraryTypes}>
              <div
                className={styles.libraryType}
                draggable
                onDragStart={(event) => onLibraryDragStart(event, 'Standard')}
                title="Drag onto canvas to add Standard step"
              >
                Standard
              </div>
              <div
                className={styles.libraryType}
                draggable
                onDragStart={(event) => onLibraryDragStart(event, 'Approval')}
                title="Drag onto canvas to add Approval step"
              >
                Approval (AnyOne/All)
              </div>
              <div
                className={styles.libraryType}
                draggable
                onDragStart={(event) => onLibraryDragStart(event, 'Terminal')}
                title="Drag onto canvas to add Terminal step"
              >
                Terminal
              </div>
            </div>
            <div className={styles.stepList}>
              {editor.steps.map((step, idx) => (
                <button
                  key={`${step.stepCode}-${idx}`}
                  type="button"
                  className={`${styles.stepListItem} ${idx === selectedStepIndex ? styles.stepListItemActive : ''}`}
                  onClick={() => setSelectedStepIndex(idx)}
                >
                  <span>{step.stepCode}</span>
                  <span className={styles.stepListMeta}>{step.approvalMode}</span>
                </button>
              ))}
            </div>
          </aside>

          <section className={styles.canvasPanel}>
            <ReactFlow
              nodes={nodes}
              edges={graph.edges}
              nodeTypes={nodeTypes}
              fitView
              onDrop={onCanvasDrop}
              onDragOver={onCanvasDragOver}
              onNodesChange={onNodesChange}
              onNodeDragStop={(_, draggedNode) => {
                const nextNodes = nodes.map((node) => node.id === draggedNode.id ? { ...node, position: draggedNode.position } : node);
                setNodes(nextNodes);
                persistNodePositions(nextNodes);
              }}
              onNodeClick={(_, node) => {
                const stepIndex = editor.steps.findIndex((step, idx) => {
                  const stepKey = step.stepCode.trim() || `idx-${idx + 1}`;
                  return `step-${stepKey}` === node.id;
                });
                if (stepIndex >= 0) setSelectedStepIndex(stepIndex);
              }}
            >
              <MiniMap zoomable pannable />
              <Controls />
              <Background gap={12} size={1} />
            </ReactFlow>
            <div className={styles.statusBar}>No errors</div>
          </section>

          <aside className={styles.rightPanel}>
            <div className={styles.panelTitle}>Selected Step Properties</div>
            {!selectedStep ? (
              <div className={styles.emptyState}>Select or add a step to edit properties.</div>
            ) : (
              <div className={styles.propertiesForm}>
                {sourceDefinitionId && (
                  <div className={styles.sourceText}>
                    Based on definition: <code>{sourceDefinitionId}</code>
                  </div>
                )}
                <Label>Step Code</Label>
                <Input value={selectedStep.stepCode} onChange={(_, d) => updateStep(selectedStepIndex, { stepCode: d.value })} />
                <Label>Step Name</Label>
                <Input value={selectedStep.stepName} onChange={(_, d) => updateStep(selectedStepIndex, { stepName: d.value })} />
                <Label>Sequence</Label>
                <Input value={String(selectedStep.sequence)} readOnly />

                <div className={styles.inlineActions}>
                  <Button size="small" onClick={() => moveStep(selectedStepIndex, -1)} disabled={selectedStepIndex === 0}>Move Up</Button>
                  <Button size="small" onClick={() => moveStep(selectedStepIndex, 1)} disabled={selectedStepIndex === editor.steps.length - 1}>Move Down</Button>
                  <Button size="small" onClick={() => removeStep(selectedStepIndex)}>Remove</Button>
                </div>

                <Label>Approval Mode</Label>
                <Dropdown
                  value={selectedStep.approvalMode}
                  selectedOptions={[selectedStep.approvalMode]}
                  onOptionSelect={(_, d) => updateStep(selectedStepIndex, { approvalMode: (d.optionValue as 'None' | 'AnyOne' | 'All') ?? 'None' })}
                >
                  <Option value="None">None</Option>
                  <Option value="AnyOne">AnyOne</Option>
                  <Option value="All">All</Option>
                </Dropdown>
                <Label>Assignments (CSV: role:3,user:{'{guid}'})</Label>
                <Textarea
                  value={selectedStep.approvalAssignments.join(',')}
                  onChange={(_, d) => updateStep(selectedStepIndex, { approvalAssignments: d.value.split(',').map((x) => x.trim()).filter(Boolean) })}
                />
                <Label>Approve Next Step</Label>
                <Dropdown
                  value={selectedStep.onApproveNextStepCode ?? '__terminal__'}
                  selectedOptions={[selectedStep.onApproveNextStepCode ?? '__terminal__']}
                  onOptionSelect={(_, d) => updateStep(selectedStepIndex, { onApproveNextStepCode: d.optionValue === '__terminal__' ? undefined : d.optionValue })}
                  disabled={definedStepCodes.length === 0}
                >
                  <Option value="__terminal__">(Complete / Terminal)</Option>
                  {definedStepCodes.map((code) => <Option key={`approve-${selectedStepIndex}-${code}`} value={code}>{code}</Option>)}
                </Dropdown>
                <Label>Reject Target Step</Label>
                <Dropdown
                  value={selectedStep.onRejectTargetStepCode ?? '__none__'}
                  selectedOptions={[selectedStep.onRejectTargetStepCode ?? '__none__']}
                  onOptionSelect={(_, d) => updateStep(selectedStepIndex, { onRejectTargetStepCode: d.optionValue === '__none__' ? undefined : d.optionValue })}
                  disabled={definedStepCodes.length === 0}
                >
                  <Option value="__none__">(None / Stay Rejected)</Option>
                  {definedStepCodes.map((code) => <Option key={`reject-${selectedStepIndex}-${code}`} value={code}>{code}</Option>)}
                </Dropdown>
              </div>
            )}
            {validation.length > 0 && (
              <div className={styles.errorList}>
                {validation.map((v) => <div key={v}>{v}</div>)}
              </div>
            )}
            {saveError && <div className={styles.error}>{saveError}</div>}
            {saveSuccess && <div className={styles.success}>{saveSuccess}</div>}
          </aside>
        </div>
      </div>

      <Dialog open={isPickerOpen} onOpenChange={(_, data) => setIsPickerOpen(data.open)}>
        <DialogSurface className={styles.workflowPickerSurface}>
          <DialogBody>
            <DialogTitle>Select Workflow Definition</DialogTitle>
            <DialogContent className={styles.workflowPickerContent}>
              <div className={styles.workflowPickerLayout}>
                <div className={styles.workflowPickerToolbarSection}>
                  <Label>Workflow Type</Label>
                  <Dropdown
                    className={styles.workflowPickerTypeDropdown}
                    value={workflowType}
                    selectedOptions={[workflowType]}
                    onOptionSelect={(_, d) => {
                      const next = d.optionValue ?? 'HoldTag';
                      setWorkflowType(next);
                      resetEditor(next);
                    }}
                  >
                    {selectableWorkflowTypes.map((t) => <Option key={`picker-${t}`} value={t}>{t}</Option>)}
                  </Dropdown>
                </div>

                {loading && <div>Loading definitions...</div>}
                {error && <div className={styles.error}>{error}</div>}
                {!loading && !error && sorted.length === 0 && (
                  <div className={styles.emptyState}>No workflow definitions are available for {workflowType}.</div>
                )}
                {!loading && !error && sorted.length > 0 && (
                  <div className={styles.definitionListScroll}>
                    <div className={styles.definitionCards}>
                      {sorted.map((item) => (
                        <div key={item.id} className={styles.definitionCard}>
                          <div className={styles.definitionTitle}>{item.workflowType} v{item.version}</div>
                          <div className={styles.definitionMeta}>Start: {item.startStepCode}</div>
                          <div className={styles.definitionMeta}>Active: {item.isActive ? 'Yes' : 'No'}</div>
                          <div className={styles.definitionActions}>
                            <Button
                              size="small"
                              onClick={() => {
                                loadDefinition(item, true);
                                setIsPickerOpen(false);
                              }}
                            >
                              Edit As New Version
                            </Button>
                            <Button
                              size="small"
                              onClick={() => {
                                loadDefinition(item, false);
                                setIsPickerOpen(false);
                              }}
                            >
                              Clone As Draft
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </DialogContent>
            <DialogActions>
              <Button
                onClick={() => {
                  resetEditor(workflowType);
                  setIsPickerOpen(false);
                }}
              >
                New Empty Draft
              </Button>
              <Button onClick={closeScreen}>Close</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </AdminLayout>
  );
}
