import { useState, useCallback, useEffect, useRef } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Input,
} from '@fluentui/react-components';
import { DismissRegular, PersonAddRegular } from '@fluentui/react-icons';
import type { Welder } from '../../types/domain.ts';
import { workCenterApi } from '../../api/endpoints.ts';
import { HelpButton } from '../../help/components/HelpButton.tsx';
import type { HelpArticle } from '../../help/helpRegistry.ts';
import styles from './TopBar.module.css';

interface TopBarProps {
  workCenterName: string;
  workCenterId: string;
  productionLineName: string;
  assetName: string;
  operatorName: string;
  welders: Welder[];
  onAddWelder: (employeeNumber: string) => void;
  onRemoveWelder: (userId: string) => void;
  externalInput: boolean;
  helpArticle?: HelpArticle;
}

export function TopBar({
  workCenterName,
  workCenterId,
  productionLineName,
  assetName,
  operatorName,
  welders,
  onAddWelder,
  onRemoveWelder,
  externalInput,
  helpArticle,
}: TopBarProps) {
  const [addWelderOpen, setAddWelderOpen] = useState(false);
  const [newWelderEmpNo, setNewWelderEmpNo] = useState('');
  const [lookupName, setLookupName] = useState<string | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  useEffect(() => {
    if (!addWelderOpen) {
      setNewWelderEmpNo('');
      setLookupName(null);
      setLookupLoading(false);
    }
  }, [addWelderOpen]);

  useEffect(() => () => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
  }, []);

  const doLookup = useCallback((empNo: string) => {
    if (!empNo.trim() || !workCenterId) {
      setLookupName(null);
      return;
    }
    setLookupLoading(true);
    workCenterApi.lookupWelder(workCenterId, empNo.trim())
      .then(w => setLookupName(w.displayName))
      .catch(() => setLookupName('Not found'))
      .finally(() => setLookupLoading(false));
  }, [workCenterId]);

  const handleEmpNoChange = useCallback((value: string) => {
    setNewWelderEmpNo(value);
    setLookupName(null);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => doLookup(value), 500);
  }, [doLookup]);

  const handleAddWelder = useCallback(() => {
    if (newWelderEmpNo.trim()) {
      onAddWelder(newWelderEmpNo.trim());
      setNewWelderEmpNo('');
      setLookupName(null);
      setAddWelderOpen(false);
    }
  }, [newWelderEmpNo, onAddWelder]);

  return (
    <header className={styles.topBar}>
      <div className={styles.wcInfo}>
        <span className={styles.wcName}>{workCenterName}</span>
        <span className={styles.wcDetail}>
          {productionLineName} · {assetName}
        </span>
      </div>

      <div className={styles.operator}>
        <span>{operatorName}</span>
        {!externalInput && (
          <HelpButton currentArticle={helpArticle} className={styles.addWelderBtn} />
        )}
      </div>

      <div className={styles.welders}>
        <span className={styles.welderLabel}>Welder(s):</span>
        {welders.length === 0 ? (
          <span className={styles.noWelders}>No Welders</span>
        ) : (
          welders.map((w) => (
            <span key={w.userId} className={styles.welderChip}>
              {w.displayName}
              {!externalInput && (
                <button
                  className={styles.removeWelder}
                  onClick={() => onRemoveWelder(w.userId)}
                  aria-label={`Remove ${w.displayName}`}
                >
                  <DismissRegular fontSize={14} />
                </button>
              )}
            </span>
          ))
        )}

        {!externalInput && (
          <Dialog
            open={addWelderOpen}
            onOpenChange={(_, data) => setAddWelderOpen(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Button
                appearance="subtle"
                icon={<PersonAddRegular />}
                size="small"
                className={styles.addWelderBtn}
                aria-label="Add welder"
              />
            </DialogTrigger>
            <DialogSurface className={styles.addWelderDialog}>
              <DialogBody>
                <DialogTitle>Add Welder</DialogTitle>
                <DialogContent>
                  <p className={styles.addWelderStatus}>
                    Enter an employee number to add a welder for this work center.
                  </p>

                  {welders.length > 0 && (
                    <div className={styles.dialogWelders}>
                      <span className={styles.dialogWeldersLabel}>Current Welders:</span>
                      <div className={styles.dialogWeldersList}>
                        {welders.map((w) => (
                          <span key={`dialog-${w.userId}`} className={styles.welderChip}>
                            {w.displayName}
                            <button
                              className={styles.removeWelder}
                              onClick={() => onRemoveWelder(w.userId)}
                              aria-label={`Remove ${w.displayName}`}
                            >
                              <DismissRegular fontSize={14} />
                            </button>
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className={styles.addWelderForm}>
                    <Input
                      placeholder="Employee Number"
                      value={newWelderEmpNo}
                      onChange={(_, data) => handleEmpNoChange(data.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') handleAddWelder();
                      }}
                      size="large"
                      inputMode="numeric"
                    />
                    <span
                      className={`${styles.lookupStatus} ${lookupName === 'Not found' ? styles.lookupStatusError : styles.lookupStatusSuccess}`}
                      aria-live="polite"
                    >
                      {lookupLoading ? 'Looking up...' : lookupName ?? ''}
                    </span>
                  </div>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">
                      Cancel
                    </Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleAddWelder}
                    disabled={!newWelderEmpNo.trim()}
                  >
                    Add Welder
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        )}
      </div>
    </header>
  );
}
