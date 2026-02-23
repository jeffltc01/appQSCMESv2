import { useState, useCallback, useEffect } from 'react';
import { Button, Spinner, Dropdown, Option } from '@fluentui/react-components';
import { spotXrayApi } from '../../api/endpoints';
import type { SpotXrayIncrementSummary, SpotXrayIncrementDetail, SpotXraySeam, SpotXrayIncrementTank } from '../../types/domain';
import styles from './SpotXrayScreen.module.css';

interface Props {
  incrementSummaries: SpotXrayIncrementSummary[];
  operatorId: string;
  plantId: string;
  onBackToCreate: () => void;
}

export function SpotXrayResultsView({ incrementSummaries, operatorId, plantId, onBackToCreate }: Props) {
  const [activeIdx, setActiveIdx] = useState(0);
  const [detail, setDetail] = useState<SpotXrayIncrementDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [inspectTankId, setInspectTankId] = useState<string>('');
  const [seamData, setSeamData] = useState<SeamFormData[]>([]);

  const activeIncrement = incrementSummaries[activeIdx];

  const fetchDetail = useCallback(async (id: string) => {
    try {
      setLoading(true);
      setError('');
      const data = await spotXrayApi.getIncrement(id);
      setDetail(data);
      setInspectTankId(data.inspectTankId ?? '');
      setSeamData(data.seams.map(s => seamToForm(s)));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load increment');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeIncrement) {
      fetchDetail(activeIncrement.id);
    }
  }, [activeIncrement, fetchDetail]);

  const handleGetShotNumber = useCallback(async (seamIdx: number, field: 'shotNo' | 'trace1ShotNo' | 'trace2ShotNo' | 'finalShotNo') => {
    try {
      const resp = await spotXrayApi.getNextShotNumber(plantId);
      setSeamData(prev => {
        const next = [...prev];
        next[seamIdx] = { ...next[seamIdx], [field]: String(resp.shotNumber) };
        return next;
      });
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to get shot number');
    }
  }, [plantId]);

  const updateSeam = useCallback((seamIdx: number, field: string, value: string) => {
    setSeamData(prev => {
      const next = [...prev];
      next[seamIdx] = { ...next[seamIdx], [field]: value };
      return next;
    });
  }, []);

  const handleSave = async (isDraft: boolean) => {
    if (!detail) return;
    try {
      setSaving(true);
      setError('');
      const result = await spotXrayApi.saveResults(detail.id, {
        inspectTankId: inspectTankId || undefined,
        isDraft,
        operatorId,
        seams: seamData.map((s, i) => ({
          seamNumber: i + 1,
          shotNo: s.shotNo || undefined,
          result: s.result || undefined,
          trace1ShotNo: s.trace1ShotNo || undefined,
          trace1TankId: s.trace1TankId || undefined,
          trace1Result: s.trace1Result || undefined,
          trace2ShotNo: s.trace2ShotNo || undefined,
          trace2TankId: s.trace2TankId || undefined,
          trace2Result: s.trace2Result || undefined,
          finalShotNo: s.finalShotNo || undefined,
          finalResult: s.finalResult || undefined,
        })),
      });
      setDetail(result);
      setInspectTankId(result.inspectTankId ?? '');
      setSeamData(result.seams.map(s => seamToForm(s)));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to save results');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container} style={{ alignItems: 'center', justifyContent: 'center' }}>
        <Spinner size="large" label="Loading increment..." />
      </div>
    );
  }

  if (!detail) {
    return (
      <div className={styles.container} style={{ alignItems: 'center', justifyContent: 'center', padding: 20 }}>
        <span style={{ color: '#dc3545' }}>{error || 'Increment not found'}</span>
        <Button appearance="secondary" onClick={onBackToCreate} style={{ marginTop: 12 }}>Back</Button>
      </div>
    );
  }

  const isFinalized = !detail.isDraft && detail.overallStatus !== 'Pending';

  return (
    <div className={styles.resultsContainer}>
      <div className={styles.resultsHeader}>
        <h2>Test Results</h2>
        <Button appearance="subtle" onClick={onBackToCreate}>Back to Create</Button>
      </div>

      {incrementSummaries.length > 1 && (
        <div className={styles.tabsRow}>
          {incrementSummaries.map((inc, i) => (
            <button
              key={inc.id}
              className={`${styles.tabButton} ${i === activeIdx ? styles.tabButtonActive : ''} ${
                inc.overallStatus === 'Reject' ? styles.tabButtonReject :
                inc.overallStatus === 'Accept' ? styles.tabButtonAccept : ''
              }`}
              onClick={() => setActiveIdx(i)}
            >
              {inc.laneNo} — {inc.incrementNo}
            </button>
          ))}
        </div>
      )}

      <div className={styles.resultsBody}>
        {error && <div style={{ color: '#dc3545', marginBottom: 8, fontSize: 13 }}>{error}</div>}

        <div className={styles.incrementMeta}>
          <span>Increment: <strong>{detail.incrementNo}</strong></span>
          <span>Lane: <strong>{detail.laneNo}</strong></span>
          <span>Tank Size: <strong>{detail.tankSize} gal</strong></span>
          <span>Tanks: <strong>{detail.tanks.length}</strong></span>
          <StatusBadge status={detail.overallStatus} />
        </div>

        <div className={styles.inspectRow}>
          <span style={{ fontWeight: 600, fontSize: 13 }}>Inspection Tank:</span>
          <Dropdown
            style={{ minWidth: 280 }}
            value={(() => { const t = detail.tanks.find(t => t.serialNumberId === inspectTankId); return t ? tankLabel(t) : ''; })()}
            selectedOptions={inspectTankId ? [inspectTankId] : []}
            onOptionSelect={(_, data) => setInspectTankId(data.optionValue ?? '')}
            disabled={isFinalized}
          >
            {detail.tanks.map(t => (
              <Option key={t.serialNumberId} value={t.serialNumberId}>
                {tankLabel(t)}
              </Option>
            ))}
          </Dropdown>
        </div>

        <div className={styles.seamsGrid} style={{ gridTemplateColumns: `repeat(${detail.seamCount <= 2 ? 2 : detail.seamCount}, 1fr)` }}>
          {seamData.map((seam, i) => (
            <SeamCard
              key={i}
              seamNumber={i + 1}
              seam={seam}
              serverSeam={detail.seams[i]}
              tanks={detail.tanks}
              isFinalized={isFinalized}
              onGetShotNumber={(field) => handleGetShotNumber(i, field)}
              onChange={(field, value) => updateSeam(i, field, value)}
            />
          ))}
        </div>
      </div>

      <div className={styles.actionsRow}>
        <Button
          appearance="secondary"
          size="large"
          disabled={saving || isFinalized}
          onClick={() => handleSave(true)}
        >
          {saving ? 'Saving...' : 'Save Draft'}
        </Button>
        <Button
          appearance="primary"
          size="large"
          disabled={saving || isFinalized || !inspectTankId}
          onClick={() => handleSave(false)}
        >
          {saving ? 'Saving...' : 'Submit'}
        </Button>
      </div>
    </div>
  );
}

interface SeamFormData {
  shotNo: string;
  result: string;
  trace1ShotNo: string;
  trace1TankId: string;
  trace1Result: string;
  trace2ShotNo: string;
  trace2TankId: string;
  trace2Result: string;
  finalShotNo: string;
  finalResult: string;
}

function tankLabel(t: SpotXrayIncrementTank): string {
  if (t.shellSerials.length > 0) {
    return `${t.alphaCode} (${t.shellSerials.join(', ')})`;
  }
  return t.alphaCode;
}

function seamToForm(s: SpotXraySeam): SeamFormData {
  return {
    shotNo: s.shotNo ?? '',
    result: s.result ?? '',
    trace1ShotNo: s.trace1ShotNo ?? '',
    trace1TankId: s.trace1TankId ?? '',
    trace1Result: s.trace1Result ?? '',
    trace2ShotNo: s.trace2ShotNo ?? '',
    trace2TankId: s.trace2TankId ?? '',
    trace2Result: s.trace2Result ?? '',
    finalShotNo: s.finalShotNo ?? '',
    finalResult: s.finalResult ?? '',
  };
}

function SeamCard({ seamNumber, seam, serverSeam, tanks, isFinalized, onGetShotNumber, onChange }: {
  seamNumber: number;
  seam: SeamFormData;
  serverSeam: SpotXraySeam;
  tanks: SpotXrayIncrementTank[];
  isFinalized: boolean;
  onGetShotNumber: (field: 'shotNo' | 'trace1ShotNo' | 'trace2ShotNo' | 'finalShotNo') => void;
  onChange: (field: string, value: string) => void;
}) {
  const showTrace1 = seam.result === 'Reject';
  const showTrace2 = showTrace1 && seam.trace1Result === 'Accept';
  const showFinal = showTrace2 && seam.trace2Result === 'Accept';

  return (
    <div className={styles.seamCard}>
      <div className={styles.seamCardHeader}>
        <span>Seam {seamNumber}</span>
        {serverSeam.welderName && <span style={{ fontWeight: 400, fontSize: 12 }}>{serverSeam.welderName}</span>}
      </div>
      <div className={styles.seamCardBody}>
        {/* Initial Shot */}
        <ShotRow
          label="Initial"
          shotNo={seam.shotNo}
          result={seam.result}
          disabled={isFinalized}
          onGetShot={() => onGetShotNumber('shotNo')}
          onResultChange={(v) => onChange('result', v)}
        />

        {/* Trace 1 */}
        {showTrace1 && (
          <>
            <ShotRow
              label="Trace 1"
              shotNo={seam.trace1ShotNo}
              result={seam.trace1Result}
              disabled={isFinalized}
              onGetShot={() => onGetShotNumber('trace1ShotNo')}
              onResultChange={(v) => onChange('trace1Result', v)}
            />
            <div className={styles.shotRow}>
              <span className={styles.shotLabel}>Trace 1 Tank</span>
              <Dropdown
                style={{ minWidth: 280 }}
                value={(() => { const t = tanks.find(t => t.serialNumberId === seam.trace1TankId); return t ? tankLabel(t) : ''; })()}
                selectedOptions={seam.trace1TankId ? [seam.trace1TankId] : []}
                onOptionSelect={(_, data) => onChange('trace1TankId', data.optionValue ?? '')}
                disabled={isFinalized}
              >
                {tanks.map(t => (
                  <Option key={t.serialNumberId} value={t.serialNumberId}>{tankLabel(t)}</Option>
                ))}
              </Dropdown>
            </div>
          </>
        )}

        {/* Trace 2 */}
        {showTrace2 && (
          <>
            <ShotRow
              label="Trace 2"
              shotNo={seam.trace2ShotNo}
              result={seam.trace2Result}
              disabled={isFinalized}
              onGetShot={() => onGetShotNumber('trace2ShotNo')}
              onResultChange={(v) => onChange('trace2Result', v)}
            />
            <div className={styles.shotRow}>
              <span className={styles.shotLabel}>Trace 2 Tank</span>
              <Dropdown
                style={{ minWidth: 280 }}
                value={(() => { const t = tanks.find(t => t.serialNumberId === seam.trace2TankId); return t ? tankLabel(t) : ''; })()}
                selectedOptions={seam.trace2TankId ? [seam.trace2TankId] : []}
                onOptionSelect={(_, data) => onChange('trace2TankId', data.optionValue ?? '')}
                disabled={isFinalized}
              >
                {tanks.map(t => (
                  <Option key={t.serialNumberId} value={t.serialNumberId}>{tankLabel(t)}</Option>
                ))}
              </Dropdown>
            </div>
          </>
        )}

        {/* Final */}
        {showFinal && (
          <ShotRow
            label="Final"
            shotNo={seam.finalShotNo}
            result={seam.finalResult}
            disabled={isFinalized}
            onGetShot={() => onGetShotNumber('finalShotNo')}
            onResultChange={(v) => onChange('finalResult', v)}
          />
        )}
      </div>
    </div>
  );
}

function ShotRow({ label, shotNo, result, disabled, onGetShot, onResultChange }: {
  label: string;
  shotNo: string;
  result: string;
  disabled: boolean;
  onGetShot: () => void;
  onResultChange: (value: string) => void;
}) {
  return (
    <div className={styles.shotRow}>
      <span className={styles.shotLabel}>{label}</span>
      {shotNo ? (
        <span style={{ fontWeight: 600, fontSize: 13, minWidth: 40 }}>#{shotNo}</span>
      ) : (
        <Button size="small" appearance="outline" disabled={disabled} onClick={onGetShot}>
          Get Shot #
        </Button>
      )}
      <Dropdown
        style={{ minWidth: 120 }}
        value={result || ''}
        selectedOptions={result ? [result] : []}
        onOptionSelect={(_, data) => onResultChange(data.optionValue ?? '')}
        disabled={disabled || !shotNo}
        placeholder="Result"
      >
        <Option value="Accept">Accept</Option>
        <Option value="Reject">Reject</Option>
      </Dropdown>
      {result && (
        <span className={result === 'Accept' ? styles.resultAccept : styles.resultReject}>
          {result}
        </span>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  let cls = styles.statusPending;
  if (status === 'Accept') cls = styles.statusAccept;
  else if (status === 'Reject') cls = styles.statusReject;
  else if (status === 'Accept-Scrap') cls = styles.statusScrap;

  return <span className={`${styles.statusBadge} ${cls}`}>{status}</span>;
}
