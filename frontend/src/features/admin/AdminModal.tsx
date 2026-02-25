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
import { DismissRegular } from '@fluentui/react-icons';
import styles from './AdminModal.module.css';

interface AdminModalProps {
  open: boolean;
  title: string;
  children: React.ReactNode;
  onConfirm: () => void;
  onCancel: () => void;
  confirmLabel?: string;
  cancelLabel?: string;
  loading?: boolean;
  error?: string;
  confirmDisabled?: boolean;
  wide?: boolean;
  titleClassName?: string;
  contentClassName?: string;
  closeButtonClassName?: string;
  bodyClassName?: string;
  surfaceClassName?: string;
  hideCancel?: boolean;
}

export function AdminModal({
  open,
  title,
  children,
  onConfirm,
  onCancel,
  confirmLabel = 'OK',
  cancelLabel = 'Cancel',
  loading = false,
  error,
  confirmDisabled = false,
  wide = false,
  titleClassName,
  contentClassName,
  closeButtonClassName,
  bodyClassName,
  surfaceClassName,
  hideCancel = false,
}: AdminModalProps) {
  const baseSurfaceClassName = wide ? styles.surfaceWide : styles.surface;
  const dialogSurfaceClassName = surfaceClassName
    ? `${baseSurfaceClassName} ${surfaceClassName}`
    : baseSurfaceClassName;
  const dialogTitleClassName = titleClassName ? `${styles.title} ${titleClassName}` : styles.title;
  const dialogContentClassName = contentClassName ? `${styles.content} ${contentClassName}` : styles.content;
  const dialogCloseButtonClassName = closeButtonClassName
    ? `${styles.closeButton} ${closeButtonClassName}`
    : styles.closeButton;
  const dialogBodyClassName = bodyClassName ? `${styles.body} ${bodyClassName}` : styles.body;

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onCancel(); }}>
      <DialogSurface className={dialogSurfaceClassName}>
        <DialogBody className={dialogBodyClassName}>
          <Button
            appearance="subtle"
            aria-label="close"
            icon={<DismissRegular />}
            onClick={onCancel}
            className={dialogCloseButtonClassName}
          />
          <DialogTitle className={dialogTitleClassName}>{title}</DialogTitle>
          <DialogContent className={dialogContentClassName}>
            {children}
            {error && <div className={styles.error}>{error}</div>}
          </DialogContent>
          <DialogActions className={styles.actions}>
            {!hideCancel && (
              <Button appearance="secondary" onClick={onCancel} disabled={loading}>
                {cancelLabel}
              </Button>
            )}
            <Button
              appearance="primary"
              onClick={onConfirm}
              disabled={confirmDisabled || loading}
            >
              {loading ? <Spinner size="tiny" /> : confirmLabel}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
