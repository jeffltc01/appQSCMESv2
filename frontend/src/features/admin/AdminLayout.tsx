import { useNavigate } from 'react-router-dom';
import { Button } from '@fluentui/react-components';
import { ArrowLeftRegular, AddRegular, SignOutRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { HelpButton } from '../../help/components/HelpButton.tsx';
import { useCurrentHelpArticle } from '../../help/useCurrentHelpArticle.ts';
import styles from './AdminLayout.module.css';

interface AdminLayoutProps {
  title: string;
  children: React.ReactNode;
  onAdd?: () => void;
  addLabel?: string;
  backLabel?: string;
  onBack?: () => void;
}

export function AdminLayout({ title, children, onAdd, addLabel = 'Add', backLabel, onBack }: AdminLayoutProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const helpArticle = useCurrentHelpArticle();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className={styles.shell}>
      <header className={styles.topBar}>
        <div className={styles.topBarLeft}>
          <Button
            appearance="subtle"
            icon={<ArrowLeftRegular />}
            className={styles.backBtn}
            onClick={onBack ?? (() => navigate('/menu'))}
          >
            {backLabel ?? 'Menu'}
          </Button>
          <span className={styles.pageTitle}>{title}</span>
          <span className={styles.plantCode}>{user?.plantName ? `${user.plantName} (${user.plantCode})` : user?.plantCode ?? ''}</span>
        </div>
        <div className={styles.topBarRight}>
          {onAdd && (
            <Button
              appearance="primary"
              icon={<AddRegular />}
              onClick={onAdd}
              size="small"
            >
              {addLabel}
            </Button>
          )}
          <span className={styles.userName}>{user?.displayName ?? ''}</span>
          <HelpButton currentArticle={helpArticle} className={styles.logoutBtn} />
          <Button
            appearance="subtle"
            icon={<SignOutRegular />}
            className={styles.logoutBtn}
            onClick={handleLogout}
          >
            Logout
          </Button>
        </div>
      </header>
      <main className={styles.content}>
        {children}
      </main>
    </div>
  );
}
