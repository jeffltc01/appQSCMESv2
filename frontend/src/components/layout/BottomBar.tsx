import { Switch } from '@fluentui/react-components';
import { useClock } from '../../hooks/useClock.ts';
import { useHealthCheck } from '../../hooks/useHealthCheck.ts';
import type { HealthStatus } from '../../hooks/useHealthCheck.ts';
import styles from './BottomBar.module.css';

interface BottomBarProps {
  plantCode: string;
  plantTimeZoneId?: string;
  externalInput: boolean;
  onToggleExternalInput: () => void;
  showToggle?: boolean;
  scannerReady?: boolean;
  currentCount?: number | null;
  capacityCount?: number | null;
  capacityLabel?: string;
}

const statusConfig: Record<HealthStatus, { className: string; label: string }> = {
  online:   { className: styles.dotOnline,   label: 'Online' },
  offline:  { className: styles.dotOffline,  label: 'Offline' },
  checking: { className: styles.dotChecking, label: 'Checking…' },
};

export function BottomBar({
  plantCode,
  plantTimeZoneId,
  externalInput,
  onToggleExternalInput,
  showToggle = true,
  scannerReady,
  currentCount,
  capacityCount,
  capacityLabel = 'Capacity',
}: BottomBarProps) {
  const clock = useClock(plantTimeZoneId);
  const health = useHealthCheck();
  const { className: dotClass, label: statusLabel } = statusConfig[health];
  const hasCapacityData = Number.isFinite(currentCount) && Number.isFinite(capacityCount) && (capacityCount ?? 0) > 0;
  const safeCurrent = hasCapacityData ? Math.max(0, currentCount as number) : 0;
  const safeCapacity = hasCapacityData ? capacityCount as number : 0;
  const ratio = hasCapacityData ? safeCurrent / safeCapacity : 0;
  const percent = hasCapacityData ? Math.round(ratio * 100) : 0;
  const percentForBar = Math.max(0, Math.min(percent, 100));
  const remaining = hasCapacityData ? Math.max(safeCapacity - safeCurrent, 0) : 0;
  const capacityStateClass = percent >= 100
    ? styles.capacityFull
    : percent >= 80
      ? styles.capacityWarning
      : styles.capacityNormal;
  const capacityStateLabel = percent >= 100
    ? 'Capacity Reached'
    : percent >= 80
      ? 'Near Capacity'
      : 'In Capacity';

  return (
    <footer className={styles.bottomBar}>
      <div className={styles.plantTime}>
        {plantCode} - {clock}
      </div>

      {showToggle && (
        <div className={styles.toggleArea}>
          <Switch
            checked={externalInput}
            onChange={onToggleExternalInput}
            label="External Input"
            className={styles.toggle}
            indicator={{
              style: {
                background: externalInput ? '#0078d4' : '#6c757d',
                borderColor: externalInput ? '#0078d4' : '#adb5bd',
                color: '#ffffff',
              },
            }}
          />
          {externalInput && (
            <span
              className={`${styles.scannerDot} ${scannerReady ? styles.scannerReady : styles.scannerLost}`}
              title={scannerReady ? 'Scanner ready' : 'Scanner inactive'}
            />
          )}
        </div>
      )}

      <div className={styles.rightSide}>
        <div className={styles.status}>
          <span className={`${styles.statusDot} ${dotClass}`} />
          {statusLabel}
        </div>

        {hasCapacityData && (
          <section className={`${styles.capacityBadge} ${capacityStateClass}`} aria-label="Operator capacity indicator">
            <div className={styles.capacityHeader}>
              <span>{capacityLabel}</span>
              <span>{safeCurrent} / {safeCapacity}</span>
            </div>
            <div className={styles.capacityMeta}>
              <span>{percent}%</span>
              <span>{capacityStateLabel}</span>
            </div>
            <div className={styles.capacityTrack} aria-hidden="true">
              <span className={styles.capacityFill} style={{ width: `${percentForBar}%` }} />
            </div>
            <div className={styles.capacityFooter}>{remaining} remaining</div>
          </section>
        )}
      </div>
    </footer>
  );
}
