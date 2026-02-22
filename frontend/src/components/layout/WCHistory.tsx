import { useNavigate } from 'react-router-dom';
import { FlagRegular, FlagFilled } from '@fluentui/react-icons';
import type { WCHistoryData } from '../../types/domain.ts';
import styles from './WCHistory.module.css';

interface WCHistoryProps {
  data: WCHistoryData;
  logType?: string;
}

export function WCHistory({ data, logType }: WCHistoryProps) {
  const navigate = useNavigate();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.headerCount}>Today&apos;s Count: {data.dayCount}</span>
      </div>

      <div className={styles.tableHeader}>
        <span className={styles.colAnnot}>Annot</span>
        <span className={styles.colDateTime}>Date/Time</span>
        <span className={styles.colSerial}>Shell Code / Serial No.</span>
        <span className={styles.colSize}>Size</span>
      </div>

      <div className={styles.tableBody}>
        {data.recentRecords.length === 0 ? (
          <div className={styles.noRecords}>No records today</div>
        ) : (
          data.recentRecords.map((record) => {
            const dt = new Date(record.timestamp);
            const dateStr = `${dt.getMonth() + 1}/${dt.getDate()}`;
            const timeStr = dt.toLocaleTimeString('en-US', {
              hour: 'numeric',
              minute: '2-digit',
              second: '2-digit',
              hour12: true,
            });
            return (
              <div key={record.id} className={styles.row}>
                <span className={styles.colAnnot}>
                  {record.hasAnnotation
                    ? <FlagFilled fontSize={20} className={styles.flagActive} style={{ color: record.annotationColor ?? '#212529' }} />
                    : <FlagRegular fontSize={20} className={styles.flagInactive} />}
                </span>
                <span className={styles.colDateTime}>{dateStr} {timeStr}</span>
                <span className={styles.colSerial}>{record.serialOrIdentifier}</span>
                <span className={styles.colSize}>{record.tankSize ?? ''}</span>
              </div>
            );
          })
        )}
      </div>

      {logType && (
        <button
          className={styles.viewFullLogBtn}
          onClick={() => navigate(`/menu/production-logs?logType=${logType}`)}
          type="button"
        >
          View Full Log
        </button>
      )}
    </div>
  );
}
