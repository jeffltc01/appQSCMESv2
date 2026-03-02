import styles from './NextStepBanner.module.css';

export interface NextStepInstruction {
  title: string;
  detail?: string;
  isActive: boolean;
}

interface NextStepBannerProps {
  instruction: NextStepInstruction;
}

export function NextStepBanner({ instruction }: NextStepBannerProps) {
  return (
    <div className={`${styles.scanStateBanner} ${instruction.isActive ? styles.scanStateBannerActive : styles.scanStateBannerIdle}`}>
      <span className={styles.scanStateTitle}>{instruction.title}</span>
      {instruction.detail && <span className={styles.scanStateDetail}>{instruction.detail}</span>}
    </div>
  );
}
