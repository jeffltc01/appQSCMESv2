import type { QueueTransaction } from '../../types/domain.ts';
import { formatShortDateTime } from '../../utils/dateFormat.ts';
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
          added.map((tx) => (
              <div key={tx.id} className={styles.queueRow}>
                <span className={styles.colDateTime}>{formatShortDateTime(tx.timestamp)}</span>
                <span className={styles.colSerial}>{tx.itemSummary}</span>
              </div>
          ))
        )}
      </div>
    </div>
  );
}
