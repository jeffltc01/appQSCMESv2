import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Checkbox, Dropdown, Input, Label, Option, Spinner, Textarea } from '@fluentui/react-components';
import { useNavigate, useParams } from 'react-router-dom';
import { AdminLayout } from './AdminLayout.tsx';
import { adminUserApi, checklistApi, productionLineApi, siteApi, workCenterApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { UpsertChecklistTemplateRequest } from '../../types/api.ts';
import type { AdminUser, ChecklistTemplate, Plant, ProductionLine, WorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

const CHECKLIST_TYPES = ['SafetyPreShift', 'SafetyPeriodic', 'OpsPreShift', 'OpsChangeover'];
const SCOPE_LEVELS = ['PlantWorkCenter', 'SiteDefault', 'GlobalDefault'];
const RESPONSE_MODES = ['PFNA', 'PF'];
const QUESTION_RESPONSE_TYPES = ['Checkbox', 'Datetime', 'Number', 'Image', 'Dimension', 'Score'] as const;
const NEW_SECTION_OPTION = '__new__';

type QuestionResponseType = (typeof QUESTION_RESPONSE_TYPES)[number];

type EditableChecklistItem = {
  id?: string;
  sortOrder: number;
  prompt: string;
  isRequired: boolean;
  section?: string;
  responseType: QuestionResponseType;
  scoreTypeId?: string;
  dimensionTarget?: string;
  dimensionUpperLimit?: string;
  dimensionLowerLimit?: string;
  dimensionUnitOfMeasure?: string;
  helpText?: string;
  requireFailNote: boolean;
};

function toLocalDateTimeInputValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function toLocalDateTimeInput(value?: string) {
  if (!value) return '';
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return '';
  return toLocalDateTimeInputValue(parsed);
}

function toUtcIsoFromLocalDateTime(value?: string) {
  if (!value?.trim()) return new Date().toISOString();
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return new Date().toISOString();
  return parsed.toISOString();
}

function toNumeric(value?: string): number | undefined {
  if (!value || !value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function getErrorMessage(err: unknown, fallback: string): string {
  if (typeof err === 'object' && err !== null && 'message' in err) {
    const message = (err as { message?: unknown }).message;
    if (typeof message === 'string' && message.trim()) return message;
  }
  return fallback;
}

export function ChecklistTemplateEditorScreen() {
  const navigate = useNavigate();
  const { templateId } = useParams<{ templateId: string }>();
  const isCreate = !templateId || templateId === 'new';
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const canCrossSite = roleTier <= 2;
  const canAssignAnyOwner = roleTier <= 2;

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [sites, setSites] = useState<Plant[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [productionLines, setProductionLines] = useState<ProductionLine[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [scoreTypes, setScoreTypes] = useState<{ id: string; name: string }[]>([]);

  const [editing, setEditing] = useState<ChecklistTemplate | null>(null);
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
  const [ownerUserId, setOwnerUserId] = useState('');
  const [items, setItems] = useState<EditableChecklistItem[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<string[]>([]);
  const [activeSectionEditorIndex, setActiveSectionEditorIndex] = useState<number | null>(null);

  const siteNameById = useMemo(() => new Map(sites.map((s) => [s.id, `${s.name} (${s.code})`])), [sites]);
  const wcNameById = useMemo(() => new Map(workCenters.map((w) => [w.id, w.name])), [workCenters]);
  const plNameById = useMemo(() => new Map(productionLines.map((p) => [p.id, p.name])), [productionLines]);
  const ownerNameById = useMemo(() => new Map(users.map((u) => [u.id, `${u.displayName} (${u.employeeNumber})`])), [users]);

  const sectionOptions = useMemo(
    () => Array.from(new Set(items.map((item) => (item.section ?? '').trim()).filter(Boolean))).sort((a, b) => a.localeCompare(b)),
    [items],
  );

  const groupedItems = useMemo(() => {
    const map = new Map<string, Array<{ index: number; item: EditableChecklistItem }>>();
    items.forEach((item, index) => {
      const key = (item.section ?? '').trim();
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push({ index, item });
    });
    const keys = Array.from(map.keys()).sort((a, b) => {
      if (!a) return -1;
      if (!b) return 1;
      return a.localeCompare(b);
    });
    return keys.map((key) => ({ section: key, items: map.get(key)! }));
  }, [items]);

  useEffect(() => {
    if (!siteId) {
      setProductionLines([]);
      return;
    }
    productionLineApi.getProductionLines(siteId).then(setProductionLines).catch(() => setProductionLines([]));
  }, [siteId]);

  const applyTemplateToState = useCallback((template: ChecklistTemplate | null) => {
    if (!template) {
      setEditing(null);
      setTitle('');
      setTemplateCode('');
      setChecklistType(CHECKLIST_TYPES[0]);
      setScopeLevel('SiteDefault');
      setSiteId(user?.defaultSiteId ?? '');
      setWorkCenterId('');
      setProductionLineId('');
      setVersionNo('1');
      setEffectiveFromUtc(toLocalDateTimeInputValue(new Date()));
      setEffectiveToUtc('');
      setResponseMode('PFNA');
      setRequireFailNote(false);
      setIsSafetyProfile(false);
      setIsActive(true);
      setOwnerUserId(canAssignAnyOwner ? (user?.id ?? '') : (user?.id ?? ''));
      setItems([]);
      setDeletedItemIds([]);
      return;
    }

    setEditing(template);
    setTitle(template.title);
    setTemplateCode(template.templateCode);
    setChecklistType(template.checklistType);
    setScopeLevel(template.scopeLevel);
    setSiteId(template.siteId ?? '');
    setWorkCenterId(template.workCenterId ?? '');
    setProductionLineId(template.productionLineId ?? '');
    setVersionNo(String(template.versionNo));
    setEffectiveFromUtc(toLocalDateTimeInput(template.effectiveFromUtc));
    setEffectiveToUtc(toLocalDateTimeInput(template.effectiveToUtc));
    setResponseMode(template.responseMode);
    setRequireFailNote(template.requireFailNote);
    setIsSafetyProfile(template.isSafetyProfile);
    setIsActive(template.isActive);
    setOwnerUserId(template.ownerUserId);
    setItems(
      [...template.items]
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((item, index) => ({
          id: item.id,
          sortOrder: item.sortOrder ?? index + 1,
          prompt: item.prompt,
          isRequired: item.isRequired,
          section: item.section ?? '',
          responseType: item.responseType ?? 'Checkbox',
          scoreTypeId: item.scoreTypeId,
          dimensionTarget: item.dimensionTarget != null ? String(item.dimensionTarget) : '',
          dimensionUpperLimit: item.dimensionUpperLimit != null ? String(item.dimensionUpperLimit) : '',
          dimensionLowerLimit: item.dimensionLowerLimit != null ? String(item.dimensionLowerLimit) : '',
          dimensionUnitOfMeasure: item.dimensionUnitOfMeasure ?? '',
          helpText: item.helpText ?? '',
          requireFailNote: item.requireFailNote,
        })),
    );
    setDeletedItemIds([]);
  }, [canAssignAnyOwner, user?.defaultSiteId, user?.id]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError('');
      try {
        const [siteData, wcData, scoreTypeData, userData] = await Promise.all([
          siteApi.getSites(),
          workCenterApi.getWorkCenters(),
          checklistApi.getScoreTypes(false),
          canAssignAnyOwner ? adminUserApi.getAll() : Promise.resolve([] as AdminUser[]),
        ]);
        setSites(siteData);
        setWorkCenters(wcData);
        setScoreTypes(scoreTypeData.map((s) => ({ id: s.id, name: s.name })));
        setUsers(canAssignAnyOwner ? userData : []);

        if (isCreate) {
          applyTemplateToState(null);
        } else {
          const template = await checklistApi.getTemplate(templateId ?? '');
          applyTemplateToState(template);
        }
      } catch (err) {
        setError(getErrorMessage(err, 'Failed to load checklist template editor.'));
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [applyTemplateToState, canAssignAnyOwner, isCreate, templateId]);

  const updateItem = (index: number, patch: Partial<EditableChecklistItem>) => {
    setItems((prev) => prev.map((item, i) => (i === index ? { ...item, ...patch } : item)));
  };

  const addItem = () => {
    setItems((prev) => [
      ...prev,
      {
        sortOrder: prev.length + 1,
        prompt: '',
        isRequired: true,
        responseType: 'Checkbox',
        section: '',
        scoreTypeId: undefined,
        dimensionTarget: '',
        dimensionUpperLimit: '',
        dimensionLowerLimit: '',
        dimensionUnitOfMeasure: '',
        helpText: '',
        requireFailNote: false,
      },
    ]);
  };

  const removeItem = (index: number) => {
    setItems((prev) => {
      const removed = prev[index];
      if (removed?.id) {
        setDeletedItemIds((existing) => (existing.includes(removed.id!) ? existing : [...existing, removed.id!]));
      }
      return prev.filter((_, i) => i !== index).map((item, i) => ({ ...item, sortOrder: i + 1 }));
    });
  };

  const moveItem = (index: number, direction: -1 | 1) => {
    setItems((prev) => {
      const next = index + direction;
      if (next < 0 || next >= prev.length) return prev;
      const copy = [...prev];
      [copy[index], copy[next]] = [copy[next], copy[index]];
      return copy.map((item, i) => ({ ...item, sortOrder: i + 1 }));
    });
  };

  const handleSave = async () => {
    if (!title.trim() || !templateCode.trim()) {
      setError('Title and template code are required.');
      return;
    }
    if (!ownerUserId) {
      setError('Template owner is required.');
      return;
    }
    if (!user?.id) {
      setError('Current user context is missing.');
      return;
    }
    const normalizedItems = items.map((item, index) => ({
      ...item,
      sortOrder: index + 1,
      prompt: item.prompt.trim(),
      section: item.section?.trim() || undefined,
      helpText: item.helpText?.trim() || undefined,
      dimensionUnitOfMeasure: item.dimensionUnitOfMeasure?.trim() || undefined,
    }));
    if (!normalizedItems.length || normalizedItems.some((item) => !item.prompt)) {
      setError('Every checklist item must have a prompt.');
      return;
    }
    if (normalizedItems.some((item) => item.responseType === 'Score' && !item.scoreTypeId)) {
      setError('Score response type requires a score type.');
      return;
    }
    if (normalizedItems.some((item) => item.responseType === 'Dimension' && (!item.dimensionTarget || !item.dimensionUpperLimit || !item.dimensionLowerLimit || !item.dimensionUnitOfMeasure))) {
      setError('Dimension response type requires target, lower/upper limits, and unit of measure.');
      return;
    }
    if (scopeLevel === 'PlantWorkCenter' && (!siteId || !workCenterId)) {
      setError('PlantWorkCenter scope requires both Site and Work Center.');
      return;
    }
    if (scopeLevel === 'SiteDefault' && !siteId) {
      setError('SiteDefault scope requires Site.');
      return;
    }
    if (scopeLevel === 'GlobalDefault' && (siteId || workCenterId || productionLineId)) {
      setError('GlobalDefault cannot include Site, Work Center, or Production Line.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const payload: UpsertChecklistTemplateRequest = {
        id: editing?.id,
        title: title.trim(),
        templateCode: templateCode.trim(),
        checklistType,
        scopeLevel,
        siteId: siteId || undefined,
        workCenterId: workCenterId || undefined,
        productionLineId: productionLineId || undefined,
        versionNo: Number(versionNo || '1'),
        effectiveFromUtc: toUtcIsoFromLocalDateTime(effectiveFromUtc),
        effectiveToUtc: effectiveToUtc ? toUtcIsoFromLocalDateTime(effectiveToUtc) : undefined,
        isActive,
        responseMode,
        requireFailNote,
        isSafetyProfile,
        ownerUserId,
        deletedItemIds,
        items: normalizedItems.map((item, index) => ({
          id: item.id,
          sortOrder: index + 1,
          prompt: item.prompt,
          isRequired: item.isRequired,
          section: item.section,
          responseMode: undefined,
          responseType: item.responseType,
          responseOptions: [],
          scoreTypeId: item.responseType === 'Score' ? item.scoreTypeId : undefined,
          dimensionTarget: item.responseType === 'Dimension' ? toNumeric(item.dimensionTarget) : undefined,
          dimensionUpperLimit: item.responseType === 'Dimension' ? toNumeric(item.dimensionUpperLimit) : undefined,
          dimensionLowerLimit: item.responseType === 'Dimension' ? toNumeric(item.dimensionLowerLimit) : undefined,
          dimensionUnitOfMeasure: item.responseType === 'Dimension' ? item.dimensionUnitOfMeasure : undefined,
          helpText: item.helpText,
          requireFailNote: false,
        })),
      };
      await checklistApi.upsertTemplate(payload);
      navigate('/menu/checklists');
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to save checklist template.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <AdminLayout title={isCreate ? 'New Checklist Template' : 'Edit Checklist Template'} backLabel="Templates" onBack={() => navigate('/menu/checklists')}>
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
          {error && <div style={{ color: '#c92a2a', marginBottom: 10 }}>{error}</div>}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h3 style={{ margin: 0 }}>{isCreate ? 'Create template' : `Editing ${editing?.title ?? ''}`}</h3>
            <Button appearance="primary" onClick={handleSave} disabled={saving}>
              {saving ? 'Saving...' : 'Save Template'}
            </Button>
          </div>
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
              <Label>Owner</Label>
              <Dropdown
                value={ownerNameById.get(ownerUserId) ?? (ownerUserId ? ownerUserId : 'Select owner')}
                selectedOptions={ownerUserId ? [ownerUserId] : []}
                onOptionSelect={(_, data) => setOwnerUserId(data.optionValue ?? '')}
                disabled={!canAssignAnyOwner}
              >
                {(canAssignAnyOwner ? users : [{ id: user?.id ?? '', displayName: user?.displayName ?? '', employeeNumber: user?.employeeNumber ?? '' } as AdminUser])
                  .filter((item) => !!item.id)
                  .map((item) => (
                    <Option key={item.id} value={item.id}>{item.displayName} ({item.employeeNumber})</Option>
                  ))}
              </Dropdown>
            </div>
            <div className={styles.formColumn}>
              <Label>Checklist Type</Label>
              <Dropdown value={checklistType} selectedOptions={[checklistType]} onOptionSelect={(_, data) => data.optionValue && setChecklistType(data.optionValue)}>
                {CHECKLIST_TYPES.map((type) => <Option key={type} value={type}>{type}</Option>)}
              </Dropdown>
            </div>
            <div className={styles.formColumn}>
              <Label>Scope Level</Label>
              <Dropdown value={scopeLevel} selectedOptions={[scopeLevel]} onOptionSelect={(_, data) => setScopeLevel(data.optionValue ?? scopeLevel)}>
                {SCOPE_LEVELS.map((scope) => <Option key={scope} value={scope}>{scope}</Option>)}
              </Dropdown>
            </div>
            <div className={styles.formColumn}>
              <Label>Site</Label>
              <Dropdown value={siteNameById.get(siteId) ?? 'None'} selectedOptions={[siteId]} onOptionSelect={(_, data) => setSiteId(data.optionValue ?? '')} disabled={!canCrossSite && !!siteId}>
                <Option value="">None</Option>
                {sites.map((site) => (
                  <Option key={site.id} value={site.id}>{site.name} ({site.code})</Option>
                ))}
              </Dropdown>
            </div>
            <div className={styles.formColumn}>
              <Label>Work Center</Label>
              <Dropdown value={wcNameById.get(workCenterId) ?? 'None'} selectedOptions={[workCenterId]} onOptionSelect={(_, data) => setWorkCenterId(data.optionValue ?? '')}>
                <Option value="">None</Option>
                {workCenters.map((wc) => <Option key={wc.id} value={wc.id}>{wc.name}</Option>)}
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
            <div className={styles.formColumn}><Checkbox label="Template active" checked={isActive} onChange={(_, data) => setIsActive(!!data.checked)} /></div>
            <div className={styles.formColumn}><Checkbox label="Require note on fail" checked={requireFailNote} onChange={(_, data) => setRequireFailNote(!!data.checked)} /></div>
            <div className={styles.formColumn}><Checkbox label="Safety profile" checked={isSafetyProfile} onChange={(_, data) => setIsSafetyProfile(!!data.checked)} /></div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 16, marginBottom: 8 }}>
            <Label>Questions</Label>
            <Button onClick={addItem}>Add Question</Button>
          </div>
          {groupedItems.map((group) => (
            <div key={group.section || 'unsectioned'} style={{ marginBottom: 12 }}>
              <div style={{ fontWeight: 600, marginBottom: 4 }}>{group.section || 'Unsectioned'}</div>
              {group.items.map(({ item, index }) => (
                <div key={item.id ?? `item-${index}`} style={{ border: '1px solid #d0d0d0', borderRadius: 4, padding: 12, marginBottom: 8 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                    <strong>Question {index + 1}</strong>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <Button size="small" disabled={index === 0} onClick={() => moveItem(index, -1)}>Up</Button>
                      <Button size="small" disabled={index === items.length - 1} onClick={() => moveItem(index, 1)}>Down</Button>
                      <Button size="small" appearance="subtle" onClick={() => removeItem(index)}>Remove</Button>
                    </div>
                  </div>
                  <Label>Prompt</Label>
                  <Textarea value={item.prompt} onChange={(_, data) => updateItem(index, { prompt: data.value })} rows={2} />
                  <div className={styles.formGrid} style={{ marginTop: 8 }}>
                    <div className={styles.formColumn}>
                      <Label>Section (optional)</Label>
                      <Dropdown
                        value={(item.section ?? '').trim() || 'Unsectioned'}
                        selectedOptions={item.section ? [item.section] : []}
                        onOptionSelect={(_, data) => {
                          const value = data.optionValue ?? '';
                          if (value === NEW_SECTION_OPTION) {
                            setActiveSectionEditorIndex(index);
                            updateItem(index, { section: '' });
                            return;
                          }
                          setActiveSectionEditorIndex(null);
                          updateItem(index, { section: value });
                        }}
                      >
                        <Option value="">Unsectioned</Option>
                        {sectionOptions.map((section) => (
                          <Option key={section} value={section}>{section}</Option>
                        ))}
                        <Option value={NEW_SECTION_OPTION}>+ Create new section</Option>
                      </Dropdown>
                      {(activeSectionEditorIndex === index || (!item.section && sectionOptions.length === 0)) && (
                        <Input
                          placeholder="New section name"
                          value={item.section ?? ''}
                          onChange={(_, data) => updateItem(index, { section: data.value })}
                          style={{ marginTop: 6 }}
                        />
                      )}
                    </div>
                    <div className={styles.formColumn}>
                      <Label>Response Type</Label>
                      <Dropdown
                        value={item.responseType}
                        selectedOptions={[item.responseType]}
                        onOptionSelect={(_, data) => {
                          const nextType = (data.optionValue ?? item.responseType) as QuestionResponseType;
                          updateItem(index, {
                            responseType: nextType,
                            scoreTypeId: nextType === 'Score' ? item.scoreTypeId : undefined,
                            dimensionTarget: nextType === 'Dimension' ? item.dimensionTarget : '',
                            dimensionUpperLimit: nextType === 'Dimension' ? item.dimensionUpperLimit : '',
                            dimensionLowerLimit: nextType === 'Dimension' ? item.dimensionLowerLimit : '',
                            dimensionUnitOfMeasure: nextType === 'Dimension' ? (item.dimensionUnitOfMeasure || 'inches') : '',
                          });
                        }}
                      >
                        {QUESTION_RESPONSE_TYPES.map((type) => <Option key={type} value={type}>{type}</Option>)}
                      </Dropdown>
                    </div>
                    {item.responseType === 'Score' && (
                      <div className={styles.formColumn}>
                        <Label>Score Type</Label>
                        <Dropdown
                          value={scoreTypes.find((s) => s.id === item.scoreTypeId)?.name ?? 'Select score type'}
                          selectedOptions={item.scoreTypeId ? [item.scoreTypeId] : []}
                          onOptionSelect={(_, data) => updateItem(index, { scoreTypeId: data.optionValue ?? '' })}
                        >
                          {scoreTypes.map((scoreType) => <Option key={scoreType.id} value={scoreType.id}>{scoreType.name}</Option>)}
                        </Dropdown>
                      </div>
                    )}
                    {item.responseType === 'Dimension' && (
                      <>
                        <div className={styles.formColumn}>
                          <Label>Target</Label>
                          <Input type="number" value={item.dimensionTarget ?? ''} onChange={(_, data) => updateItem(index, { dimensionTarget: data.value })} />
                        </div>
                        <div className={styles.formColumn}>
                          <Label>Upper Limit</Label>
                          <Input type="number" value={item.dimensionUpperLimit ?? ''} onChange={(_, data) => updateItem(index, { dimensionUpperLimit: data.value })} />
                        </div>
                        <div className={styles.formColumn}>
                          <Label>Lower Limit</Label>
                          <Input type="number" value={item.dimensionLowerLimit ?? ''} onChange={(_, data) => updateItem(index, { dimensionLowerLimit: data.value })} />
                        </div>
                        <div className={styles.formColumn}>
                          <Label>Unit of Measure</Label>
                          <Input value={item.dimensionUnitOfMeasure ?? 'inches'} onChange={(_, data) => updateItem(index, { dimensionUnitOfMeasure: data.value })} />
                        </div>
                      </>
                    )}
                    <div className={styles.formColumn}>
                      <Label>Help Text (optional)</Label>
                      <Input value={item.helpText ?? ''} onChange={(_, data) => updateItem(index, { helpText: data.value })} />
                    </div>
                    <div className={styles.formColumn}>
                      <Checkbox label="Required" checked={item.isRequired} onChange={(_, data) => updateItem(index, { isRequired: !!data.checked })} />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ))}
        </>
      )}
    </AdminLayout>
  );
}
