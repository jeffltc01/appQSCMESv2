import { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogActions,
  Input,
  Label,
  Select,
  Spinner,
} from '@fluentui/react-components';
import { DismissRegular, FlagFilled, FlagRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { logViewerApi, siteApi, adminAnnotationTypeApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { Plant, AdminAnnotationType } from '../../types/domain.ts';
import type {
  RollsLogEntry,
  FitupLogEntry,
  HydroLogEntry,
  RtXrayLogEntry,
  SpotXrayLogEntry,
  SpotXrayShotCount,
  LogAnnotationBadge,
} from '../../types/domain.ts';
import { formatDateForInput, formatShortDateTime } from '../../utils/dateFormat.ts';
import styles from './ProductionLogsScreen.module.css';

type LogType = 'rolls' | 'fitup' | 'hydro' | 'rt-xray' | 'spot-xray';

const LOG_TYPE_OPTIONS: { value: LogType; label: string }[] = [
  { value: 'rolls', label: 'Rolls Log' },
  { value: 'fitup', label: 'Fitup Log' },
  { value: 'hydro', label: 'Hydro Log' },
  { value: 'rt-xray', label: 'Realtime Xray Log' },
  { value: 'spot-xray', label: 'Spot Xray Log' },
];

type AnyLogEntry = RollsLogEntry | FitupLogEntry | HydroLogEntry | RtXrayLogEntry | SpotXrayLogEntry;

function isDateInputValue(value: string | null): value is string {
  if (!value) return false;
  return /^\d{4}-\d{2}-\d{2}$/.test(value);
}

function ResultCell({ result }: { result?: string }) {
  if (!result) return <span className={styles.resultUnknown}>—</span>;
  const lower = result.toLowerCase();
  if (lower.startsWith('accept') || lower === 'pass' || lower === 'go')
    return <span className={styles.resultAccept}>{result}</span>;
  if (lower.startsWith('reject') || lower === 'fail' || lower === 'nogo')
    return <span className={styles.resultReject}>{result}</span>;
  return <span className={styles.resultUnknown}>{result}</span>;
}

function AnnotationBadges({
  badges,
  onAdd,
  onViewDetail,
}: {
  badges: LogAnnotationBadge[];
  onAdd: () => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <div className={styles.annotCell}>
      <button
        className={styles.addAnnotBtn}
        onClick={onAdd}
        title="Add annotation"
        type="button"
      >
        +
      </button>
      {badges.length > 0
        ? badges.map((b, i) => (
            <button
              key={i}
              className={styles.flagBtn}
              onClick={() => onViewDetail(b)}
              title={`${b.typeName} — click to view details`}
              type="button"
            >
              <FlagFilled
                fontSize={18}
                className={styles.flagActive}
                style={{ color: b.color }}
              />
            </button>
          ))
        : <FlagRegular fontSize={18} className={styles.flagInactive} />}
    </div>
  );
}

export function ProductionLogsScreen() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [sites, setSites] = useState<Plant[]>([]);
  const [annotationTypes, setAnnotationTypes] = useState<AdminAnnotationType[]>([]);

  const initialLogType = (searchParams.get('logType') as LogType) || '';
  const initialStartDate = searchParams.get('startDate');
  const initialEndDate = searchParams.get('endDate');
  const [logType, setLogType] = useState<LogType | ''>(initialLogType);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [startDate, setStartDate] = useState(
    isDateInputValue(initialStartDate)
      ? initialStartDate
      : formatDateForInput(new Date(Date.now() - 7 * 86400000)),
  );
  const [endDate, setEndDate] = useState(
    isDateInputValue(initialEndDate)
      ? initialEndDate
      : formatDateForInput(new Date()),
  );

  const [entries, setEntries] = useState<AnyLogEntry[]>([]);
  const [shotCounts, setShotCounts] = useState<SpotXrayShotCount[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [hasSearched, setHasSearched] = useState(false);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 100;

  const [annotModalOpen, setAnnotModalOpen] = useState(false);
  const [annotRecordId, setAnnotRecordId] = useState('');
  const [annotTypeId, setAnnotTypeId] = useState('');
  const [annotNotes, setAnnotNotes] = useState('');
  const [annotSaving, setAnnotSaving] = useState(false);
  const [annotError, setAnnotError] = useState('');

  const [detailBadge, setDetailBadge] = useState<LogAnnotationBadge | null>(null);

  useEffect(() => {
    siteApi.getSites().then(setSites).catch(() => {});
    adminAnnotationTypeApi.getAll().then(setAnnotationTypes).catch(() => {});
  }, []);

  const fetchLog = useCallback(async () => {
    if (!logType || !siteId) return;
    setLoading(true);
    setError('');
    setHasSearched(true);
    setShotCounts([]);
    setPage(1);

    try {
      switch (logType) {
        case 'rolls': {
          const data = await logViewerApi.getRollsLog(siteId, startDate, endDate);
          setEntries(data);
          break;
        }
        case 'fitup': {
          const data = await logViewerApi.getFitupLog(siteId, startDate, endDate);
          setEntries(data);
          break;
        }
        case 'hydro': {
          const data = await logViewerApi.getHydroLog(siteId, startDate, endDate);
          setEntries(data);
          break;
        }
        case 'rt-xray': {
          const data = await logViewerApi.getRtXrayLog(siteId, startDate, endDate);
          setEntries(data);
          break;
        }
        case 'spot-xray': {
          const resp = await logViewerApi.getSpotXrayLog(siteId, startDate, endDate);
          setEntries(resp.entries);
          setShotCounts(resp.shotCounts);
          break;
        }
      }
    } catch {
      setError('Failed to load log data.');
    } finally {
      setLoading(false);
    }
  }, [logType, siteId, startDate, endDate]);

  useEffect(() => {
    if (initialLogType && siteId) {
      fetchLog();
    }
    // Only run on mount when navigated with a logType param
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const openAnnotModal = (recordId: string) => {
    setAnnotRecordId(recordId);
    const operatorCreatable = annotationTypes.filter((t) => t.operatorCanCreate);
    setAnnotTypeId(operatorCreatable[0]?.id ?? '');
    setAnnotNotes('');
    setAnnotError('');
    setAnnotModalOpen(true);
  };

  const handleAnnotSave = async () => {
    if (!annotRecordId || !annotTypeId || !user) return;
    setAnnotSaving(true);
    setAnnotError('');
    try {
      await logViewerApi.createAnnotation({
        productionRecordId: annotRecordId,
        annotationTypeId: annotTypeId,
        notes: annotNotes || undefined,
        initiatedByUserId: user.id,
      });
      setAnnotModalOpen(false);
      fetchLog();
    } catch {
      setAnnotError('Failed to save annotation.');
    } finally {
      setAnnotSaving(false);
    }
  };

  const openDetail = (badge: LogAnnotationBadge) => {
    setDetailBadge(badge);
  };

  const operatorCreatableTypes = annotationTypes.filter((t) => t.operatorCanCreate);
  const handleBack = useCallback(() => {
    navigate(-1);
  }, [navigate]);

  return (
    <AdminLayout
      title="Log Viewer"
      backLabel="Back"
      onBack={handleBack}
      nlqContext={{
        screenKey: 'log-viewer',
        activeFilterTotalCount: entries.length,
        filterSummary: `logType=${logType || 'none'}, siteId=${siteId || 'none'}, start=${startDate}, end=${endDate}`,
      }}
    >
      <div className={styles.filterBar}>
        <div className={styles.filterField}>
          <label>Log Type</label>
          <Select
            value={logType}
            onChange={(_, d) => {
              setLogType(d.value as LogType);
              setEntries([]);
              setShotCounts([]);
              setHasSearched(false);
            }}
            style={{ minWidth: 180 }}
          >
            <option value="">-- Select Log --</option>
            {LOG_TYPE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>Site</label>
          <Select
            value={siteId}
            onChange={(_, d) => setSiteId(d.value)}
            style={{ minWidth: 160 }}
          >
            <option value="">-- Select Site --</option>
            {sites.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>Start Date</label>
          <Input
            type="date"
            value={startDate}
            onChange={(_, d) => setStartDate(d.value)}
            style={{ minWidth: 140, borderRadius: 10, backgroundColor: '#ffffff' }}
          />
        </div>
        <div className={styles.filterField}>
          <label>End Date</label>
          <Input
            type="date"
            value={endDate}
            onChange={(_, d) => setEndDate(d.value)}
            style={{ minWidth: 140, borderRadius: 10, backgroundColor: '#ffffff' }}
          />
        </div>
        <button
          className={styles.goButton}
          onClick={fetchLog}
          disabled={!logType || !siteId || loading}
          type="button"
        >
          Go
        </button>
      </div>

      {error && <div style={{ color: '#d13438', marginBottom: 12 }}>{error}</div>}

      {logType === 'spot-xray' && shotCounts.length > 0 && (
        <div className={styles.shotCountsBar}>
          <span className={styles.shotCountsLabel}>Shot Counts:</span>
          {shotCounts.map((sc) => (
            <span key={sc.date} className={styles.shotCountBadge}>
              {sc.date} - {sc.count}
            </span>
          ))}
        </div>
      )}

      {loading ? (
        <div className={styles.loadingState}>
          <Spinner size="medium" label="Loading..." />
        </div>
      ) : !hasSearched ? (
        <div className={styles.emptyState}>
          {logType ? 'Click Go to load log data.' : 'Select a log type to begin.'}
        </div>
      ) : entries.length === 0 ? (
        <div className={styles.emptyState}>No records found for the selected criteria.</div>
      ) : (
        <>
          {(() => {
            const totalPages = Math.ceil(entries.length / PAGE_SIZE);
            const start = (page - 1) * PAGE_SIZE;
            const pageEntries = entries.slice(start, start + PAGE_SIZE);
            return (
              <>
                <div className={styles.countLabel}>
                  Showing {start + 1}–{Math.min(start + PAGE_SIZE, entries.length)} of {entries.length} records
                </div>
                <div className={styles.tableContainer}>
                  {logType === 'rolls' && (
                    <RollsTable entries={pageEntries as RollsLogEntry[]} onAddAnnot={openAnnotModal} onViewDetail={openDetail} />
                  )}
                  {logType === 'fitup' && (
                    <FitupTable entries={pageEntries as FitupLogEntry[]} onAddAnnot={openAnnotModal} onViewDetail={openDetail} />
                  )}
                  {logType === 'hydro' && (
                    <HydroTable entries={pageEntries as HydroLogEntry[]} onAddAnnot={openAnnotModal} onViewDetail={openDetail} />
                  )}
                  {logType === 'rt-xray' && (
                    <RtXrayTable entries={pageEntries as RtXrayLogEntry[]} onAddAnnot={openAnnotModal} onViewDetail={openDetail} />
                  )}
                  {logType === 'spot-xray' && (
                    <SpotXrayTable entries={pageEntries as SpotXrayLogEntry[]} onAddAnnot={openAnnotModal} onViewDetail={openDetail} />
                  )}
                </div>
                {totalPages > 1 && (
                  <div className={styles.pagination}>
                    <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                      Previous
                    </Button>
                    <span className={styles.pageInfo}>Page {page} of {totalPages}</span>
                    <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                      Next
                    </Button>
                  </div>
                )}
              </>
            );
          })()}
        </>
      )}

      <AdminModal
        open={annotModalOpen}
        title="Add Annotation"
        onConfirm={handleAnnotSave}
        onCancel={() => setAnnotModalOpen(false)}
        confirmLabel="Save"
        loading={annotSaving}
        error={annotError}
        confirmDisabled={!annotTypeId}
      >
        <div className={styles.annotModalRow}>
          <Label>Annotation Type</Label>
          <Select
            value={annotTypeId}
            onChange={(_, d) => setAnnotTypeId(d.value)}
          >
            {operatorCreatableTypes.map((t) => (
              <option key={t.id} value={t.id}>{t.name}</option>
            ))}
          </Select>
          <Label>Notes (optional)</Label>
          <Input
            value={annotNotes}
            onChange={(_, d) => setAnnotNotes(d.value)}
            placeholder="Enter notes..."
          />
        </div>
      </AdminModal>

      <Dialog open={detailBadge !== null} onOpenChange={(_, data) => { if (!data.open) setDetailBadge(null); }}>
        <DialogSurface className={styles.detailSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <Button
                  appearance="subtle"
                  aria-label="close"
                  icon={<DismissRegular />}
                  onClick={() => setDetailBadge(null)}
                />
              }
            >
              Annotation Details
            </DialogTitle>
            <DialogContent>
              {detailBadge && (
                <div className={styles.detailGrid}>
                  <div className={styles.detailLabel}>Type</div>
                  <div className={styles.detailValue}>
                    <span className={styles.detailTypeDot} style={{ background: detailBadge.color }} />
                    {detailBadge.typeName}
                  </div>

                  <div className={styles.detailLabel}>Status</div>
                  <div className={styles.detailValue}>
                    <span className={detailBadge.status === 'Open' ? styles.statusOpen : styles.statusResolved}>
                      {detailBadge.status}
                    </span>
                  </div>

                  <div className={styles.detailLabel}>Created</div>
                  <div className={styles.detailValue}>{formatShortDateTime(detailBadge.createdAt)}</div>

                  <div className={styles.detailLabel}>Initiated By</div>
                  <div className={styles.detailValue}>{detailBadge.initiatedByName || '—'}</div>

                  {detailBadge.notes && (
                    <>
                      <div className={styles.detailLabel}>Notes</div>
                      <div className={styles.detailValue}>{detailBadge.notes}</div>
                    </>
                  )}

                  {detailBadge.resolvedByName && (
                    <>
                      <div className={styles.detailLabel}>Resolved By</div>
                      <div className={styles.detailValue}>{detailBadge.resolvedByName}</div>
                    </>
                  )}

                  {detailBadge.resolvedNotes && (
                    <>
                      <div className={styles.detailLabel}>Resolution Notes</div>
                      <div className={styles.detailValue}>{detailBadge.resolvedNotes}</div>
                    </>
                  )}
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="primary" onClick={() => setDetailBadge(null)}>Close</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </AdminLayout>
  );
}

function RollsTable({
  entries,
  onAddAnnot,
  onViewDetail,
}: {
  entries: RollsLogEntry[];
  onAddAnnot: (id: string) => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Date/Time (MT)</th>
          <th>Coil / Heat / Lot</th>
          <th>Thickness</th>
          <th>Shell Code</th>
          <th>Tank Size</th>
          <th>Welder(s)</th>
          <th>Annot</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e) => (
          <tr key={e.id}>
            <td style={{ whiteSpace: 'nowrap' }}>{formatShortDateTime(e.timestamp)}</td>
            <td>{e.coilHeatLot}</td>
            <td><ResultCell result={e.thickness} /></td>
            <td>{e.shellCode}</td>
            <td>{e.tankSize ?? ''}</td>
            <td>{e.welders.join(', ')}</td>
            <td>
              <AnnotationBadges badges={e.annotations} onAdd={() => onAddAnnot(e.id)} onViewDetail={onViewDetail} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function FitupTable({
  entries,
  onAddAnnot,
  onViewDetail,
}: {
  entries: FitupLogEntry[];
  onAddAnnot: (id: string) => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Date/Time (MT)</th>
          <th>Head No. 1</th>
          <th>Head No. 2</th>
          <th>Shell No. 1</th>
          <th>Shell No. 2</th>
          <th>Shell No. 3</th>
          <th>Alpha Code</th>
          <th>Tank Size</th>
          <th>Welder(s)</th>
          <th>Annot</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e) => (
          <tr key={e.id}>
            <td style={{ whiteSpace: 'nowrap' }}>{formatShortDateTime(e.timestamp)}</td>
            <td>{e.headNo1 ?? ''}</td>
            <td>{e.headNo2 ?? ''}</td>
            <td>{e.shellNo1 ?? ''}</td>
            <td>{e.shellNo2 ?? ''}</td>
            <td>{e.shellNo3 ?? ''}</td>
            <td>{e.alphaCode ?? ''}</td>
            <td>{e.tankSize ?? ''}</td>
            <td>{e.welders.join(', ')}</td>
            <td>
              <AnnotationBadges badges={e.annotations} onAdd={() => onAddAnnot(e.id)} onViewDetail={onViewDetail} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function HydroTable({
  entries,
  onAddAnnot,
  onViewDetail,
}: {
  entries: HydroLogEntry[];
  onAddAnnot: (id: string) => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Date/Time (MT)</th>
          <th>Nameplate</th>
          <th>Alpha Code</th>
          <th>Tank Size</th>
          <th>Operator</th>
          <th>Welder(s)</th>
          <th>Result</th>
          <th>Defect(s)</th>
          <th>Annot</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e) => (
          <tr key={e.id}>
            <td style={{ whiteSpace: 'nowrap' }}>{formatShortDateTime(e.timestamp)}</td>
            <td>{e.nameplate ?? ''}</td>
            <td>{e.alphaCode ?? ''}</td>
            <td>{e.tankSize ?? ''}</td>
            <td>{e.operator}</td>
            <td>{e.welders.join(', ')}</td>
            <td><ResultCell result={e.result} /></td>
            <td>{e.defectCount}</td>
            <td>
              <AnnotationBadges badges={e.annotations} onAdd={() => onAddAnnot(e.id)} onViewDetail={onViewDetail} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function RtXrayTable({
  entries,
  onAddAnnot,
  onViewDetail,
}: {
  entries: RtXrayLogEntry[];
  onAddAnnot: (id: string) => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Date/Time (MT)</th>
          <th>Shell Code</th>
          <th>Tank Size</th>
          <th>Operator</th>
          <th>Result</th>
          <th>Defect(s)</th>
          <th>Annot</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e) => (
          <tr key={e.id}>
            <td style={{ whiteSpace: 'nowrap' }}>{formatShortDateTime(e.timestamp)}</td>
            <td>{e.shellCode}</td>
            <td>{e.tankSize ?? ''}</td>
            <td>{e.operator}</td>
            <td><ResultCell result={e.result} /></td>
            <td>{e.defects ?? ''}</td>
            <td>
              <AnnotationBadges badges={e.annotations} onAdd={() => onAddAnnot(e.id)} onViewDetail={onViewDetail} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function SpotXrayTable({
  entries,
  onAddAnnot,
  onViewDetail,
}: {
  entries: SpotXrayLogEntry[];
  onAddAnnot: (id: string) => void;
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Date/Time (MT)</th>
          <th>Tank(s)</th>
          <th>Inspected</th>
          <th>Tank Size</th>
          <th>Operator</th>
          <th>Result</th>
          <th>Shots</th>
          <th>Annot</th>
        </tr>
      </thead>
      <tbody>
        {entries.map((e) => (
          <tr key={e.id}>
            <td style={{ whiteSpace: 'nowrap' }}>{formatShortDateTime(e.timestamp)}</td>
            <td>{e.tanks}</td>
            <td>{e.inspected ?? ''}</td>
            <td>{e.tankSize ?? ''}</td>
            <td>{e.operator}</td>
            <td><ResultCell result={e.result} /></td>
            <td style={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {e.shots ?? ''}
            </td>
            <td>
              <AnnotationBadges badges={e.annotations} onAdd={() => onAddAnnot(e.id)} onViewDetail={onViewDetail} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
