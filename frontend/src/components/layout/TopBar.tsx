import { useState, useCallback } from 'react';
import {
  Button,
  Input,
  Popover,
  PopoverTrigger,
  PopoverSurface,
} from '@fluentui/react-components';
import { DismissRegular, PersonAddRegular } from '@fluentui/react-icons';
import type { Welder } from '../../types/domain.ts';
import styles from './TopBar.module.css';

interface TopBarProps {
  workCenterName: string;
  productionLineName: string;
  assetName: string;
  operatorName: string;
  welders: Welder[];
  onAddWelder: (employeeNumber: string) => void;
  onRemoveWelder: (userId: string) => void;
  externalInput: boolean;
}

export function TopBar({
  workCenterName,
  productionLineName,
  assetName,
  operatorName,
  welders,
  onAddWelder,
  onRemoveWelder,
  externalInput,
}: TopBarProps) {
  const [addWelderOpen, setAddWelderOpen] = useState(false);
  const [newWelderEmpNo, setNewWelderEmpNo] = useState('');

  const handleAddWelder = useCallback(() => {
    if (newWelderEmpNo.trim()) {
      onAddWelder(newWelderEmpNo.trim());
      setNewWelderEmpNo('');
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
                  onChange={(_, data) => setNewWelderEmpNo(data.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleAddWelder();
                  }}
                  size="medium"
                  inputMode="numeric"
                />
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
