import { useEffect, useMemo, useState } from 'react';
import { Button, Spinner } from '@fluentui/react-components';
import { demoShellApi } from '../../api/endpoints.ts';
import type { DemoShellCurrent } from '../../types/domain.ts';
import { getDemoActionsForDataEntryType } from '../../features/scanning/demoScanMenuConfig.ts';
import { Code128Barcode } from './Code128Barcode.tsx';
import styles from './DemoScanOverlay.module.css';

interface DemoScanOverlayProps {
  open: boolean;
  workCenterId: string;
  dataEntryType: string;
  demoModeEnabled: boolean;
  onClose: () => void;
  onMessage: (result: { type: 'success' | 'error'; message: string }) => void;
  onBarcodeClick?: (barcodeRaw: string) => void;
  onInteraction?: () => void;
}

export function DemoScanOverlay({
  open,
  workCenterId,
  dataEntryType,
  demoModeEnabled,
  onClose,
  onMessage,
  onBarcodeClick,
  onInteraction,
}: DemoScanOverlayProps) {
  const [loading, setLoading] = useState(false);
  const [advancing, setAdvancing] = useState(false);
  const [demoShell, setDemoShell] = useState<DemoShellCurrent | null>(null);

  const actions = useMemo(() => getDemoActionsForDataEntryType(dataEntryType), [dataEntryType]);
  const shellBarcodes = useMemo(() => {
    if (!demoModeEnabled || !demoShell?.hasCurrent || !demoShell.barcodeRaw || !demoShell.serialNumber) return [];
    if (dataEntryType === 'Rolls') {
      return [
        { label: 'Shell Label 1', raw: `SC;${demoShell.serialNumber}/L1` },
        { label: 'Shell Label 2', raw: `SC;${demoShell.serialNumber}/L2` },
      ];
    }
    return [{ label: 'Shell', raw: demoShell.barcodeRaw }];
  }, [dataEntryType, demoModeEnabled, demoShell]);

  useEffect(() => {
    if (!open || !workCenterId || !demoModeEnabled) return;
    setLoading(true);
    demoShellApi.getCurrent(workCenterId)
      .then(setDemoShell)
      .catch((error: { message?: string }) => onMessage({
        type: 'error',
        message: error.message ?? 'Failed to load demo shell state.',
      }))
      .finally(() => setLoading(false));
  }, [open, workCenterId, demoModeEnabled, onMessage]);

  if (!open) return null;

  const handleAdvance = async () => {
    if (!workCenterId || !demoModeEnabled) return;
    onInteraction?.();
    setAdvancing(true);
    try {
      const next = await demoShellApi.advance({ workCenterId });
      setDemoShell(next);
      onMessage({ type: 'success', message: 'Demo shell advanced to next stage.' });
    } catch (error: unknown) {
      const msg = (error as { message?: string })?.message ?? 'Failed to advance demo shell.';
      onMessage({ type: 'error', message: msg });
    } finally {
      setAdvancing(false);
    }
  };

  return (
    <div className={styles.backdrop} role="dialog" aria-modal="true" aria-label="Demo scan overlay">
      <div className={styles.panel}>
        <div className={styles.header}>
          <div>
            <h2 className={styles.title}>Demo Scan Menu</h2>
            <p className={styles.meta}>
              Data Entry Type: {dataEntryType || 'Unknown'} {demoShell?.stage ? `· Stage: ${demoShell.stage}` : ''}
            </p>
          </div>
          <div className={styles.actions}>
            {demoModeEnabled && (
              <Button appearance="primary" onClick={handleAdvance} disabled={advancing || loading}>
                {advancing ? 'Advancing...' : 'Advance To Next Shell'}
              </Button>
            )}
            <Button appearance="secondary" onClick={onClose}>
              Close
            </Button>
          </div>
        </div>

        {loading ? (
          <Spinner size="medium" label="Loading demo shell state..." />
        ) : (
          <div className={styles.cards}>
            {actions.map((action) => (
              <div className={styles.card} key={action.id}>
                <h3 className={styles.cardTitle}>{action.label}</h3>
                <Code128Barcode className={styles.barcode} value={action.barcodeRaw} />
                <div className={styles.raw}>{action.barcodeRaw}</div>
                <button
                  type="button"
                  className={styles.invisibleHitArea}
                  aria-label={`Process ${action.label}`}
                  onClick={() => {
                    onInteraction?.();
                    onBarcodeClick?.(action.barcodeRaw);
                  }}
                />
              </div>
            ))}

            {demoModeEnabled && shellBarcodes.map((shellBarcode) => (
              <div className={styles.card} key={shellBarcode.raw}>
                <h3 className={styles.cardTitle}>{shellBarcode.label}</h3>
                <Code128Barcode className={styles.barcode} value={shellBarcode.raw} />
                <div className={styles.raw}>{shellBarcode.raw}</div>
                <div className={styles.queueInfo}>Queue depth at stage: {demoShell?.stageQueueCount ?? 0}</div>
                <button
                  type="button"
                  className={styles.invisibleHitArea}
                  aria-label={`Process ${shellBarcode.label}`}
                  onClick={() => {
                    onInteraction?.();
                    onBarcodeClick?.(shellBarcode.raw);
                  }}
                />
              </div>
            ))}

            {demoModeEnabled && (!demoShell || !demoShell.hasCurrent) && (
              <div className={styles.empty}>No demo shell is currently available at this stage.</div>
            )}

            {!demoModeEnabled && (
              <div className={styles.empty}>Demo shell barcodes are disabled for this user.</div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
