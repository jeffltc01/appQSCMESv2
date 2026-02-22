import { useState, useEffect, useCallback, useMemo } from 'react';
import { Button, Dropdown, Input, Label, Option, Spinner } from '@fluentui/react-components';
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { shiftScheduleApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { Plant, ShiftSchedule } from '../../types/domain.ts';
import styles from './ShiftScheduleScreen.module.css';

const DAYS = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'] as const;
const DAY_LABELS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const SHORT_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

function getSundayOfWeek(date: Date): Date {
  const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  d.setDate(d.getDate() - d.getDay());
  return d;
}

function getWeekNumber(date: Date): number {
  const startOfYear = new Date(date.getFullYear(), 0, 1);
  const dayOfYear = Math.floor((date.getTime() - startOfYear.getTime()) / 86400000) + 1;
  return Math.ceil((dayOfYear + startOfYear.getDay()) / 7);
}

function toDateStr(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function formatWeekLabel(sunday: Date): string {
  const sat = new Date(sunday);
  sat.setDate(sat.getDate() + 6);
  const wk = getWeekNumber(sunday);
  const sMonth = SHORT_MONTHS[sunday.getMonth()];
  const eMonth = SHORT_MONTHS[sat.getMonth()];
  const range = sMonth === eMonth
    ? `${sMonth} ${sunday.getDate()} - ${sat.getDate()}`
    : `${sMonth} ${sunday.getDate()} - ${eMonth} ${sat.getDate()}`;
  return `Week ${wk} (${range})`;
}

function formatWeekLabelFromStr(dateStr: string): string {
  const [y, m, d] = dateStr.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  const sunday = date.getDay() === 0 ? date : getSundayOfWeek(date);
  return formatWeekLabel(sunday);
}

interface WeekOption { value: string; label: string }

function buildWeekOptions(): WeekOption[] {
  const currentSunday = getSundayOfWeek(new Date());
  const options: WeekOption[] = [];
  for (let offset = -4; offset <= 26; offset++) {
    const sunday = new Date(currentSunday);
    sunday.setDate(sunday.getDate() + offset * 7);
    options.push({ value: toDateStr(sunday), label: formatWeekLabel(sunday) });
  }
  return options;
}

function currentSundayStr(): string {
  return toDateStr(getSundayOfWeek(new Date()));
}

interface DraftSchedule {
  effectiveDate: string;
  days: { hours: string; breakMinutes: string }[];
}

const PRESET_5x8 = DAYS.map((_, i) =>
  i < 5 ? { hours: '8', breakMinutes: '30' } : { hours: '0', breakMinutes: '0' },
);
const PRESET_4x10 = DAYS.map((_, i) =>
  i < 4 ? { hours: '10', breakMinutes: '30' } : { hours: '0', breakMinutes: '0' },
);

const emptyDraft = (): DraftSchedule => ({
  effectiveDate: currentSundayStr(),
  days: DAYS.map(() => ({ hours: '0', breakMinutes: '0' })),
});

export function ShiftScheduleScreen() {
  const { user } = useAuth();
  const showPlantSelector = (user?.roleTier ?? 99) <= 2;

  const [plants, setPlants] = useState<Plant[]>([]);
  const [selectedPlantId, setSelectedPlantId] = useState(user?.defaultSiteId ?? '');
  const [schedules, setSchedules] = useState<ShiftSchedule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<DraftSchedule>(emptyDraft());
  const [saving, setSaving] = useState(false);

  const weekOptions = useMemo(buildWeekOptions, []);

  useEffect(() => {
    siteApi.getSites().then(setPlants).catch(() => {});
  }, []);

  const load = useCallback(async () => {
    if (!selectedPlantId) return;
    setLoading(true);
    setError(null);
    try {
      setSchedules(await shiftScheduleApi.getAll(selectedPlantId));
    } catch (err: unknown) {
      const msg = (err && typeof err === 'object' && 'message' in err) ? (err as { message: string }).message : 'Failed to load shift schedules.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [selectedPlantId]);

  useEffect(() => { load(); }, [load]);

  const handleSave = async () => {
    setSaving(true);
    try {
      const body: Record<string, unknown> = {
        plantId: selectedPlantId,
        effectiveDate: draft.effectiveDate,
      };
      DAYS.forEach((day, i) => {
        body[`${day}Hours`] = parseFloat(draft.days[i].hours) || 0;
        body[`${day}BreakMinutes`] = parseInt(draft.days[i].breakMinutes, 10) || 0;
      });
      if (editingId) {
        await shiftScheduleApi.update(editingId, body as never);
      } else {
        await shiftScheduleApi.create(body as never);
      }
      setShowForm(false);
      setEditingId(null);
      setDraft(emptyDraft());
      await load();
    } catch { alert('Failed to save schedule.'); }
    finally { setSaving(false); }
  };

  const handleEdit = (s: ShiftSchedule) => {
    setEditingId(s.id);
    setDraft({
      effectiveDate: s.effectiveDate,
      days: DAYS.map(day => ({
        hours: String(s[`${day}Hours` as keyof ShiftSchedule] as number),
        breakMinutes: String(s[`${day}BreakMinutes` as keyof ShiftSchedule] as number),
      })),
    });
    setShowForm(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this shift schedule?')) return;
    try { await shiftScheduleApi.delete(id); await load(); }
    catch { alert('Failed to delete.'); }
  };

  const updateDay = (idx: number, field: 'hours' | 'breakMinutes', value: string) => {
    setDraft(prev => {
      const days = [...prev.days];
      days[idx] = { ...days[idx], [field]: value };
      return { ...prev, days };
    });
  };

  return (
    <AdminLayout title="Shift Schedule">
      {loading ? (
        <div style={{ textAlign: 'center', padding: 48 }}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
          <div className={styles.filterBar}>
            {showPlantSelector && plants.length > 0 ? (
              <>
                <Label weight="semibold">Plant</Label>
                <Dropdown
                  value={plants.find(p => p.id === selectedPlantId)?.name ?? ''}
                  selectedOptions={[selectedPlantId]}
                  onOptionSelect={(_, d) => { if (d.optionValue) setSelectedPlantId(d.optionValue); }}
                  style={{ minWidth: 180 }}
                >
                  {plants.map(p => <Option key={p.id} value={p.id}>{p.name}</Option>)}
                </Dropdown>
              </>
            ) : plants.length > 0 && (
              <>
                <Label weight="semibold">Plant</Label>
                <span style={{ fontWeight: 600, fontSize: 14 }}>
                  {plants.find(p => p.id === selectedPlantId)?.name ?? ''}
                </span>
              </>
            )}
            <Button
              appearance="primary"
              icon={<AddRegular />}
              onClick={() => { setEditingId(null); setShowForm(true); setDraft(emptyDraft()); }}
              style={{ borderRadius: 0, marginLeft: 'auto' }}
            >
              New Schedule
            </Button>
          </div>

          {showForm && (
            <div className={styles.formCard}>
              <div className={styles.formHeader}>{editingId ? 'Edit Shift Schedule' : 'New Shift Schedule'}</div>
              <div className={styles.formRow}>
                <div className={styles.formField}>
                  <Label weight="semibold">Week</Label>
                  <Dropdown
                    value={weekOptions.find(o => o.value === draft.effectiveDate)?.label ?? formatWeekLabelFromStr(draft.effectiveDate)}
                    selectedOptions={[draft.effectiveDate]}
                    onOptionSelect={(_, d) => {
                      if (d.optionValue) setDraft(prev => ({ ...prev, effectiveDate: d.optionValue as string }));
                    }}
                    disabled={!!editingId}
                    style={{ minWidth: 260 }}
                  >
                    {weekOptions.map(o => (
                      <Option key={o.value} value={o.value}>{o.label}</Option>
                    ))}
                  </Dropdown>
                </div>
                <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
                  <Button
                    size="small"
                    onClick={() => setDraft(prev => ({ ...prev, days: PRESET_5x8.map(d => ({ ...d })) }))}
                    style={{ borderRadius: 0 }}
                  >
                    5x8s Preset
                  </Button>
                  <Button
                    size="small"
                    onClick={() => setDraft(prev => ({ ...prev, days: PRESET_4x10.map(d => ({ ...d })) }))}
                    style={{ borderRadius: 0 }}
                  >
                    4x10s Preset
                  </Button>
                </div>
              </div>
              <table className={styles.dayTable}>
                <thead>
                  <tr>
                    <th>Day</th>
                    <th>Hours</th>
                    <th>Break (min)</th>
                    <th>Net Minutes</th>
                  </tr>
                </thead>
                <tbody>
                  {DAYS.map((_, i) => {
                    const hrs = parseFloat(draft.days[i].hours) || 0;
                    const brk = parseInt(draft.days[i].breakMinutes, 10) || 0;
                    const net = Math.max(0, hrs * 60 - brk);
                    return (
                      <tr key={i}>
                        <td className={styles.dayLabel}>{DAY_LABELS[i]}</td>
                        <td>
                          <Input
                            size="small"
                            type="number"
                            value={draft.days[i].hours}
                            onChange={(_, d) => updateDay(i, 'hours', d.value)}
                            style={{ width: 80 }}
                          />
                        </td>
                        <td>
                          <Input
                            size="small"
                            type="number"
                            value={draft.days[i].breakMinutes}
                            onChange={(_, d) => updateDay(i, 'breakMinutes', d.value)}
                            style={{ width: 80 }}
                          />
                        </td>
                        <td className={net > 0 ? styles.netPositive : styles.netZero}>
                          {net}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                <Button appearance="primary" onClick={handleSave} disabled={saving} style={{ borderRadius: 0 }}>
                  {saving ? <Spinner size="tiny" /> : 'Save'}
                </Button>
                <Button appearance="outline" onClick={() => { setShowForm(false); setEditingId(null); }} style={{ borderRadius: 0 }}>
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {error ? (
            <div style={{ textAlign: 'center', padding: 48, color: '#c92a2a' }}>
              {error}
              <div style={{ marginTop: 12 }}>
                <Button appearance="outline" onClick={load} style={{ borderRadius: 0 }}>Retry</Button>
              </div>
            </div>
          ) : schedules.length === 0 && !showForm ? (
            <div style={{ textAlign: 'center', padding: 48, color: '#868e96' }}>
              No shift schedules configured. OEE cannot be calculated without a shift schedule.
            </div>
          ) : (
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Week</th>
                  {DAY_LABELS.map(d => <th key={d}>{d}</th>)}
                  <th>Created</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {schedules.map(s => (
                  <tr key={s.id}>
                    <td className={styles.dateCell}>{formatWeekLabelFromStr(s.effectiveDate)}</td>
                    {DAYS.map(day => {
                      const hrs = s[`${day}Hours` as keyof ShiftSchedule] as number;
                      const brk = s[`${day}BreakMinutes` as keyof ShiftSchedule] as number;
                      return (
                        <td key={day} className={hrs > 0 ? styles.dayActive : styles.dayOff}>
                          {hrs > 0 ? `${hrs}h / ${brk}m` : '--'}
                        </td>
                      );
                    })}
                    <td className={styles.metaCell}>
                      {s.createdByName ?? ''}
                    </td>
                    <td style={{ whiteSpace: 'nowrap' }}>
                      <Button
                        size="small"
                        icon={<EditRegular />}
                        appearance="subtle"
                        onClick={() => handleEdit(s)}
                      />
                      <Button
                        size="small"
                        icon={<DeleteRegular />}
                        appearance="subtle"
                        onClick={() => handleDelete(s.id)}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </AdminLayout>
  );
}
