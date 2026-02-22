import { useState, useEffect } from 'react';
import { CheckmarkCircleRegular, DismissCircleRegular, WarningRegular } from '@fluentui/react-icons';
import styles from './ScanOverlay.module.css';

export interface ScanResult {
  type: 'success' | 'error' | 'warning';
  message?: string;
}

interface ScanOverlayProps {
  result: ScanResult;
  onDismiss: () => void;
  autoCloseMs?: number;
}

const bgColors: Record<ScanResult['type'], string> = {
  success: 'rgba(40, 167, 69, 0.93)',
  warning: 'rgba(255, 165, 0, 0.93)',
  error: 'rgba(220, 53, 69, 0.93)',
};

export function ScanOverlay({ result, onDismiss, autoCloseMs }: ScanOverlayProps) {
  const isSuccess = result.type === 'success';
  const showDismiss = result.type !== 'success';

  const [remaining, setRemaining] = useState(() =>
    autoCloseMs ? Math.ceil(autoCloseMs / 1000) : 0,
  );

  useEffect(() => {
    if (!autoCloseMs || isSuccess) return;
    setRemaining(Math.ceil(autoCloseMs / 1000));
    const id = setInterval(() => {
      setRemaining((prev) => (prev > 1 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(id);
  }, [autoCloseMs, isSuccess]);

  const icon = result.type === 'success'
    ? <CheckmarkCircleRegular className={styles.icon} />
    : result.type === 'warning'
      ? <WarningRegular className={styles.icon} />
      : <DismissCircleRegular className={styles.icon} />;

  return (
    <div
      className={styles.overlay}
      style={{ backgroundColor: bgColors[result.type] }}
      onClick={onDismiss}
      role="alert"
      data-testid="scan-overlay"
    >
      <div className={styles.content}>
        {icon}
        {result.message && <span className={styles.message}>{result.message}</span>}
        {showDismiss && (
          <span className={styles.dismissHint}>
            Tap to dismiss{remaining > 0 ? ` (${remaining}s)` : ''}
          </span>
        )}
      </div>
    </div>
  );
}
