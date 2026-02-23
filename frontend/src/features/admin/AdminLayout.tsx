import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Spinner } from '@fluentui/react-components';
import { ArrowLeftRegular, AddRegular, SignOutRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { HelpButton } from '../../help/components/HelpButton.tsx';
import { useCurrentHelpArticle } from '../../help/useCurrentHelpArticle.ts';
import { nlqApi } from '../../api/endpoints.ts';
import type {
  NaturalLanguageQueryContextRequest,
  NaturalLanguageQueryRequest,
} from '../../types/api.ts';
import type { NaturalLanguageQueryResponse } from '../../types/domain.ts';
import styles from './AdminLayout.module.css';

interface AdminLayoutProps {
  title: string;
  children: React.ReactNode;
  onAdd?: () => void;
  addLabel?: string;
  backLabel?: string;
  onBack?: () => void;
  nlqContext?: NaturalLanguageQueryContextRequest;
}

export function AdminLayout({
  title,
  children,
  onAdd,
  addLabel = 'Add',
  backLabel,
  onBack,
  nlqContext,
}: AdminLayoutProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const helpArticle = useCurrentHelpArticle();
  const [question, setQuestion] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<NaturalLanguageQueryResponse | null>(null);
  const [error, setError] = useState('');
  const canAskMes = (user?.roleTier ?? 99) <= 5.0;

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleAsk = async () => {
    const clean = question.trim();
    if (!clean) return;
    setLoading(true);
    setError('');
    setResult(null);
    try {
      const req: NaturalLanguageQueryRequest = {
        question: clean,
        context: {
          plantId: user?.defaultSiteId,
          ...nlqContext,
        },
      };
      const response = await nlqApi.ask(req);
      setResult(response);
    } catch (err) {
      const message = typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message?: unknown }).message ?? '')
        : '';
      setError(message || 'Unable to answer right now. Please try again.');
    } finally {
      setLoading(false);
    }
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
      {canAskMes && (
        <section className={styles.askMesBar}>
          <label htmlFor="ask-mes-input" className={styles.askMesLabel}>Ask MES</label>
          <input
            id="ask-mes-input"
            className={styles.askMesInput}
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Ask a metrics question (for example: how many tanks have we produced today?)"
          />
          <Button
            appearance="primary"
            className={styles.askMesButton}
            onClick={handleAsk}
            disabled={loading || question.trim().length === 0}
          >
            {loading ? <Spinner size="tiny" /> : 'Ask'}
          </Button>
        </section>
      )}
      {canAskMes && (result || error) && (
        <section className={styles.askMesResult}>
          {error ? (
            <div className={styles.askMesError}>{error}</div>
          ) : (
            <>
              <div className={styles.askMesAnswer}>{result?.answerText}</div>
              {result?.dataPoints?.length ? (
                <ul className={styles.askMesDataPoints}>
                  {result.dataPoints.map((dp, idx) => (
                    <li key={`${dp.label}-${idx}`}>
                      {dp.label}: {dp.value}{dp.unit ? ` (${dp.unit})` : ''}
                    </li>
                  ))}
                </ul>
              ) : null}
              <div className={styles.askMesMeta}>
                Scope: {result?.scopeUsed ?? '--'} | Intent: {result?.trace?.intent ?? '--'}
              </div>
            </>
          )}
        </section>
      )}
      <main className={styles.content}>
        {children}
      </main>
    </div>
  );
}
