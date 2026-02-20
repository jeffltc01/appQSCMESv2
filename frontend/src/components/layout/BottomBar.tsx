import { Switch } from '@fluentui/react-components';
import { useClock } from '../../hooks/useClock.ts';
import styles from './BottomBar.module.css';

interface BottomBarProps {
  plantCode: string;
  externalInput: boolean;
  onToggleExternalInput: () => void;
  showToggle?: boolean;
}

export function BottomBar({ plantCode, externalInput, onToggleExternalInput, showToggle = true }: BottomBarProps) {
  const clock = useClock();

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
        <span className={styles.onlineDot} />
        Online
      </div>
    </footer>
  );
}
