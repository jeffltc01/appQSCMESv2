import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Tooltip } from '@fluentui/react-components';
import {
  WrenchRegular,
  TabletRegular,
  CalendarRegular,
  SettingsRegular,
  SignOutRegular,
  BugRegular,
  ClipboardTaskListLtrRegular,
} from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { MaintenanceRequestDialog } from '../../features/maintenance/MaintenanceRequestDialog.tsx';
import { IssueRequestDialog } from '../../features/issueRequest/IssueRequestDialog.tsx';
import styles from './LeftPanel.module.css';

interface LeftPanelProps {
  externalInput: boolean;
  currentGearLevel?: number | null;
  kioskMode?: boolean;
  showChecklistButton?: boolean;
  onChecklistClick?: () => void;
}

export function LeftPanel({
  externalInput,
  currentGearLevel,
  kioskMode = false,
  showChecklistButton = false,
  onChecklistClick,
}: LeftPanelProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [maintDialogOpen, setMaintDialogOpen] = useState(false);
  const [issueDialogOpen, setIssueDialogOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const disabled = externalInput;
  const navLocked = disabled || kioskMode;

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
          onClick={() => setMaintDialogOpen(true)}
        />
      </Tooltip>

      <Tooltip content="Tablet Setup" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<TabletRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={navLocked}
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

      {showChecklistButton && (
        <Tooltip content="Checklist" relationship="label" positioning="after">
          <Button
            appearance="subtle"
            icon={<ClipboardTaskListLtrRegular fontSize={60} />}
            className={styles.iconBtn}
            disabled={disabled}
            aria-label="Checklist"
            onClick={onChecklistClick}
          />
        </Tooltip>
      )}

      <Tooltip content="Report Issue" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<BugRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={disabled}
          aria-label="Report Issue"
          onClick={() => setIssueDialogOpen(true)}
        />
      </Tooltip>

      <Tooltip content="Settings" relationship="label" positioning="after">
        <Button
          appearance="subtle"
          icon={<SettingsRegular fontSize={60} />}
          className={styles.iconBtn}
          disabled={navLocked}
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

      <MaintenanceRequestDialog
        open={maintDialogOpen}
        onClose={() => setMaintDialogOpen(false)}
        employeeNumber={user?.employeeNumber ?? ''}
        displayName={user?.displayName ?? ''}
      />

      <IssueRequestDialog
        open={issueDialogOpen}
        onClose={() => setIssueDialogOpen(false)}
        userId={user?.id ?? ''}
        roleTier={user?.roleTier ?? 99}
      />
    </nav>
  );
}
