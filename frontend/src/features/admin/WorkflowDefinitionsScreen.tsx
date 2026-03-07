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
import { adminUserApi, workflowApi } from '../../api/endpoints.ts';
import type { AdminUser, NotificationRule, RoleOption, WorkflowDefinition } from '../../types/domain.ts';
import type { NotificationRuleRequest, UpsertWorkflowDefinitionRequest, WorkflowStepDefinitionRequest } from '../../types/api.ts';
import styles from './WorkflowDefinitionsScreen.module.css';

const WORKFLOW_TYPES = ['HoldTag', 'Ncr'];
const NOTIFICATION_TRIGGER_EVENTS = ['Created', 'StepEntered', 'SubmittedForApproval', 'Rejected', 'Completed'];
const STEP_SCOPED_NOTIFICATION_EVENTS = new Set(['StepEntered', 'SubmittedForApproval', 'Rejected']);
const NOTIFICATION_RECIPIENT_MODES: NotificationRuleRequest['recipientMode'][] = ['Users', 'Roles', 'Resolvers'];
const NOTIFICATION_CLEAR_POLICIES: NotificationRuleRequest['clearPolicy'][] = ['None', 'OnEntityComplete', 'OnStepExit', 'Manual'];
const RESOLVER_KEY_OPTIONS = [
  {
    key: 'SiteQualityUsers',
    label: 'Site Quality Users',
    description: 'Resolves to quality users for the current site context.',
  },
  {
    key: 'SiteTeamLeads',
    label: 'Site Team Leads',
    description: 'Resolves to team leads for the current site or line context.',
  },
  {
    key: 'WorkflowSubmitter',
    label: 'Workflow Submitter',
    description: 'Resolves to the user who submitted or created the workflow entity.',
  },
  {
    key: 'CurrentStepApprovers',
    label: 'Current Step Approvers',
    description: 'Resolves to users assigned as approvers for the active step.',
  },
  {
    key: 'NcrCoordinator',
    label: 'NCR Coordinator',
    description: 'Resolves to the coordinator assigned on the NCR record.',
  },
] as const;
const STEP_SPACING_X = 270;
const STEP_BASE_X = 60;
const STEP_BASE_Y = 160;
const NOTIFICATION_BASE_Y = 420;
const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
type StepLibraryType = 'Standard' | 'Approval' | 'Terminal' | 'Notification';

type WorkflowStepNodeData = {
  kind: 'step';
  stepCode: string;
  stepName: string;
  approvalMode: string;
  isApproval: boolean;
  isStart: boolean;
  isTerminal: boolean;
  isSelected: boolean;
};

type WorkflowNotificationNodeData = {
  kind: 'notification';
  triggerEvent: string;
  recipientSummary: string;
  isSelected: boolean;
};

type WorkflowBuilderNodeData = WorkflowStepNodeData | WorkflowNotificationNodeData;

function WorkflowBuilderNode({ data }: { data: WorkflowBuilderNodeData }) {
  if (data.kind === 'notification') {
    return (
      <div className={`${styles.canvasNotificationNode} ${data.isSelected ? styles.canvasNodeSelected : ''}`}>
      <Handle type="target" position={Position.Left} className={styles.nodeHandleNotification} />
        <div className={styles.canvasNodeHeader}>
          <span className={styles.canvasNodeTitle}>{data.triggerEvent}</span>
          <span className={styles.nodeBadge}>Notify</span>
        </div>
        <div className={styles.canvasNodeSub}>{data.recipientSummary}</div>
      </div>
    );
  }

  if (data.isApproval) {
    return (
      <div className={`${styles.canvasDecisionWrap} ${data.isSelected ? styles.canvasNodeSelected : ''}`}>
        <Handle type="target" position={Position.Left} className={styles.nodeHandle} />
        <Handle id="approve" type="source" position={Position.Right} className={styles.nodeHandle} />
        <Handle id="reject" type="source" position={Position.Bottom} className={styles.nodeHandleReject} />
        <div className={`${styles.canvasDecision} ${data.isTerminal ? styles.canvasNodeTerminal : ''} ${data.isStart ? styles.canvasDecisionStart : ''}`}>
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
    <div className={`${styles.canvasNode} ${data.isSelected ? styles.canvasNodeSelected : ''} ${data.isTerminal ? styles.canvasNodeTerminal : ''} ${data.isStart ? styles.canvasNodeStart : ''}`}>
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

type NotificationRuleDraft = NotificationRuleRequest & { id: string };

function createClientNotificationId(): string {
  if (typeof globalThis.crypto?.randomUUID === 'function') {
    return `temp-${globalThis.crypto.randomUUID()}`;
  }

  return `temp-${Date.now()}-${Math.round(Math.random() * 100000)}`;
}

function isGuid(value: string): boolean {
  return GUID_REGEX.test(value);
}

const emptyNotificationRule = (workflowType: string): NotificationRuleDraft => ({
  id: createClientNotificationId(),
  workflowType,
  triggerEvent: 'Created',
  targetStepCodes: [],
  recipientMode: 'Roles',
  recipientConfigJson: '[]',
  templateKey: '',
  templateTitle: '',
  templateBody: '',
  clearPolicy: 'OnEntityComplete',
  isActive: true,
});

const emptyRecipientConfigJson = '[]';

function parseRecipientConfigValues(recipientConfigJson: string): string[] {
  if (!recipientConfigJson.trim()) return [];
  try {
    const parsed = JSON.parse(recipientConfigJson);
    if (Array.isArray(parsed)) {
      return parsed.map((value) => String(value).trim()).filter(Boolean);
    }
    return [];
  } catch {
    return [];
  }
}

function getNotificationRecipientSummary(rule: NotificationRuleDraft): string {
  const count = parseRecipientConfigValues(rule.recipientConfigJson).length;
  const noun = rule.recipientMode === 'Users'
    ? (count === 1 ? 'user' : 'users')
    : rule.recipientMode === 'Roles'
      ? (count === 1 ? 'role' : 'roles')
      : (count === 1 ? 'resolver' : 'resolvers');
  return `${rule.recipientMode} · ${count} ${noun}`;
}

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
  const [notificationRules, setNotificationRules] = useState<NotificationRuleDraft[]>([]);
  const [loadedNotificationRules, setLoadedNotificationRules] = useState<NotificationRule[]>([]);
  const [selectedNotificationIndex, setSelectedNotificationIndex] = useState<number>(0);
  const [adminUsers, setAdminUsers] = useState<AdminUser[]>([]);
  const [roleOptions, setRoleOptions] = useState<RoleOption[]>([]);
  const [notificationLookupError, setNotificationLookupError] = useState('');
  const [isPickerOpen, setIsPickerOpen] = useState(true);
  const [selectedPanel, setSelectedPanel] = useState<'step' | 'notification'>('step');
  const [selectedStepIndex, setSelectedStepIndex] = useState<number>(0);
  const [draftPositionKey, setDraftPositionKey] = useState<string>('workflow-draft:new:HoldTag');
  const [nodes, setNodes, onNodesChange] = useNodesState<WorkflowBuilderNodeData>([]);
  const [nodePositionOverrides, setNodePositionOverrides] = useState<Record<string, { x: number; y: number }>>({});

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
    Promise.all([
      workflowApi.getDefinitions(workflowType),
      workflowApi.getNotificationRules(workflowType),
    ])
      .then(([definitions, rules]) => {
        setItems(definitions);
        setLoadedNotificationRules(rules);
        setNotificationRules(rules.map(toNotificationDraft));
        setSelectedNotificationIndex(0);
        setSelectedPanel('step');
        setNodePositionOverrides({});
      })
      .catch(() => setError('Failed to load workflow definitions.'))
      .finally(() => setLoading(false));
  }, [workflowType]);

  useEffect(() => {
    adminUserApi.getAll(undefined, [])
      .then(setAdminUsers)
      .catch(() => setNotificationLookupError('Unable to load user list for notification recipients.'));
    adminUserApi.getRoles()
      .then(setRoleOptions)
      .catch(() => setNotificationLookupError('Unable to load role list for notification recipients.'));
  }, []);

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
    setSelectedPanel('step');
    setValidation([]);
    setSaveError('');
    setSaveSuccess('');
    setNotificationRules(loadedNotificationRules
      .filter((rule) => rule.workflowType === selectedWorkflowType)
      .map(toNotificationDraft));
    setSelectedNotificationIndex(0);
    setNodePositionOverrides({});
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

  const toNotificationDraft = (rule: NotificationRule): NotificationRuleDraft => ({
    id: rule.id,
    workflowType: rule.workflowType,
    triggerEvent: rule.triggerEvent,
    targetStepCodes: Array.isArray(rule.targetStepCodes) ? rule.targetStepCodes : [],
    recipientMode: (NOTIFICATION_RECIPIENT_MODES.includes(rule.recipientMode as NotificationRuleRequest['recipientMode'])
      ? (rule.recipientMode as NotificationRuleRequest['recipientMode'])
      : 'Roles'),
    recipientConfigJson: rule.recipientConfigJson,
    templateKey: rule.templateKey,
    templateTitle: rule.templateTitle ?? '',
    templateBody: rule.templateBody ?? '',
    clearPolicy: (NOTIFICATION_CLEAR_POLICIES.includes(rule.clearPolicy as NotificationRuleRequest['clearPolicy'])
      ? (rule.clearPolicy as NotificationRuleRequest['clearPolicy'])
      : 'None'),
    isActive: rule.isActive,
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
    setSelectedPanel('step');
    setValidation([]);
    setSaveError('');
    setSaveSuccess('');
    setNotificationRules(loadedNotificationRules
      .filter((rule) => rule.workflowType === definition.workflowType)
      .map(toNotificationDraft));
    setSelectedNotificationIndex(0);
    setNodePositionOverrides({});
  };

  const addStep = (stepType: StepLibraryType = 'Standard') => {
    if (stepType === 'Notification') {
      addNotificationRule();
      return;
    }

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
    setSelectedPanel('step');
  };

  const updateStep = (index: number, patch: Partial<WorkflowStepDefinitionRequest>) => {
    setEditor((prev) => ({
      ...prev,
      steps: prev.steps.map((s, i) => i === index ? { ...s, ...patch } : s),
    }));
  };

  const removeStep = (index: number) => {
    const removedStepCode = editor.steps[index]?.stepCode?.trim();
    setEditor((prev) => ({
      ...prev,
      steps: prev.steps
        .filter((_, i) => i !== index)
        .map((step, i) => ({ ...step, sequence: i + 1 })),
    }));
    if (removedStepCode) {
      setNotificationRules((prev) => prev.map((rule) => ({
        ...rule,
        targetStepCodes: rule.targetStepCodes.filter((code) => code !== removedStepCode),
      })));
    }
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

  const addNotificationRule = (position?: { x: number; y: number }) => {
    const newRule = emptyNotificationRule(editor.workflowType);
    setNotificationRules((prev) => {
      const next = [...prev, newRule];
      setSelectedNotificationIndex(next.length - 1);
      setSelectedPanel('notification');
      return next;
    });
    if (position) {
      setNodePositionOverrides((prev) => ({
        ...prev,
        [`notification-${newRule.id}`]: position,
      }));
    }
  };

  const updateNotificationRule = (index: number, patch: Partial<NotificationRuleDraft>) => {
    setNotificationRules((prev) => prev.map((rule, i) => (i === index ? { ...rule, ...patch } : rule)));
  };

  const updateNotificationRecipientValues = (index: number, values: string[]) => {
    updateNotificationRule(index, {
      recipientConfigJson: JSON.stringify(values),
    });
  };

  const removeNotificationRule = (index: number) => {
    const removedRuleId = notificationRules[index]?.id;
    setNotificationRules((prev) => {
      const next = prev.filter((_, i) => i !== index);
      setSelectedNotificationIndex((current) => Math.max(0, Math.min(current, next.length - 1)));
      if (next.length === 0) {
        setSelectedPanel('step');
      }
      return next;
    });
    if (removedRuleId) {
      setNodePositionOverrides((prev) => {
        const next = { ...prev };
        delete next[`notification-${removedRuleId}`];
        return next;
      });
    }
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
      const invalidStepScopedRules = notificationRules.filter((rule) =>
        STEP_SCOPED_NOTIFICATION_EVENTS.has(rule.triggerEvent) && rule.targetStepCodes.length === 0);
      if (invalidStepScopedRules.length > 0) {
        throw new Error('Step-based notifications require at least one Target Step.');
      }

      await workflowApi.upsertDefinition(editor);
      const draftRules = notificationRules.map((rule) => ({
        ...rule,
        workflowType: editor.workflowType,
      }));
      for (const rule of draftRules) {
        await workflowApi.upsertNotificationRule({
          ...rule,
          id: isGuid(rule.id) ? rule.id : undefined,
        });
      }
      const activeDraftRuleIds = new Set(draftRules
        .map((rule) => rule.id)
        .filter((id) => isGuid(id)));
      const removedRules = loadedNotificationRules
        .filter((rule) => rule.workflowType === editor.workflowType)
        .filter((rule) => !activeDraftRuleIds.has(rule.id));
      for (const removedRule of removedRules) {
        await workflowApi.upsertNotificationRule({
          id: removedRule.id,
          workflowType: removedRule.workflowType,
          triggerEvent: removedRule.triggerEvent,
          recipientMode: (NOTIFICATION_RECIPIENT_MODES.includes(removedRule.recipientMode as NotificationRuleRequest['recipientMode'])
            ? (removedRule.recipientMode as NotificationRuleRequest['recipientMode'])
            : 'Roles'),
          targetStepCodes: removedRule.targetStepCodes ?? [],
          recipientConfigJson: removedRule.recipientConfigJson,
          templateKey: removedRule.templateKey,
          templateTitle: removedRule.templateTitle ?? '',
          templateBody: removedRule.templateBody ?? '',
          clearPolicy: (NOTIFICATION_CLEAR_POLICIES.includes(removedRule.clearPolicy as NotificationRuleRequest['clearPolicy'])
            ? (removedRule.clearPolicy as NotificationRuleRequest['clearPolicy'])
            : 'None'),
          isActive: false,
        });
      }
      const refreshed = await workflowApi.getDefinitions(workflowType);
      const refreshedRules = await workflowApi.getNotificationRules(workflowType);
      setItems(refreshed);
      setLoadedNotificationRules(refreshedRules);
      setNotificationRules(refreshedRules
        .filter((rule) => rule.workflowType === editor.workflowType)
        .map(toNotificationDraft));
      setSelectedNotificationIndex(0);
      setSelectedPanel('step');
      setNodePositionOverrides({});
      setSaveSuccess('Workflow definition version saved.');
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : 'Failed to save workflow definition.');
    } finally {
      setSaving(false);
    }
  };

  const selectedStep = editor.steps[selectedStepIndex];
  const selectedNotificationRule = notificationRules[selectedNotificationIndex];
  const selectedNotificationRecipients = useMemo(
    () => parseRecipientConfigValues(selectedNotificationRule?.recipientConfigJson ?? emptyRecipientConfigJson),
    [selectedNotificationRule],
  );
  const selectedNotificationTargetStepCodes = useMemo(
    () => selectedNotificationRule?.targetStepCodes.filter((code) => definedStepCodes.includes(code)) ?? [],
    [definedStepCodes, selectedNotificationRule?.targetStepCodes],
  );
  const selectedNotificationIsStepScoped = useMemo(
    () => selectedNotificationRule ? STEP_SCOPED_NOTIFICATION_EVENTS.has(selectedNotificationRule.triggerEvent) : false,
    [selectedNotificationRule],
  );
  const roleRecipientOptions = useMemo(() => {
    const byTier = new Map<number, string[]>();
    roleOptions.forEach((role) => {
      const names = byTier.get(role.tier) ?? [];
      names.push(role.name);
      byTier.set(role.tier, names);
    });

    return Array.from(byTier.entries())
      .sort((a, b) => a[0] - b[0])
      .map(([tier, names]) => ({
        tier,
        label: names.join(' / '),
      }));
  }, [roleOptions]);
  const selectedResolverKeyDescriptions = useMemo(() => {
    if (selectedNotificationRule?.recipientMode !== 'Resolvers') return [];

    return selectedNotificationRecipients.map((resolverKey) => {
      const known = RESOLVER_KEY_OPTIONS.find((option) => option.key === resolverKey);
      return {
        key: resolverKey,
        label: known?.label ?? `${resolverKey} (Custom)`,
        description: known?.description ?? 'Custom resolver key. Ensure backend resolver support exists before using.',
      };
    });
  }, [selectedNotificationRecipients, selectedNotificationRule?.recipientMode]);
  const resolverDropdownOptions = useMemo(() => {
    const knownOptions = RESOLVER_KEY_OPTIONS.map((option) => ({
      key: option.key,
      label: option.label,
      description: option.description,
    }));
    const unknownSelected = selectedNotificationRecipients
      .filter((resolverKey) => !RESOLVER_KEY_OPTIONS.some((option) => option.key === resolverKey))
      .map((resolverKey) => ({
        key: resolverKey,
        label: `${resolverKey} (Custom)`,
        description: 'Custom resolver key. Ensure backend resolver support exists before using.',
      }));

    return [...knownOptions, ...unknownSelected];
  }, [selectedNotificationRecipients]);

  const graph = useMemo(() => {
    const codeToNodeId = new Map<string, string>();
    const stepNodes: Node<WorkflowBuilderNodeData>[] = editor.steps.map((step, index) => {
      const stepKey = step.stepCode.trim() || `idx-${index + 1}`;
      const nodeId = `step-${stepKey}`;
      if (!codeToNodeId.has(step.stepCode)) {
        codeToNodeId.set(step.stepCode, nodeId);
      }

      return {
        id: nodeId,
        type: 'workflowNode',
        position: nodePositionOverrides[nodeId] ?? { x: STEP_BASE_X + index * STEP_SPACING_X, y: STEP_BASE_Y },
        data: {
          kind: 'step',
          stepCode: step.stepCode,
          stepName: step.stepName,
          approvalMode: step.approvalMode,
          isApproval: step.approvalMode !== 'None',
          isStart: editor.startStepCode === step.stepCode,
          isTerminal: !step.onApproveNextStepCode,
          isSelected: selectedPanel === 'step' && selectedStepIndex === index,
        },
      };
    });

    const notificationNodes: Node<WorkflowBuilderNodeData>[] = notificationRules.map((rule, index) => {
      const nodeId = `notification-${rule.id}`;
      return {
        id: nodeId,
        type: 'workflowNode',
        position: nodePositionOverrides[nodeId] ?? { x: STEP_BASE_X + index * STEP_SPACING_X, y: NOTIFICATION_BASE_Y },
        data: {
          kind: 'notification',
          triggerEvent: rule.triggerEvent,
          recipientSummary: getNotificationRecipientSummary(rule),
          isSelected: selectedPanel === 'notification' && selectedNotificationIndex === index,
        },
      };
    });

    const nodes = [...stepNodes, ...notificationNodes];

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

    const mapNotificationTargets = (rule: NotificationRuleDraft): string[] => {
      const explicitTargets = rule.targetStepCodes.filter((code) => codeToNodeId.has(code));
      if (explicitTargets.length > 0) {
        return explicitTargets;
      }

      switch (rule.triggerEvent) {
        case 'Created':
          return [editor.startStepCode].filter((code) => codeToNodeId.has(code));
        case 'Completed':
          return editor.steps
            .filter((step) => !step.onApproveNextStepCode)
            .map((step) => step.stepCode)
            .filter((code) => codeToNodeId.has(code));
        default:
          return [];
      }
    };

    notificationRules.forEach((rule) => {
      const notificationNodeId = `notification-${rule.id}`;
      const targets = mapNotificationTargets(rule);

      targets.forEach((sourceStepCode) => {
        const sourceId = codeToNodeId.get(sourceStepCode);
        if (!sourceId) return;

        edges.push({
          id: `${sourceId}-notify-${notificationNodeId}-${rule.triggerEvent}`,
          source: sourceId,
          target: notificationNodeId,
          type: 'smoothstep',
          animated: true,
          label: `notify:${rule.triggerEvent}`,
          markerEnd: { type: MarkerType.ArrowClosed, color: '#17a2b8' },
          style: { stroke: '#17a2b8', strokeWidth: 1.5, strokeDasharray: '4 4' },
          labelStyle: { fill: '#0f6e7f', fontWeight: 600, fontSize: 10 },
        });
      });
    });

    return { nodes, edges };
  }, [
    editor.steps,
    editor.startStepCode,
    notificationRules,
    nodePositionOverrides,
    selectedPanel,
    selectedStepIndex,
    selectedNotificationIndex,
  ]);

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
    if (stepType === 'Notification') {
      const panelRect = (event.currentTarget as HTMLElement).getBoundingClientRect();
      addNotificationRule({
        x: Math.max(20, event.clientX - panelRect.left - 90),
        y: Math.max(20, event.clientY - panelRect.top - 35),
      });
      return;
    }
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
              <div
                className={styles.libraryType}
                draggable
                onDragStart={(event) => onLibraryDragStart(event, 'Notification')}
                title="Drag onto canvas to add Notification rule"
              >
                Notification
              </div>
            </div>
            <div className={styles.stepList}>
              {editor.steps.map((step, idx) => (
                <button
                  key={`${step.stepCode}-${idx}`}
                  type="button"
                  className={`${styles.stepListItem} ${idx === selectedStepIndex ? styles.stepListItemActive : ''}`}
                  onClick={() => {
                    setSelectedStepIndex(idx);
                    setSelectedPanel('step');
                  }}
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
                setNodePositionOverrides((prev) => ({ ...prev, [draggedNode.id]: draggedNode.position }));
                persistNodePositions(nextNodes);
              }}
              onNodeClick={(_, node) => {
                if (node.id.startsWith('notification-')) {
                  const ruleId = node.id.replace('notification-', '');
                  const notificationIndex = notificationRules.findIndex((rule) => rule.id === ruleId);
                  if (notificationIndex >= 0) {
                    setSelectedNotificationIndex(notificationIndex);
                    setSelectedPanel('notification');
                  }
                  return;
                }

                const stepIndex = editor.steps.findIndex((step, idx) => {
                  const stepKey = step.stepCode.trim() || `idx-${idx + 1}`;
                  return `step-${stepKey}` === node.id;
                });
                if (stepIndex >= 0) {
                  setSelectedStepIndex(stepIndex);
                  setSelectedPanel('step');
                }
              }}
            >
              <MiniMap zoomable pannable />
              <Controls />
              <Background gap={12} size={1} />
            </ReactFlow>
            <div className={styles.statusBar}>No errors</div>
          </section>

          <aside className={styles.rightPanel}>
            <div className={styles.panelTitle}>
              {selectedPanel === 'notification' ? 'Selected Notification Properties' : 'Selected Step Properties'}
            </div>
            {selectedPanel === 'step' && !selectedStep && (
              <div className={styles.emptyState}>Select or add a step to edit properties.</div>
            )}
            {selectedPanel === 'step' && selectedStep && (
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
            {selectedPanel === 'notification' && !selectedNotificationRule && (
              <div className={styles.emptyState}>Drag a Notification from Step Library and click it to edit.</div>
            )}
            {selectedPanel === 'notification' && selectedNotificationRule && (
              <div className={styles.propertiesForm}>
                <Label>Trigger Event</Label>
                <Dropdown
                  value={selectedNotificationRule.triggerEvent}
                  selectedOptions={[selectedNotificationRule.triggerEvent]}
                  onOptionSelect={(_, d) => updateNotificationRule(selectedNotificationIndex, { triggerEvent: d.optionValue ?? 'Created' })}
                >
                  {NOTIFICATION_TRIGGER_EVENTS.map((eventKey) => (
                    <Option key={eventKey} value={eventKey}>{eventKey}</Option>
                  ))}
                </Dropdown>
                <Label>Target Steps</Label>
                <Dropdown
                  multiselect
                  selectedOptions={selectedNotificationTargetStepCodes}
                  value={`${selectedNotificationTargetStepCodes.length} step(s) selected`}
                  onOptionSelect={(_, d) => updateNotificationRule(selectedNotificationIndex, { targetStepCodes: d.selectedOptions })}
                  disabled={definedStepCodes.length === 0}
                >
                  {definedStepCodes.map((code) => (
                    <Option key={`notify-target-${selectedNotificationIndex}-${code}`} value={code} text={code}>
                      {code}
                    </Option>
                  ))}
                </Dropdown>
                {selectedNotificationIsStepScoped && selectedNotificationTargetStepCodes.length === 0 && (
                  <div className={styles.error}>Select at least one target step for this trigger event.</div>
                )}
                <Label>Recipient Mode</Label>
                <Dropdown
                  value={selectedNotificationRule.recipientMode}
                  selectedOptions={[selectedNotificationRule.recipientMode]}
                  onOptionSelect={(_, d) => updateNotificationRule(selectedNotificationIndex, {
                    recipientMode: (d.optionValue as NotificationRuleRequest['recipientMode']) ?? 'Roles',
                    recipientConfigJson: emptyRecipientConfigJson,
                  })}
                >
                  {NOTIFICATION_RECIPIENT_MODES.map((mode) => (
                    <Option key={mode} value={mode}>{mode}</Option>
                  ))}
                </Dropdown>
                <Label>Who</Label>
                {selectedNotificationRule.recipientMode === 'Users' && (
                  <Dropdown
                    multiselect
                    selectedOptions={selectedNotificationRecipients}
                    value={`${selectedNotificationRecipients.length} user(s) selected`}
                    onOptionSelect={(_, d) => updateNotificationRecipientValues(selectedNotificationIndex, d.selectedOptions)}
                  >
                    {adminUsers.map((user) => (
                      <Option key={user.id} value={user.id} text={`${user.displayName} (${user.employeeNumber})`}>
                        {user.displayName} ({user.employeeNumber})
                      </Option>
                    ))}
                  </Dropdown>
                )}
                {selectedNotificationRule.recipientMode === 'Roles' && (
                  <Dropdown
                    multiselect
                    selectedOptions={selectedNotificationRecipients}
                    value={`${selectedNotificationRecipients.length} role(s) selected`}
                    onOptionSelect={(_, d) => updateNotificationRecipientValues(selectedNotificationIndex, d.selectedOptions)}
                  >
                    {roleRecipientOptions.map((roleOption) => (
                      <Option
                        key={String(roleOption.tier)}
                        value={String(roleOption.tier)}
                        text={`${roleOption.label} (${roleOption.tier})`}
                      >
                        {roleOption.label} ({roleOption.tier})
                      </Option>
                    ))}
                  </Dropdown>
                )}
                {selectedNotificationRule.recipientMode === 'Resolvers' && (
                  <div className={styles.resolverEditor}>
                    <Dropdown
                      multiselect
                      selectedOptions={selectedNotificationRecipients}
                      value={`${selectedNotificationRecipients.length} resolver(s) selected`}
                      onOptionSelect={(_, d) => updateNotificationRecipientValues(selectedNotificationIndex, d.selectedOptions)}
                    >
                      {resolverDropdownOptions.map((option) => (
                        <Option
                          key={option.key}
                          value={option.key}
                          text={option.label}
                        >
                          {option.label}
                        </Option>
                      ))}
                    </Dropdown>
                    <div className={styles.helperList}>
                      {selectedResolverKeyDescriptions.length === 0 ? (
                        <div className={styles.emptyState}>Select resolver keys from the list.</div>
                      ) : (
                        selectedResolverKeyDescriptions.map((resolver) => (
                          <div key={resolver.key} className={styles.helperItem}>
                            <strong>{resolver.label}:</strong> {resolver.description}
                          </div>
                        ))
                      )}
                    </div>
                  </div>
                )}
                {notificationLookupError && <div className={styles.error}>{notificationLookupError}</div>}
                <Label>Template Key</Label>
                <Input
                  value={selectedNotificationRule.templateKey}
                  onChange={(_, d) => updateNotificationRule(selectedNotificationIndex, { templateKey: d.value })}
                />
                <Label>Notification Title</Label>
                <Input
                  value={selectedNotificationRule.templateTitle}
                  onChange={(_, d) => updateNotificationRule(selectedNotificationIndex, { templateTitle: d.value })}
                />
                <Label>Notification Body</Label>
                <Textarea
                  value={selectedNotificationRule.templateBody}
                  onChange={(_, d) => updateNotificationRule(selectedNotificationIndex, { templateBody: d.value })}
                />
                <Label>Clear Policy</Label>
                <Dropdown
                  value={selectedNotificationRule.clearPolicy}
                  selectedOptions={[selectedNotificationRule.clearPolicy]}
                  onOptionSelect={(_, d) => updateNotificationRule(selectedNotificationIndex, {
                    clearPolicy: (d.optionValue as NotificationRuleRequest['clearPolicy']) ?? 'None',
                  })}
                >
                  {NOTIFICATION_CLEAR_POLICIES.map((policy) => (
                    <Option key={policy} value={policy}>{policy}</Option>
                  ))}
                </Dropdown>
                <Switch
                  label="Rule Active"
                  checked={selectedNotificationRule.isActive}
                  onChange={(_, d) => updateNotificationRule(selectedNotificationIndex, { isActive: d.checked })}
                />
                <Button size="small" onClick={() => removeNotificationRule(selectedNotificationIndex)}>Remove Notification</Button>
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
