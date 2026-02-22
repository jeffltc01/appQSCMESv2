import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Button,
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

export function HelpDialog({ open, onClose, initialSlug }: HelpDialogProps) {
  const [activeSlug, setActiveSlug] = useState(initialSlug ?? 'overview');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (open && initialSlug) {
      setActiveSlug(initialSlug);
    }
  }, [open, initialSlug]);

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

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface className={styles.surface}>
        <DialogBody className={styles.body}>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                icon={<Dismiss24Regular />}
                onClick={onClose}
                aria-label="Close"
              />
            }
            className={styles.title}
          >
            MES v2 Help
          </DialogTitle>
          <DialogContent className={styles.contentArea}>
            <nav className={styles.toc}>
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
            <div className={styles.article}>
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
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
