import { useState, useEffect, useCallback } from 'react';
import { Dropdown, Input, Label, Option, Spinner } from '@fluentui/react-components';
import { AdminModal } from '../../features/admin/AdminModal.tsx';
import {
  workCenterApi,
  adminWorkCenterApi,
  downtimeReasonCategoryApi,
  adminUserApi,
  downtimeEventApi,
} from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { WorkCenter, WorkCenterProductionLine, DowntimeReasonCategory, AdminUser, DowntimeEvent } from '../../types/domain.ts';

interface LogDowntimeDialogProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  /** Pre-select a work center (e.g. from Supervisor Dashboard) */
  preselectedWorkCenterId?: string;
  /** Existing event for edit mode */
  editEvent?: DowntimeEvent;
}

export function LogDowntimeDialog({
  open,
  onClose,
  onSaved,
  preselectedWorkCenterId,
  editEvent,
}: LogDowntimeDialogProps) {
  const { user } = useAuth();
  const isEdit = !!editEvent;

  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [productionLines, setProductionLines] = useState<WorkCenterProductionLine[]>([]);
  const [categories, setCategories] = useState<DowntimeReasonCategory[]>([]);
  const [operators, setOperators] = useState<AdminUser[]>([]);
  const [loadingRef, setLoadingRef] = useState(false);

  const [selectedWcId, setSelectedWcId] = useState('');
  const [selectedWcName, setSelectedWcName] = useState('');
  const [selectedPlId, setSelectedPlId] = useState('');
  const [selectedPlName, setSelectedPlName] = useState('');
  const [selectedOperatorId, setSelectedOperatorId] = useState('');
  const [selectedOperatorName, setSelectedOperatorName] = useState('');
  const [selectedReasonId, setSelectedReasonId] = useState('');
  const [selectedReasonLabel, setSelectedReasonLabel] = useState('');
  const [startDate, setStartDate] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endDate, setEndDate] = useState('');
  const [endTime, setEndTime] = useState('');

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const resetForm = useCallback(() => {
    setSelectedWcId('');
    setSelectedWcName('');
    setSelectedPlId('');
    setSelectedPlName('');
    setSelectedOperatorId('');
    setSelectedOperatorName('');
    setSelectedReasonId('');
    setSelectedReasonLabel('');
    setStartDate('');
    setStartTime('');
    setEndDate('');
    setEndTime('');
    setError('');
    setProductionLines([]);
  }, []);

  useEffect(() => {
    if (!open) return;
    setLoadingRef(true);
    Promise.all([
      workCenterApi.getWorkCenters(),
      adminUserApi.getAll(),
    ]).then(([wcs, users]) => {
      setWorkCenters(wcs);
      setOperators(users.filter((u) => u.isActive));
    }).catch(() => {})
      .finally(() => setLoadingRef(false));
  }, [open]);

  useEffect(() => {
    if (!open) {
      resetForm();
      return;
    }
    if (editEvent) {
      setSelectedOperatorId(editEvent.operatorUserId);
      setSelectedOperatorName(editEvent.operatorName);
      setSelectedReasonId(editEvent.downtimeReasonId ?? '');
      setSelectedReasonLabel(
        editEvent.downtimeReasonCategoryName && editEvent.downtimeReasonName
          ? `${editEvent.downtimeReasonCategoryName} > ${editEvent.downtimeReasonName}`
          : '',
      );
      const start = new Date(editEvent.startedAt);
      const end = new Date(editEvent.endedAt);
      setStartDate(toDateInputValue(start));
      setStartTime(toTimeInputValue(start));
      setEndDate(toDateInputValue(end));
      setEndTime(toTimeInputValue(end));
    } else {
      const now = new Date();
      const today = toDateInputValue(now);
      setStartDate(today);
      setEndDate(today);
      setStartTime(toTimeInputValue(now));
      setEndTime(toTimeInputValue(now));
    }
  }, [open, editEvent, resetForm]);

  // Load production lines when WC changes
  useEffect(() => {
    if (!selectedWcId) {
      setProductionLines([]);
      return;
    }
    adminWorkCenterApi.getProductionLineConfigs(selectedWcId)
      .then(setProductionLines)
      .catch(() => setProductionLines([]));
  }, [selectedWcId]);

  // Pre-select WC
  useEffect(() => {
    if (!open || !preselectedWorkCenterId || workCenters.length === 0) return;
    const wc = workCenters.find((w) => w.id === preselectedWorkCenterId);
    if (wc && !selectedWcId) {
      setSelectedWcId(wc.id);
      setSelectedWcName(wc.name);
    }
  }, [open, preselectedWorkCenterId, workCenters, selectedWcId]);

  // For edit mode: set WC from the event's production line after PLs load
  useEffect(() => {
    if (!editEvent || productionLines.length === 0) return;
    const pl = productionLines.find((p) => p.id === editEvent.workCenterProductionLineId);
    if (pl) {
      setSelectedPlId(pl.id);
      setSelectedPlName(pl.displayName || pl.productionLineName);
    }
  }, [editEvent, productionLines]);

  // Load downtime reason categories for the plant
  useEffect(() => {
    if (!user?.defaultSiteId || !open) return;
    downtimeReasonCategoryApi.getAll(user.defaultSiteId)
      .then(setCategories)
      .catch(() => setCategories([]));
  }, [user?.defaultSiteId, open]);

  // For edit mode: set WC from event
  useEffect(() => {
    if (!editEvent || workCenters.length === 0) return;
    // Find the WC that owns this production line -- we need to iterate
    // We'll load all WCs and try to find the matching production line
    const findWc = async () => {
      for (const wc of workCenters) {
        try {
          const pls = await adminWorkCenterApi.getProductionLineConfigs(wc.id);
          const match = pls.find((pl: WorkCenterProductionLine) => pl.id === editEvent.workCenterProductionLineId);
          if (match) {
            setSelectedWcId(wc.id);
            setSelectedWcName(wc.name);
            return;
          }
        } catch { /* ignore */ }
      }
    };
    if (!selectedWcId) findWc();
  }, [editEvent, workCenters, selectedWcId]);

  const buildReasonOptions = () => {
    const options: { id: string; label: string; categoryName: string; reasonName: string }[] = [];
    for (const cat of categories) {
      for (const reason of cat.reasons.filter((r) => r.isActive)) {
        options.push({
          id: reason.id,
          label: `${cat.name} > ${reason.name}`,
          categoryName: cat.name,
          reasonName: reason.name,
        });
      }
    }
    return options;
  };

  const validate = (): string | null => {
    if (!selectedWcId) return 'Please select a work center.';
    if (!selectedPlId) return 'Please select a production line.';
    if (!selectedOperatorId) return 'Please select an operator.';
    if (!startDate || !startTime) return 'Please enter a start date and time.';
    if (!endDate || !endTime) return 'Please enter an end date and time.';
    const start = new Date(`${startDate}T${startTime}`);
    const end = new Date(`${endDate}T${endTime}`);
    if (isNaN(start.getTime()) || isNaN(end.getTime())) return 'Invalid date/time values.';
    if (end <= start) return 'End time must be after start time.';
    if (!selectedReasonId) return 'Please select a downtime reason.';
    return null;
  };

  const handleConfirm = async () => {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setError('');
    setSaving(true);
    try {
      const startedAt = new Date(`${startDate}T${startTime}`).toISOString();
      const endedAt = new Date(`${endDate}T${endTime}`).toISOString();

      if (isEdit && editEvent) {
        await downtimeEventApi.update(editEvent.id, {
          operatorUserId: selectedOperatorId,
          startedAt,
          endedAt,
          downtimeReasonId: selectedReasonId || undefined,
        });
      } else {
        await downtimeEventApi.create({
          workCenterProductionLineId: selectedPlId,
          operatorUserId: selectedOperatorId,
          startedAt,
          endedAt,
          downtimeReasonId: selectedReasonId || undefined,
          isAutoGenerated: false,
        });
      }
      onSaved();
      onClose();
    } catch {
      setError('Failed to save downtime event. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const reasonOptions = buildReasonOptions();

  if (loadingRef) {
    return (
      <AdminModal
        open={open}
        title={isEdit ? 'Edit Downtime Event' : 'Log Downtime'}
        onConfirm={() => {}}
        onCancel={onClose}
        confirmDisabled
        wide
      >
        <div style={{ display: 'flex', justifyContent: 'center', padding: 32 }}>
          <Spinner size="medium" label="Loading..." />
        </div>
      </AdminModal>
    );
  }

  return (
    <AdminModal
      open={open}
      title={isEdit ? 'Edit Downtime Event' : 'Log Downtime'}
      onConfirm={handleConfirm}
      onCancel={onClose}
      confirmLabel={isEdit ? 'Save Changes' : 'Log Downtime'}
      loading={saving}
      error={error}
      confirmDisabled={saving}
      wide
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <Label weight="semibold">Work Center</Label>
            <Dropdown
              placeholder="Select work center..."
              value={selectedWcName}
              selectedOptions={selectedWcId ? [selectedWcId] : []}
              onOptionSelect={(_, data) => {
                setSelectedWcId(data.optionValue ?? '');
                setSelectedWcName(data.optionText ?? '');
                setSelectedPlId('');
                setSelectedPlName('');
              }}
              disabled={isEdit}
              style={{ width: '100%' }}
            >
              {workCenters.map((wc) => (
                <Option key={wc.id} value={wc.id} text={wc.name}>{wc.name}</Option>
              ))}
            </Dropdown>
          </div>
          <div>
            <Label weight="semibold">Production Line</Label>
            <Dropdown
              placeholder={selectedWcId ? 'Select production line...' : 'Select a work center first'}
              value={selectedPlName}
              selectedOptions={selectedPlId ? [selectedPlId] : []}
              onOptionSelect={(_, data) => {
                setSelectedPlId(data.optionValue ?? '');
                setSelectedPlName(data.optionText ?? '');
              }}
              disabled={!selectedWcId || isEdit}
              style={{ width: '100%' }}
            >
              {productionLines.map((pl) => (
                <Option key={pl.id} value={pl.id} text={pl.displayName || pl.productionLineName}>
                  {pl.displayName || pl.productionLineName}
                </Option>
              ))}
            </Dropdown>
          </div>
        </div>

        <div>
          <Label weight="semibold">Operator</Label>
          <Dropdown
            placeholder="Select operator..."
            value={selectedOperatorName}
            selectedOptions={selectedOperatorId ? [selectedOperatorId] : []}
            onOptionSelect={(_, data) => {
              setSelectedOperatorId(data.optionValue ?? '');
              setSelectedOperatorName(data.optionText ?? '');
            }}
            style={{ width: '100%' }}
          >
            {operators.map((op) => (
              <Option key={op.id} value={op.id} text={op.displayName}>
                {op.displayName} ({op.employeeNumber})
              </Option>
            ))}
          </Dropdown>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <Label weight="semibold">Start Date</Label>
            <Input
              type="date"
              value={startDate}
              onChange={(_, d) => setStartDate(d.value)}
              style={{ width: '100%' }}
            />
          </div>
          <div>
            <Label weight="semibold">Start Time</Label>
            <Input
              type="time"
              value={startTime}
              onChange={(_, d) => setStartTime(d.value)}
              style={{ width: '100%' }}
            />
          </div>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <Label weight="semibold">End Date</Label>
            <Input
              type="date"
              value={endDate}
              onChange={(_, d) => setEndDate(d.value)}
              style={{ width: '100%' }}
            />
          </div>
          <div>
            <Label weight="semibold">End Time</Label>
            <Input
              type="time"
              value={endTime}
              onChange={(_, d) => setEndTime(d.value)}
              style={{ width: '100%' }}
            />
          </div>
        </div>

        <div>
          <Label weight="semibold">Downtime Reason</Label>
          <Dropdown
            placeholder="Select reason..."
            value={selectedReasonLabel}
            selectedOptions={selectedReasonId ? [selectedReasonId] : []}
            onOptionSelect={(_, data) => {
              setSelectedReasonId(data.optionValue ?? '');
              setSelectedReasonLabel(data.optionText ?? '');
            }}
            style={{ width: '100%' }}
          >
            {reasonOptions.map((r) => (
              <Option key={r.id} value={r.id} text={r.label}>{r.label}</Option>
            ))}
          </Dropdown>
        </div>
      </div>
    </AdminModal>
  );
}

function toDateInputValue(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function toTimeInputValue(d: Date): string {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
