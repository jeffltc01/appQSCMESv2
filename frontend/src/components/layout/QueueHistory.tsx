import type { QueueTransaction } from '../../types/domain.ts';
import styles from './WCHistory.module.css';

interface QueueHistoryProps {
  transactions: QueueTransaction[];
}

export function QueueHistory({ transactions }: QueueHistoryProps) {
  const added = transactions.filter((tx) => tx.action === 'added');
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.headerCount}>Recent Queue Activity</span>
      </div>

      <div className={styles.queueTableHeader}>
        <span className={styles.colDateTime}>Date/Time</span>
        <span className={styles.colSerial}>Item</span>
      </div>

      <div className={styles.tableBody}>
        {added.length === 0 ? (
          <div className={styles.noRecords}>No recent activity</div>
        ) : (
          added.map((tx) => {
            const dt = new Date(tx.timestamp);
            const dateStr = dt.toLocaleDateString('en-US', {
              month: 'numeric',
              day: 'numeric',
            });
            const timeStr = dt.toLocaleTimeString('en-US', {
              hour: 'numeric',
              minute: '2-digit',
              hour12: true,
            });
            const dtStr = `${dateStr} ${timeStr}`;
            return (
              <div key={tx.id} className={styles.queueRow}>
                <span className={styles.colDateTime}>{dtStr}</span>
                <span className={styles.colSerial}>{tx.itemSummary}</span>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
