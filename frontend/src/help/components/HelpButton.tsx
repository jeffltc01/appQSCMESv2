import { useState } from 'react';
import { Button } from '@fluentui/react-components';
import { QuestionCircle24Regular } from '@fluentui/react-icons';
import { HelpDialog } from './HelpDialog.tsx';
import type { HelpArticle } from '../helpRegistry.ts';

interface HelpButtonProps {
  currentArticle?: HelpArticle;
  className?: string;
}

export function HelpButton({ currentArticle, className }: HelpButtonProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        appearance="subtle"
        icon={<QuestionCircle24Regular />}
        className={className}
        onClick={() => setOpen(true)}
        aria-label="Help"
      />
      <HelpDialog
        open={open}
        onClose={() => setOpen(false)}
        initialSlug={currentArticle?.slug}
      />
    </>
  );
}
