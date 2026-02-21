import type { QueueTransaction } from '../../types/domain.ts';
import styles from './WCHistory.module.css';

interface QueueHistoryProps {
  transactions: QueueTransaction[];
}

export function QueueHistory({ transactions }: QueueHistoryProps) {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.headerCount}>Recent Queue Activity</span>
      </div>

      <div className={styles.tableHeader}>
        <span className={styles.colDateTime}>Time</span>
        <span className={styles.colSerial}>Item</span>
        <span className={styles.colSize}>Action</span>
      </div>

      <div className={styles.tableBody}>
        {transactions.length === 0 ? (
          <div className={styles.noRecords}>No recent activity</div>
        ) : (
          transactions.map((tx) => {
            const dt = new Date(tx.timestamp);
            const timeStr = dt.toLocaleTimeString('en-US', {
              hour: 'numeric',
              minute: '2-digit',
              hour12: true,
            });
            return (
              <div key={tx.id} className={styles.row}>
                <span className={styles.colDateTime}>{timeStr}</span>
                <span className={styles.colSerial}>{tx.itemSummary}</span>
                <span className={styles.colSize}>{tx.action}</span>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
