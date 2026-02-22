import { useState, useCallback, useEffect, useRef } from 'react';
import {
  Button,
  Input,
  Popover,
  PopoverTrigger,
  PopoverSurface,
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
      setLookupName(null);
      setLookupLoading(false);
    }
  }, [addWelderOpen]);

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
          <Popover
            open={addWelderOpen}
            onOpenChange={(_, data) => setAddWelderOpen(data.open)}
          >
            <PopoverTrigger>
              <Button
                appearance="subtle"
                icon={<PersonAddRegular />}
                size="small"
                className={styles.addWelderBtn}
                aria-label="Add welder"
              />
            </PopoverTrigger>
            <PopoverSurface>
              <div className={styles.addWelderForm}>
                <Input
                  placeholder="Employee No."
                  value={newWelderEmpNo}
                  onChange={(_, data) => handleEmpNoChange(data.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleAddWelder();
                  }}
                  size="medium"
                  inputMode="numeric"
                />
                {(lookupLoading || lookupName) && (
                  <span style={{ fontSize: 12, color: lookupName === 'Not found' ? '#c4314b' : '#616161', minHeight: 16 }}>
                    {lookupLoading ? 'Looking up...' : lookupName}
                  </span>
                )}
                <Button appearance="primary" size="small" onClick={handleAddWelder}>
                  Add
                </Button>
              </div>
            </PopoverSurface>
          </Popover>
        )}
      </div>
    </header>
  );
}
