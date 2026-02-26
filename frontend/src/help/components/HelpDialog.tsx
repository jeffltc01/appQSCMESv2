import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Dialog,
  DialogSurface,
  Spinner,
} from '@fluentui/react-components';
import { Dismiss24Regular, ArrowDownload24Regular } from '@fluentui/react-icons';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { helpArticles, categoryLabels, type HelpArticle } from '../helpRegistry.ts';
import styles from './HelpDialog.module.css';

const articleModules = import.meta.glob<string>('/src/help/articles/*.md', {
  query: '?raw',
  import: 'default',
});

function buildModuleKey(slug: string): string {
  return `/src/help/articles/${slug}.md`;
}

interface HelpDialogProps {
  open: boolean;
  onClose: () => void;
  initialSlug?: string;
}

const ANIMATION_DURATION_MS = 260;
const HELP_OPEN_CLASS = 'help-scroll-open';

type DialogPhase = 'hidden' | 'entering' | 'open' | 'closing';

export function HelpDialog({ open, onClose, initialSlug }: HelpDialogProps) {
  const [isMounted, setIsMounted] = useState(open);
  const [phase, setPhase] = useState<DialogPhase>(open ? 'open' : 'hidden');
  const [activeSlug, setActiveSlug] = useState(initialSlug ?? 'overview');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (open) {
      setIsMounted(true);
      setPhase('entering');
      const enterTimer = window.setTimeout(() => {
        setPhase('open');
      }, ANIMATION_DURATION_MS);
      return () => window.clearTimeout(enterTimer);
    }
    if (isMounted) {
      setPhase('closing');
    }
  }, [open, isMounted]);

  useEffect(() => {
    if (phase !== 'closing') {
      return;
    }
    const closeTimer = window.setTimeout(() => {
      setIsMounted(false);
      setPhase('hidden');
    }, ANIMATION_DURATION_MS);
    return () => window.clearTimeout(closeTimer);
  }, [phase]);

  useEffect(() => {
    if (open && initialSlug) {
      setActiveSlug(initialSlug);
    }
  }, [open, initialSlug]);

  useEffect(() => {
    const { documentElement, body } = document;
    if (isMounted) {
      documentElement.classList.add(HELP_OPEN_CLASS);
      body.classList.add(HELP_OPEN_CLASS);
      return () => {
        documentElement.classList.remove(HELP_OPEN_CLASS);
        body.classList.remove(HELP_OPEN_CLASS);
      };
    }
    documentElement.classList.remove(HELP_OPEN_CLASS);
    body.classList.remove(HELP_OPEN_CLASS);
    return undefined;
  }, [isMounted]);

  const loadArticle = useCallback(async (slug: string) => {
    const key = buildModuleKey(slug);
    const loader = articleModules[key];
    if (!loader) {
      setContent(`# Article Not Found\n\nNo help article has been written yet for "${slug}".`);
      return;
    }
    setLoading(true);
    try {
      const raw = await loader();
      setContent(raw);
    } catch {
      setContent('# Error\n\nFailed to load article.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (open) {
      loadArticle(activeSlug);
    }
  }, [open, activeSlug, loadArticle]);

  const handleSelect = useCallback((slug: string) => {
    setActiveSlug(slug);
  }, []);

  const grouped = useMemo(() => {
    const groups: Record<HelpArticle['category'], HelpArticle[]> = {
      general: [],
      operator: [],
      admin: [],
    };
    for (const a of helpArticles) {
      groups[a.category].push(a);
    }
    return groups;
  }, []);

  if (!isMounted && phase === 'hidden') {
    return null;
  }

  const surfaceClassName = [
    styles.surface,
    phase === 'entering' ? styles.surfaceEntering : '',
    phase === 'open' ? styles.surfaceOpen : '',
    phase === 'closing' ? styles.surfaceClosing : '',
  ].filter(Boolean).join(' ');

  return (
    <Dialog open={isMounted} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={surfaceClassName} data-testid="help-dialog-surface" data-phase={phase}>
        <div className={styles.body}>
          <div className={styles.header}>
            <div className={styles.headerTitle}>MES v2 Help</div>
            <button
              type="button"
              onClick={onClose}
              aria-label="Close"
              className={styles.closeButton}
            >
              <Dismiss24Regular />
            </button>
          </div>
          <div className={styles.contentArea}>
            <nav className={styles.toc} aria-label="Help table of contents" data-testid="help-dialog-toc">
              {(['general', 'operator', 'admin'] as const).map((cat) => (
                <div key={cat} className={styles.tocGroup}>
                  <div className={styles.tocGroupLabel}>{categoryLabels[cat]}</div>
                  {grouped[cat].map((a) => (
                    <button
                      key={a.slug}
                      className={`${styles.tocItem} ${a.slug === activeSlug ? styles.tocItemActive : ''}`}
                      onClick={() => handleSelect(a.slug)}
                    >
                      {a.title}
                    </button>
                  ))}
                </div>
              ))}
              <div className={styles.tocFooter}>
                <a
                  href="/help/MESv2-Help-Manual.pdf"
                  download
                  className={styles.pdfLink}
                >
                  <ArrowDownload24Regular />
                  Download PDF Manual
                </a>
              </div>
            </nav>
            <div className={styles.article} role="region" aria-label="Help article content" data-testid="help-dialog-article">
              {loading ? (
                <div className={styles.loadingState}>
                  <Spinner size="medium" />
                </div>
              ) : (
                <ReactMarkdown remarkPlugins={[remarkGfm]}>
                  {content}
                </ReactMarkdown>
              )}
            </div>
          </div>
        </div>
      </DialogSurface>
    </Dialog>
  );
}
