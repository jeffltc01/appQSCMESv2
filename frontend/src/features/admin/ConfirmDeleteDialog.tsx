import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  Spinner,
} from '@fluentui/react-components';
import { WarningRegular } from '@fluentui/react-icons';

interface ConfirmDeleteDialogProps {
  open: boolean;
  itemName: string;
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
}

export function ConfirmDeleteDialog({
  open,
  itemName,
  onConfirm,
  onCancel,
  loading = false,
}: ConfirmDeleteDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onCancel(); }}>
      <DialogSurface style={{ maxWidth: 440 }}>
        <DialogBody>
          <DialogTitle>
            <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <WarningRegular style={{ color: '#d13438', fontSize: 20 }} />
              Confirm Deactivation
            </span>
          </DialogTitle>
          <DialogContent style={{ padding: '16px 0' }}>
            <p style={{ margin: '0 0 8px' }}>
              Are you sure you want to deactivate <strong>{itemName}</strong>?
            </p>
            <p style={{ margin: 0, color: '#707070', fontSize: 13 }}>
              The record will be hidden from operational use but preserved for historical data.
              An administrator can reactivate it later via the edit form.
            </p>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={onConfirm}
              disabled={loading}
              style={{ backgroundColor: '#d13438', borderColor: '#d13438' }}
            >
              {loading ? <Spinner size="tiny" /> : 'Deactivate'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
