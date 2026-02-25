import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FlagRegular, FlagFilled } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { WCHistoryData, WCHistoryEntry } from '../../types/domain.ts';
import { formatShortDateOnly, formatTimeOnly } from '../../utils/dateFormat.ts';
import { AnnotationDialog } from './AnnotationDialog.tsx';
import styles from './WCHistory.module.css';

interface WCHistoryProps {
  data: WCHistoryData;
  logType?: string;
  operatorId?: string;
  kioskMode?: boolean;
  onAnnotationCreated?: () => void;
}

export function WCHistory({ data, logType, operatorId, kioskMode = false, onAnnotationCreated }: WCHistoryProps) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [dialogRecord, setDialogRecord] = useState<WCHistoryEntry | null>(null);

  const handleFlagClick = (record: WCHistoryEntry) => {
    if (!operatorId) return;
    setDialogRecord(record);
  };

  const handleDialogClose = () => setDialogRecord(null);

  const handleAnnotationCreated = () => {
    setDialogRecord(null);
    onAnnotationCreated?.();
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.headerTitle}>Last 5 Transactions</span>
      </div>

      <div className={styles.tableHeader}>
        <span className={styles.colAnnot}>Annot</span>
        <span className={styles.colDateTime}>Date/Time</span>
        <span className={styles.colSerial}>Shell Code / Serial No.</span>
        <span className={styles.colSize}>Size</span>
      </div>

      <div className={styles.tableBody}>
        {data.recentRecords.length === 0 ? (
          <div className={styles.noRecords}>No History Found</div>
        ) : (
          data.recentRecords.map((record) => (
              <div key={record.id} className={styles.row}>
                <span className={styles.colAnnot}>
                  <button
                    type="button"
                    className={styles.flagBtn}
                    onClick={() => handleFlagClick(record)}
                    aria-label={`Add annotation for ${record.serialOrIdentifier}`}
                    disabled={!operatorId}
                  >
                    {record.hasAnnotation
                      ? <FlagFilled fontSize={20} className={styles.flagActive} style={{ color: record.annotationColor ?? '#212529' }} />
                      : <FlagRegular fontSize={20} className={styles.flagInactive} />}
                  </button>
                </span>
                <span className={styles.colDateTime}>
                  <span className={styles.dateLine}>{formatShortDateOnly(record.timestamp, user?.plantTimeZoneId)}</span>
                  <span className={styles.timeLine}>{formatTimeOnly(record.timestamp, user?.plantTimeZoneId)}</span>
                </span>
                <span className={styles.colSerial}>{record.serialOrIdentifier}</span>
                <span className={styles.colSize}>{record.tankSize ?? ''}</span>
              </div>
          ))
        )}
      </div>

      {logType && !kioskMode && (
        <button
          className={styles.viewFullLogBtn}
          onClick={() => navigate(`/menu/production-logs?logType=${logType}`)}
          type="button"
        >
          View Full Log
        </button>
      )}

      {dialogRecord && operatorId && (
        <AnnotationDialog
          open
          onClose={handleDialogClose}
          productionRecordId={dialogRecord.productionRecordId ?? dialogRecord.id}
          serialOrIdentifier={dialogRecord.serialOrIdentifier}
          operatorId={operatorId}
          onCreated={handleAnnotationCreated}
        />
      )}
    </div>
  );
}
