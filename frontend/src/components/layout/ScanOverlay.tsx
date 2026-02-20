import { CheckmarkCircleRegular, DismissCircleRegular } from '@fluentui/react-icons';
import styles from './ScanOverlay.module.css';

export interface ScanResult {
  type: 'success' | 'error';
  message?: string;
}

interface ScanOverlayProps {
  result: ScanResult;
  onDismiss: () => void;
}

export function ScanOverlay({ result, onDismiss }: ScanOverlayProps) {
  const isSuccess = result.type === 'success';

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
      </div>
    </div>
  );
}
