import { useNavigate } from 'react-router-dom';
import { Button, Tooltip } from '@fluentui/react-components';
import {
  WrenchRegular,
  TabletRegular,
  CalendarRegular,
  SettingsRegular,
  SignOutRegular,
} from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import styles from './LeftPanel.module.css';

interface LeftPanelProps {
  externalInput: boolean;
  currentGearLevel?: number | null;
}

export function LeftPanel({ externalInput, currentGearLevel }: LeftPanelProps) {
  const navigate = useNavigate();
  const { logout } = useAuth();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const disabled = externalInput;

  return (
    <nav className={styles.panel} style={{ pointerEvents: disabled ? 'none' : 'auto', opacity: disabled ? 0.5 : 1 }}>
      <div className={styles.gearLabel}>
        Gear {currentGearLevel ?? '--'}
      </div>

      <Tooltip content="Maintenance Request" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<WrenchRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Maintenance Request"
        />
      </Tooltip>

      <Tooltip content="Tablet Setup" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<TabletRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Tablet Setup"
          onClick={() => navigate('/tablet-setup')}
        />
      </Tooltip>

      <Tooltip content="Schedule" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<CalendarRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Schedule"
        />
      </Tooltip>

      <Tooltip content="Settings" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<SettingsRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Settings"
          onClick={() => navigate('/menu')}
        />
      </Tooltip>

      <div className={styles.spacer} />

      <Tooltip content="Logout" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<SignOutRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Logout"
          onClick={handleLogout}
        />
      </Tooltip>
    </nav>
  );
}
