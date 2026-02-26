import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button, Checkbox, Dropdown, Input, Label, Option, Spinner, Textarea } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { checklistApi, productionLineApi, siteApi, workCenterApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { ChecklistTemplate, Plant, ProductionLine, WorkCenter } from '../../types/domain.ts';
import type { UpsertChecklistTemplateRequest } from '../../types/api.ts';
import styles from './CardList.module.css';

const CHECKLIST_TYPES = ['SafetyPreShift', 'SafetyPeriodic', 'OpsPreShift', 'OpsChangeover'];
const SCOPE_LEVELS = ['PlantWorkCenter', 'SiteDefault', 'GlobalDefault'];
const RESPONSE_MODES = ['PFNA', 'PF'];
const QUESTION_RESPONSE_TYPES = ['PassFail', 'Text', 'Select', 'Date'] as const;

type QuestionResponseType = (typeof QUESTION_RESPONSE_TYPES)[number];

type EditableChecklistItem = {
  id?: string;
  sortOrder: number;
  prompt: string;
  isRequired: boolean;
  responseMode?: string;
  responseType: QuestionResponseType;
  responseOptions: string[];
  helpText?: string;
  requireFailNote: boolean;
};

function toIsoLocalDate(value?: string) {
  if (!value) return '';
  return value.slice(0, 16);
}

function getErrorMessage(err: unknown, fallback: string): string {
  if (typeof err === 'object' && err !== null && 'message' in err) {
    const msg = (err as { message?: unknown }).message;
    if (typeof msg === 'string' && msg.trim()) return msg;
  }
  return fallback;
}

export function ChecklistTemplatesScreen() {
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const canManage = roleTier <= 4;
  const canCrossSite = roleTier <= 2;

  const [templates, setTemplates] = useState<ChecklistTemplate[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [productionLines, setProductionLines] = useState<ProductionLine[]>([]);
  const [siteFilter, setSiteFilter] = useState<string>(canCrossSite ? '' : (user?.defaultSiteId ?? ''));
  const [checklistTypeFilter, setChecklistTypeFilter] = useState<string>('');
  const [loading, setLoading] = useState(true);

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ChecklistTemplate | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [title, setTitle] = useState('');
  const [templateCode, setTemplateCode] = useState('');
  const [checklistType, setChecklistType] = useState(CHECKLIST_TYPES[0]);
  const [scopeLevel, setScopeLevel] = useState(SCOPE_LEVELS[0]);
  const [siteId, setSiteId] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [productionLineId, setProductionLineId] = useState('');
  const [versionNo, setVersionNo] = useState('1');
  const [effectiveFromUtc, setEffectiveFromUtc] = useState('');
  const [effectiveToUtc, setEffectiveToUtc] = useState('');
  const [responseMode, setResponseMode] = useState(RESPONSE_MODES[0]);
  const [requireFailNote, setRequireFailNote] = useState(false);
  const [isSafetyProfile, setIsSafetyProfile] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const [items, setItems] = useState<EditableChecklistItem[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<string[]>([]);
  const [itemImportText, setItemImportText] = useState('');
  const [importModalOpen, setImportModalOpen] = useState(false);
  const [importError, setImportError] = useState('');
  const [importSuccess, setImportSuccess] = useState('');
  const [questionEditorIndex, setQuestionEditorIndex] = useState<number | null>(null);
  const questionListRef = useRef<HTMLDivElement | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const siteIdQuery = canCrossSite ? (siteFilter || undefined) : user?.defaultSiteId;
      const [templatesData, siteData, wcData] = await Promise.all([
        checklistApi.getTemplates(siteIdQuery, checklistTypeFilter || undefined),
        siteApi.getSites(),
        workCenterApi.getWorkCenters(),
      ]);
      setTemplates(templatesData);
      setSites(siteData);
      setWorkCenters(wcData);
    } catch {
      setError('Failed to load checklist templates.');
    } finally {
      setLoading(false);
    }
  }, [canCrossSite, siteFilter, checklistTypeFilter, user?.defaultSiteId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!siteId) {
      setProductionLines([]);
      return;
    }
    productionLineApi.getProductionLines(siteId).then(setProductionLines).catch(() => setProductionLines([]));
  }, [siteId]);

  const siteNameById = useMemo(
    () => new Map(sites.map((s) => [s.id, `${s.name} (${s.code})`])),
    [sites],
  );
  const wcNameById = useMemo(
    () => new Map(workCenters.map((w) => [w.id, w.name])),
    [workCenters],
  );
  const plNameById = useMemo(
    () => new Map(productionLines.map((p) => [p.id, p.name])),
    [productionLines],
  );

  const openCreate = () => {
    setEditing(null);
    setTitle('');
    setTemplateCode('');
    setChecklistType(CHECKLIST_TYPES[0]);
    // Default to SiteDefault so first save path is less error-prone.
    setScopeLevel('SiteDefault');
    setSiteId(user?.defaultSiteId ?? '');
    setWorkCenterId('');
    setProductionLineId('');
    setVersionNo('1');
    setEffectiveFromUtc(new Date().toISOString().slice(0, 16));
    setEffectiveToUtc('');
    setResponseMode('PFNA');
    setRequireFailNote(false);
    setIsSafetyProfile(false);
    setIsActive(true);
    setItems([]);
    setDeletedItemIds([]);
    setItemImportText('');
    setImportError('');
    setImportSuccess('');
    setImportModalOpen(false);
    setQuestionEditorIndex(null);
    setError('');
    setModalOpen(true);
  };

  const openEdit = (template: ChecklistTemplate) => {
    setEditing(template);
    setTitle(template.title);
    setTemplateCode(template.templateCode);
    setChecklistType(template.checklistType);
    setScopeLevel(template.scopeLevel);
    setSiteId(template.siteId ?? '');
    setWorkCenterId(template.workCenterId ?? '');
    setProductionLineId(template.productionLineId ?? '');
    setVersionNo(String(template.versionNo));
    setEffectiveFromUtc(toIsoLocalDate(template.effectiveFromUtc));
    setEffectiveToUtc(toIsoLocalDate(template.effectiveToUtc));
    setResponseMode(template.responseMode);
    setRequireFailNote(template.requireFailNote);
    setIsSafetyProfile(template.isSafetyProfile);
    setIsActive(template.isActive);
    setItems(
      [...template.items]
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((item, index) => ({
          id: item.id,
          sortOrder: item.sortOrder ?? index + 1,
          prompt: item.prompt,
          isRequired: item.isRequired,
          responseMode: item.responseMode ?? template.responseMode,
          responseType: item.responseType ?? 'PassFail',
          responseOptions: item.responseOptions ?? [],
          helpText: item.helpText ?? '',
          requireFailNote: item.requireFailNote,
        })),
    );
    setDeletedItemIds([]);
    setItemImportText('');
    setImportError('');
    setImportSuccess('');
    setImportModalOpen(false);
    setQuestionEditorIndex(null);
    setError('');
    setModalOpen(true);
  };

  const updateItem = (index: number, patch: Partial<EditableChecklistItem>) => {
    setItems((prev) => prev.map((item, i) => (i === index ? { ...item, ...patch } : item)));
  };

  const scrollQuestionsToBottom = () => {
    requestAnimationFrame(() => {
      const container = questionListRef.current;
      if (!container) return;
      if (typeof container.scrollTo === 'function') {
        container.scrollTo({ top: container.scrollHeight, behavior: 'smooth' });
        return;
      }
      container.scrollTop = container.scrollHeight;
    });
  };

  const addItem = () => {
    let nextIndex = 0;
    setItems((prev) => {
      nextIndex = prev.length;
      return [
        ...prev,
        {
          sortOrder: prev.length + 1,
          prompt: '',
          isRequired: true,
          responseMode,
          responseType: 'PassFail',
          responseOptions: [],
          helpText: '',
          requireFailNote: false,
        },
      ];
    });
    setQuestionEditorIndex(nextIndex);
    setImportSuccess('');
    scrollQuestionsToBottom();
  };

  const removeItem = (index: number) => {
    setItems((prev) => {
      const removed = prev[index];
      const removedId = removed?.id;
      if (removedId) {
        setDeletedItemIds((existing) => (existing.includes(removedId) ? existing : [...existing, removedId]));
      }
      return prev
        .filter((_, i) => i !== index)
        .map((item, i) => ({ ...item, sortOrder: i + 1 }));
    });
    setQuestionEditorIndex((prev) => {
      if (prev == null) return prev;
      if (prev === index) return null;
      return prev > index ? prev - 1 : prev;
    });
  };

  const moveItem = (index: number, direction: -1 | 1) => {
    setItems((prev) => {
      const nextIndex = index + direction;
      if (nextIndex < 0 || nextIndex >= prev.length) return prev;
      const next = [...prev];
      [next[index], next[nextIndex]] = [next[nextIndex], next[index]];
      return next.map((item, i) => ({ ...item, sortOrder: i + 1 }));
    });
  };

  const importItemsFromText = () => {
    const prompts = itemImportText
      .split('\n')
      .map((line) => line.trim())
      .filter(Boolean);
    if (prompts.length === 0) {
      setImportError('Enter at least one line to import checklist items.');
      return;
    }

    setItems((prev) => {
      const imported = prompts.map((prompt, index) => ({
        sortOrder: prev.length + index + 1,
        prompt,
        isRequired: true,
        responseMode,
        responseType: 'PassFail' as QuestionResponseType,
        responseOptions: [],
        helpText: '',
        requireFailNote: false,
      }));
      return [...prev, ...imported];
    });
    setItemImportText('');
    setImportError('');
    setImportSuccess(`Imported ${prompts.length} question${prompts.length === 1 ? '' : 's'}.`);
    setImportModalOpen(false);
    scrollQuestionsToBottom();
  };

  const handleSave = async () => {
    if (!title.trim() || !templateCode.trim()) {
      setError('Title and template code are required.');
      return;
    }

    const normalizedItems = items
      .map((item, index) => ({
        ...item,
        sortOrder: index + 1,
        prompt: item.prompt.trim(),
        helpText: item.helpText?.trim() || undefined,
        responseOptions: item.responseOptions
          .map((option) => option.trim())
          .filter(Boolean)
          .filter((option, optionIndex, arr) => arr.findIndex((v) => v.toLowerCase() === option.toLowerCase()) === optionIndex),
      }));
    if (normalizedItems.length === 0) {
      setError('At least one checklist item prompt is required.');
      return;
    }
    if (normalizedItems.some((item) => !item.prompt)) {
      setError('Every checklist item must have a prompt.');
      return;
    }
    const invalidSelect = normalizedItems.find((item) => item.responseType === 'Select' && item.responseOptions.length < 2);
    if (invalidSelect) {
      setError('Select response type requires at least two unique options.');
      return;
    }
    const failNoteOnNonPassFail = normalizedItems.find((item) => item.requireFailNote && item.responseType !== 'PassFail');
    if (failNoteOnNonPassFail) {
      setError('Fail note can only be enabled on PassFail questions.');
      return;
    }

    if (scopeLevel === 'PlantWorkCenter') {
      if (!siteId || !workCenterId) {
        setError('PlantWorkCenter scope requires both Site and Work Center.');
        return;
      }
    }
    if (scopeLevel === 'SiteDefault') {
      if (!siteId) {
        setError('SiteDefault scope requires Site.');
        return;
      }
    }
    if (scopeLevel === 'GlobalDefault' && (siteId || workCenterId || productionLineId)) {
      setError('GlobalDefault cannot include Site, Work Center, or Production Line.');
      return;
    }

    if ((requireFailNote || isSafetyProfile) && responseMode !== 'PF') {
      setError('Fail-note and safety profiles require PF response mode.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const request: UpsertChecklistTemplateRequest = {
        id: editing?.id,
        title: title.trim(),
        templateCode: templateCode.trim(),
        checklistType,
        scopeLevel,
        siteId: siteId || undefined,
        workCenterId: workCenterId || undefined,
        productionLineId: productionLineId || undefined,
        versionNo: Number(versionNo || '1'),
        effectiveFromUtc: new Date(effectiveFromUtc || new Date().toISOString()).toISOString(),
        effectiveToUtc: effectiveToUtc ? new Date(effectiveToUtc).toISOString() : undefined,
        isActive,
        responseMode,
        requireFailNote,
        isSafetyProfile,
        deletedItemIds,
        items: normalizedItems.map((item, index) => ({
          id: item.id,
          sortOrder: index + 1,
          prompt: item.prompt,
          isRequired: item.isRequired,
          responseMode: item.responseType === 'PassFail' ? (item.responseMode || responseMode) : undefined,
          responseType: item.responseType,
          responseOptions: item.responseType === 'Select' ? item.responseOptions : [],
          helpText: item.helpText,
          requireFailNote: item.responseType === 'PassFail' ? item.requireFailNote : false,
        })),
      };

      await checklistApi.upsertTemplate(request);
      setModalOpen(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to save checklist template.'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDisabled = !title.trim() || !templateCode.trim() || saving;

  return (
    <AdminLayout title="Checklist Templates" onAdd={canManage ? openCreate : undefined} addLabel="Add Template">
      {(canCrossSite || checklistTypeFilter) && (
        <div className={styles.filterBar}>
          {canCrossSite && (
            <>
              <label style={{ fontSize: 12, fontWeight: 600 }}>Site</label>
              <Dropdown
                value={siteNameById.get(siteFilter) ?? 'All Sites'}
                selectedOptions={[siteFilter]}
                onOptionSelect={(_, data) => setSiteFilter(data.optionValue ?? '')}
              >
                <Option value="">All Sites</Option>
                {sites.map((site) => (
                  <Option key={site.id} value={site.id} text={`${site.name} (${site.code})`}>
                    {site.name} ({site.code})
                  </Option>
                ))}
              </Dropdown>
            </>
          )}
          <label style={{ fontSize: 12, fontWeight: 600 }}>Type</label>
          <Dropdown
            value={checklistTypeFilter || 'All Types'}
            selectedOptions={[checklistTypeFilter]}
            onOptionSelect={(_, data) => setChecklistTypeFilter(data.optionValue ?? '')}
          >
            <Option value="">All Types</Option>
            {CHECKLIST_TYPES.map((type) => (
              <Option key={type} value={type}>{type}</Option>
            ))}
          </Dropdown>
        </div>
      )}

      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {templates.length === 0 && <div className={styles.emptyState}>No checklist templates found.</div>}
          {templates.map((template) => (
            <div key={template.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{template.title}</span>
                {canManage && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(template)} />
                  </div>
                )}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Code</span>
                <span className={styles.cardFieldValue}>{template.templateCode} v{template.versionNo}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{template.checklistType}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Scope</span>
                <span className={styles.cardFieldValue}>{template.scopeLevel}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Site</span>
                <span className={styles.cardFieldValue}>{template.siteId ? siteNameById.get(template.siteId) : 'Global'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{template.workCenterId ? wcNameById.get(template.workCenterId) : 'Default'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Line</span>
                <span className={styles.cardFieldValue}>{template.productionLineId ? plNameById.get(template.productionLineId) : 'Any'}</span>
              </div>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${template.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                  {template.isActive ? 'Active' : 'Inactive'}
                </span>
                <span className={`${styles.badge} ${styles.badgeBlue}`}>{template.responseMode}</span>
                {template.requireFailNote && <span className={`${styles.badge} ${styles.badgeBlue}`}>Fail Note Required</span>}
                {template.isSafetyProfile && <span className={`${styles.badge} ${styles.badgeBlue}`}>Safety Profile</span>}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Items</span>
                <span className={styles.cardFieldValue}>{template.items.length}</span>
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? `Edit Template: ${editing.title}` : 'Add Checklist Template'}
        onConfirm={handleSave}
        onCancel={() => {
          setModalOpen(false);
          setImportModalOpen(false);
          setQuestionEditorIndex(null);
        }}
        confirmLabel="Save"
        loading={saving}
        error={error}
        confirmDisabled={confirmDisabled}
        wide
      >
        <div className={styles.formGrid}>
          <div className={styles.formColumn}>
            <Label>Title</Label>
            <Input value={title} onChange={(_, data) => setTitle(data.value)} />
          </div>

          <div className={styles.formColumn}>
            <Label>Template Code</Label>
            <Input value={templateCode} onChange={(_, data) => setTemplateCode(data.value)} />
          </div>

          <div className={styles.formColumn}>
            <Label>Checklist Type</Label>
            <Dropdown value={checklistType} selectedOptions={[checklistType]} onOptionSelect={(_, data) => data.optionValue && setChecklistType(data.optionValue)}>
              {CHECKLIST_TYPES.map((type) => <Option key={type} value={type}>{type}</Option>)}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Label>Scope Level</Label>
            <Dropdown
              value={scopeLevel}
              selectedOptions={[scopeLevel]}
              onOptionSelect={(_, data) => {
                const next = data.optionValue ?? scopeLevel;
                setScopeLevel(next);
                if (next === 'GlobalDefault') {
                  setSiteId('');
                  setWorkCenterId('');
                  setProductionLineId('');
                } else if (next === 'SiteDefault') {
                  setWorkCenterId('');
                  setProductionLineId('');
                  if (!siteId) setSiteId(user?.defaultSiteId ?? '');
                }
              }}
            >
              {SCOPE_LEVELS.map((scope) => <Option key={scope} value={scope}>{scope}</Option>)}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Label>Site</Label>
            <Dropdown value={siteNameById.get(siteId) ?? 'None'} selectedOptions={[siteId]} onOptionSelect={(_, data) => setSiteId(data.optionValue ?? '')}>
              <Option value="">None</Option>
              {sites.map((site) => (
                <Option key={site.id} value={site.id} text={`${site.name} (${site.code})`}>
                  {site.name} ({site.code})
                </Option>
              ))}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Label>Work Center</Label>
            <Dropdown value={wcNameById.get(workCenterId) ?? 'None'} selectedOptions={[workCenterId]} onOptionSelect={(_, data) => setWorkCenterId(data.optionValue ?? '')}>
              <Option value="">None</Option>
              {workCenters.map((workCenter) => <Option key={workCenter.id} value={workCenter.id}>{workCenter.name}</Option>)}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Label>Production Line</Label>
            <Dropdown value={plNameById.get(productionLineId) ?? 'None'} selectedOptions={[productionLineId]} onOptionSelect={(_, data) => setProductionLineId(data.optionValue ?? '')}>
              <Option value="">None</Option>
              {productionLines.map((line) => <Option key={line.id} value={line.id}>{line.name}</Option>)}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Label>Version</Label>
            <Input type="number" value={versionNo} onChange={(_, data) => setVersionNo(data.value)} />
          </div>

          <div className={styles.formColumn}>
            <Label>Effective From (UTC)</Label>
            <Input type="datetime-local" value={effectiveFromUtc} onChange={(_, data) => setEffectiveFromUtc(data.value)} />
          </div>

          <div className={styles.formColumn}>
            <Label>Effective To (UTC, optional)</Label>
            <Input type="datetime-local" value={effectiveToUtc} onChange={(_, data) => setEffectiveToUtc(data.value)} />
          </div>

          <div className={styles.formColumn}>
            <Label>Response Mode</Label>
            <Dropdown value={responseMode} selectedOptions={[responseMode]} onOptionSelect={(_, data) => data.optionValue && setResponseMode(data.optionValue)}>
              {RESPONSE_MODES.map((mode) => <Option key={mode} value={mode}>{mode}</Option>)}
            </Dropdown>
          </div>

          <div className={styles.formColumn}>
            <Checkbox label="Template active" checked={isActive} onChange={(_, data) => setIsActive(!!data.checked)} />
          </div>
          <div className={styles.formColumn}>
            <Checkbox label="Require note on fail" checked={requireFailNote} onChange={(_, data) => setRequireFailNote(!!data.checked)} />
          </div>
          <div className={styles.formColumn}>
            <Checkbox label="Safety profile" checked={isSafetyProfile} onChange={(_, data) => setIsSafetyProfile(!!data.checked)} />
          </div>
        </div>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 12 }}>
          <Label>Checklist Questions</Label>
          <div style={{ display: 'flex', gap: 8 }}>
            <Button
              onClick={() => {
                setImportError('');
                setImportSuccess('');
                setImportModalOpen(true);
              }}
            >
              Import as PassFail Questions
            </Button>
            <Button onClick={addItem}>Add Question</Button>
          </div>
        </div>
        {importSuccess && <div style={{ color: '#107c10', fontSize: 13 }}>{importSuccess}</div>}
        <div
          ref={questionListRef}
          style={{ maxHeight: '48vh', overflowY: 'auto', border: '1px solid #e5e7eb', borderRadius: 6, padding: 8 }}
        >
          {items.map((item, index) => (
            <div key={item.id ?? `item-${index}`} style={{ border: '1px solid #d0d0d0', padding: 12, marginTop: 8, borderRadius: 4 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
                  <strong>Question {index + 1}</strong>
                  <span style={{ color: '#374151', fontSize: 13, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {item.prompt || '(No prompt yet)'}
                  </span>
                  <span style={{ color: '#6b7280', fontSize: 12 }}>
                    Type: {item.responseType} | Required: {item.isRequired ? 'Yes' : 'No'}
                  </span>
                </div>
                <div style={{ display: 'flex', gap: 6 }}>
                  <Button size="small" onClick={() => setQuestionEditorIndex(index)}>Edit</Button>
                  <Button size="small" disabled={index === 0} onClick={() => moveItem(index, -1)}>Up</Button>
                  <Button size="small" disabled={index === items.length - 1} onClick={() => moveItem(index, 1)}>Down</Button>
                  <Button size="small" appearance="subtle" onClick={() => removeItem(index)}>Remove</Button>
                </div>
              </div>
            </div>
          ))}
        </div>

      </AdminModal>

      <AdminModal
        open={questionEditorIndex !== null}
        title={questionEditorIndex !== null ? `Edit Question ${questionEditorIndex + 1}` : 'Edit Question'}
        onConfirm={() => setQuestionEditorIndex(null)}
        onCancel={() => setQuestionEditorIndex(null)}
        confirmLabel="Done"
        confirmDisabled={questionEditorIndex === null}
      >
        {questionEditorIndex !== null && items[questionEditorIndex] && (
          <>
            <Label>Prompt</Label>
            <Input
              value={items[questionEditorIndex].prompt}
              onChange={(_, data) => updateItem(questionEditorIndex, { prompt: data.value })}
            />

            <Label>Response Type</Label>
            <Dropdown
              value={items[questionEditorIndex].responseType}
              selectedOptions={[items[questionEditorIndex].responseType]}
              onOptionSelect={(_, data) => {
                const current = items[questionEditorIndex];
                const nextType = (data.optionValue ?? current.responseType) as QuestionResponseType;
                updateItem(questionEditorIndex, {
                  responseType: nextType,
                  responseOptions: nextType === 'Select' ? current.responseOptions : [],
                  requireFailNote: nextType === 'PassFail' ? current.requireFailNote : false,
                });
              }}
            >
              {QUESTION_RESPONSE_TYPES.map((type) => <Option key={type} value={type}>{type}</Option>)}
            </Dropdown>

            {items[questionEditorIndex].responseType === 'PassFail' && (
              <>
                <Label>Pass/Fail Mode</Label>
                <Dropdown
                  value={items[questionEditorIndex].responseMode ?? responseMode}
                  selectedOptions={[items[questionEditorIndex].responseMode ?? responseMode]}
                  onOptionSelect={(_, data) =>
                    updateItem(questionEditorIndex, { responseMode: data.optionValue ?? responseMode })
                  }
                >
                  {RESPONSE_MODES.map((mode) => <Option key={mode} value={mode}>{mode}</Option>)}
                </Dropdown>
              </>
            )}

            {items[questionEditorIndex].responseType === 'Select' && (
              <>
                <Label>Select Options (one per line)</Label>
                <Textarea
                  value={items[questionEditorIndex].responseOptions.join('\n')}
                  onChange={(_, data) => updateItem(questionEditorIndex, { responseOptions: data.value.split('\n') })}
                  rows={4}
                  placeholder={'Option A\nOption B'}
                />
              </>
            )}

            <Label>Help Text (optional)</Label>
            <Input
              value={items[questionEditorIndex].helpText ?? ''}
              onChange={(_, data) => updateItem(questionEditorIndex, { helpText: data.value })}
            />

            <Checkbox
              label="Required"
              checked={items[questionEditorIndex].isRequired}
              onChange={(_, data) => updateItem(questionEditorIndex, { isRequired: !!data.checked })}
            />
            <Checkbox
              label="Require note on fail"
              checked={items[questionEditorIndex].requireFailNote}
              disabled={items[questionEditorIndex].responseType !== 'PassFail'}
              onChange={(_, data) => updateItem(questionEditorIndex, { requireFailNote: !!data.checked })}
            />
          </>
        )}
      </AdminModal>

      <AdminModal
        open={importModalOpen}
        title="Import PassFail Questions"
        onConfirm={importItemsFromText}
        onCancel={() => setImportModalOpen(false)}
        confirmLabel="Import Questions"
        confirmDisabled={!itemImportText.trim()}
        error={importError}
      >
        <Label>Paste questions (one per line)</Label>
        <Textarea
          value={itemImportText}
          onChange={(_, data) => setItemImportText(data.value)}
          rows={10}
          placeholder={'Hard hat in place?\nEmergency stop tested?'}
        />
      </AdminModal>
    </AdminLayout>
  );
}
