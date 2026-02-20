import { Switch } from '@fluentui/react-components';
import { useClock } from '../../hooks/useClock.ts';
import styles from './BottomBar.module.css';

interface BottomBarProps {
  plantCode: string;
  externalInput: boolean;
  onToggleExternalInput: () => void;
}

export function BottomBar({ plantCode, externalInput, onToggleExternalInput }: BottomBarProps) {
  const clock = useClock();

  return (
    <footer className={styles.bottomBar}>
      <div className={styles.plantTime}>
        {plantCode} - {clock}
      </div>

      <div className={styles.toggleArea}>
        <Switch
          checked={externalInput}
          onChange={onToggleExternalInput}
          label="External Input"
          className={styles.toggle}
        />
      </div>

      <div className={styles.status}>
        <span className={styles.onlineDot} />
        Online
      </div>
    </footer>
  );
}
