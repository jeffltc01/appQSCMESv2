import type { QueueTransaction } from '../../types/domain.ts';
import { formatShortDateOnly, formatTimeOnly } from '../../utils/dateFormat.ts';
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

      <div className={styles.queueTableHeader}>
        <span className={styles.colDateTime}>Date/Time</span>
        <span className={styles.colSerial}>Item</span>
      </div>

      <div className={styles.queueListPanel}>
        <div className={styles.tableBody}>
          {transactions.length === 0 ? (
            <div className={styles.noRecords}>No recent activity</div>
          ) : (
            transactions.map((tx) => (
                <div key={tx.id} className={styles.queueRow}>
                  <span className={styles.colDateTime}>
                    <span className={styles.dateLine}>{formatShortDateOnly(tx.timestamp)}</span>
                    <span className={styles.timeLine}>{formatTimeOnly(tx.timestamp)}</span>
                  </span>
                  <span className={`${styles.colSerial} ${styles.queueItemSummary}`}>{tx.itemSummary}</span>
                </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
