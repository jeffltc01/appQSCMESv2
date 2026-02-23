import { useState, useCallback, useMemo, useEffect } from 'react';
import { Button, Spinner, Checkbox } from '@fluentui/react-components';
import { spotXrayApi } from '../../api/endpoints';
import { useAuth } from '../../auth/AuthContext';
import type { SpotXrayLaneQueues, SpotXrayLane, SpotXrayQueueTank, SpotXrayIncrementSummary } from '../../types/domain';
import styles from './SpotXrayScreen.module.css';

const MAX_INCREMENT_SIZE: Record<number, number> = {
  120: 8, 250: 6, 320: 6, 500: 5, 1000: 4, 1450: 4, 1990: 4,
};

interface Props {
  workCenterId: string;
  productionLineId: string;
  operatorId: string;
  onIncrementsCreated: (ids: SpotXrayIncrementSummary[]) => void;
}

export function SpotXrayCreateView({ workCenterId, productionLineId, operatorId, onIncrementsCreated }: Props) {
  const { user } = useAuth();
  const siteCode = user?.plantCode ?? '';
  const [lanes, setLanes] = useState<SpotXrayLaneQueues | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selections, setSelections] = useState<Record<string, Set<number>>>({});
  const [creating, setCreating] = useState(false);

  const fetchLanes = useCallback(async () => {
    if (!siteCode) return;
    try {
      setLoading(true);
      setError('');
      const data = await spotXrayApi.getLaneQueues(siteCode);
      setLanes(data);
      const initial: Record<string, Set<number>> = {};
      data.lanes.forEach(l => { initial[l.laneName] = new Set(); });
      setSelections(initial);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load lane queues');
    } finally {
      setLoading(false);
    }
  }, [siteCode]);

  useEffect(() => { fetchLanes(); }, [fetchLanes]);

  const toggleTank = useCallback((laneName: string, position: number) => {
    setSelections(prev => {
      const current = new Set(prev[laneName] ?? []);
      if (current.has(position)) {
        current.delete(position);
      } else {
        current.add(position);
      }
      return { ...prev, [laneName]: current };
    });
  }, []);

  const validationErrors = useMemo(() => {
    if (!lanes) return {};
    const errors: Record<string, string> = {};
    for (const lane of lanes.lanes) {
      const sel = selections[lane.laneName];
      if (!sel || sel.size === 0) continue;
      const err = validateLaneSelection(lane, sel);
      if (err) errors[lane.laneName] = err;
    }
    return errors;
  }, [lanes, selections]);

  const anySelected = Object.values(selections).some(s => s.size > 0);
  const hasErrors = Object.keys(validationErrors).length > 0;

  const handleCreate = async () => {
    if (!anySelected || hasErrors) return;
    try {
      setCreating(true);
      setError('');
      const laneSelections = Object.entries(selections)
        .filter(([, s]) => s.size > 0)
        .map(([laneName, s]) => ({
          laneName,
          selectedPositions: Array.from(s).sort((a, b) => a - b),
        }));
      const result = await spotXrayApi.createIncrements({
        workCenterId,
        productionLineId,
        operatorId,
        siteCode,
        laneSelections,
      });
      onIncrementsCreated(result.increments);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to create increments');
    } finally {
      setCreating(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container} style={{ alignItems: 'center', justifyContent: 'center' }}>
        <Spinner size="large" label="Loading lane queues..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.createHeader}>
        <h2>Create Increment</h2>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {error && <span style={{ color: '#dc3545', fontSize: 13 }}>{error}</span>}
          <Button
            appearance="primary"
            size="large"
            disabled={!anySelected || hasErrors || creating}
            onClick={handleCreate}
          >
            {creating ? 'Creating...' : 'Create Increment'}
          </Button>
        </div>
      </div>
      <div className={styles.lanesContainer}>
        {lanes?.lanes.map(lane => (
          <LaneColumn
            key={lane.laneName}
            lane={lane}
            selected={selections[lane.laneName] ?? new Set()}
            error={validationErrors[lane.laneName]}
            onToggle={(pos) => toggleTank(lane.laneName, pos)}
          />
        ))}
        {(!lanes || lanes.lanes.length === 0) && (
          <div style={{ padding: 20, color: '#868686' }}>No tanks available in any lane.</div>
        )}
      </div>
    </div>
  );
}

function LaneColumn({ lane, selected, error, onToggle }: {
  lane: SpotXrayLane;
  selected: Set<number>;
  error?: string;
  onToggle: (pos: number) => void;
}) {
  const selectedCount = selected.size;
  const firstSelectedTank = lane.tanks.find(t => selected.has(t.position));
  const tankSize = firstSelectedTank?.tankSize;
  const maxSize = tankSize ? MAX_INCREMENT_SIZE[tankSize] ?? 8 : null;

  return (
    <div className={styles.laneColumn}>
      <div className={styles.laneHeader}>
        <span>{lane.laneName}</span>
        {lane.draftCount > 0 && (
          <span className={styles.draftBadge}>{lane.draftCount} draft</span>
        )}
      </div>
      <div className={styles.laneBody}>
        {lane.tanks.map(tank => (
          <TankRow
            key={tank.position}
            tank={tank}
            isSelected={selected.has(tank.position)}
            onToggle={() => onToggle(tank.position)}
          />
        ))}
        {lane.tanks.length === 0 && (
          <div style={{ padding: 12, color: '#868686', fontSize: 13, textAlign: 'center' }}>
            No tanks available
          </div>
        )}
      </div>
      <div className={`${styles.selectionInfo} ${error ? styles.selectionError : ''}`}>
        {error
          ? error
          : selectedCount > 0
            ? `${selectedCount} selected${maxSize ? ` (max ${maxSize})` : ''}`
            : 'Select tanks for this lane'}
      </div>
    </div>
  );
}

function TankRow({ tank, isSelected, onToggle }: {
  tank: SpotXrayQueueTank;
  isSelected: boolean;
  onToggle: () => void;
}) {
  const showBreak = tank.sizeChanged || tank.welderChanged;
  return (
    <div
      className={`${styles.tankRow} ${isSelected ? styles.tankRowSelected : ''} ${showBreak ? styles.tankRowBreak : ''}`}
      onClick={onToggle}
    >
      <Checkbox
        checked={isSelected}
        onChange={(e) => { e.stopPropagation(); onToggle(); }}
      />
      <span className={styles.tankPosition}>{tank.position}</span>
      <div className={styles.tankInfo}>
        <span className={styles.tankAlpha}>{tank.alphaCode}</span>
        <span className={styles.tankMeta}>
          {tank.welderNames.join(', ')}
        </span>
      </div>
      <span className={styles.sizeBadge}>{tank.tankSize}</span>
    </div>
  );
}

function validateLaneSelection(lane: SpotXrayLane, selected: Set<number>): string | undefined {
  if (selected.size === 0) return undefined;

  const positions = Array.from(selected).sort((a, b) => a - b);

  // Sequential check
  for (let i = 1; i < positions.length; i++) {
    if (positions[i] !== positions[i - 1] + 1) {
      return 'Selections must be sequential';
    }
  }

  const tanks = lane.tanks.filter(t => selected.has(t.position));

  // Same tank size
  const firstSize = tanks[0].tankSize;
  if (tanks.some(t => t.tankSize !== firstSize)) {
    return 'All tanks must be the same size';
  }

  // Same welders
  const firstWelders = JSON.stringify(tanks[0].welderIds);
  if (tanks.some(t => JSON.stringify(t.welderIds) !== firstWelders)) {
    return 'All tanks must have the same welders';
  }

  // Max size
  const maxSize = MAX_INCREMENT_SIZE[firstSize] ?? 8;
  if (tanks.length > maxSize) {
    return `Max ${maxSize} tanks for ${firstSize} gal`;
  }

  return undefined;
}
