import { NoteRegular } from '@fluentui/react-icons';
import type { WCHistoryData } from '../../types/domain.ts';
import styles from './WCHistory.module.css';

interface WCHistoryProps {
  data: WCHistoryData;
}

export function WCHistory({ data }: WCHistoryProps) {
  return (
    <div className={styles.container}>
      <div className={styles.countSection}>
        <span className={styles.countLabel}>Today</span>
        <span className={styles.countValue}>{data.dayCount}</span>
      </div>

      <div className={styles.divider} />

      <div className={styles.recentSection}>
        <span className={styles.recentLabel}>Last 5</span>
        {data.recentRecords.length === 0 ? (
          <span className={styles.noRecords}>No records today</span>
        ) : (
          data.recentRecords.map((record) => (
            <div key={record.id} className={styles.recordRow}>
              <div className={styles.recordMain}>
                <span className={styles.recordSerial}>{record.serialOrIdentifier}</span>
                <span className={styles.recordSize}>{record.tankSize ?? ''}</span>
              </div>
              <div className={styles.recordMeta}>
                <span className={styles.recordTime}>
                  {new Date(record.timestamp).toLocaleTimeString('en-US', {
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true,
                  })}
                </span>
                <button
                  className={`${styles.annotationFlag} ${record.hasAnnotation ? styles.hasAnnotation : ''}`}
                  aria-label="Annotation"
                >
                  <NoteRegular fontSize={14} />
                </button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
