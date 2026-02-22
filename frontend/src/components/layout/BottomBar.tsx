import { Switch } from '@fluentui/react-components';
import { useClock } from '../../hooks/useClock.ts';
import { useHealthCheck } from '../../hooks/useHealthCheck.ts';
import type { HealthStatus } from '../../hooks/useHealthCheck.ts';
import styles from './BottomBar.module.css';

interface BottomBarProps {
  plantCode: string;
  externalInput: boolean;
  onToggleExternalInput: () => void;
  showToggle?: boolean;
}

const statusConfig: Record<HealthStatus, { className: string; label: string }> = {
  online:   { className: styles.dotOnline,   label: 'Online' },
  offline:  { className: styles.dotOffline,  label: 'Offline' },
  checking: { className: styles.dotChecking, label: 'Checking…' },
};

export function BottomBar({ plantCode, externalInput, onToggleExternalInput, showToggle = true }: BottomBarProps) {
  const clock = useClock();
  const health = useHealthCheck();
  const { className: dotClass, label: statusLabel } = statusConfig[health];

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
        </div>
      )}

      <div className={styles.status}>
        <span className={`${styles.statusDot} ${dotClass}`} />
        {statusLabel}
      </div>
    </footer>
  );
}
