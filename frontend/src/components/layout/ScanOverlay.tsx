import { useState, useEffect } from 'react';
import { CheckmarkCircleRegular, DismissCircleRegular } from '@fluentui/react-icons';
import styles from './ScanOverlay.module.css';

export interface ScanResult {
  type: 'success' | 'error';
  message?: string;
}

interface ScanOverlayProps {
  result: ScanResult;
  onDismiss: () => void;
  autoCloseMs?: number;
}

export function ScanOverlay({ result, onDismiss, autoCloseMs }: ScanOverlayProps) {
  const isSuccess = result.type === 'success';

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

  return (
    <div
      className={styles.overlay}
      style={{ backgroundColor: isSuccess ? 'rgba(40, 167, 69, 0.93)' : 'rgba(220, 53, 69, 0.93)' }}
      onClick={onDismiss}
      role="alert"
      data-testid="scan-overlay"
    >
      <div className={styles.content}>
        {isSuccess ? (
          <CheckmarkCircleRegular className={styles.icon} />
        ) : (
          <DismissCircleRegular className={styles.icon} />
        )}
        {result.message && <span className={styles.message}>{result.message}</span>}
        {!isSuccess && (
          <span className={styles.dismissHint}>
            Tap to dismiss{remaining > 0 ? ` (${remaining}s)` : ''}
          </span>
        )}
      </div>
    </div>
  );
}
