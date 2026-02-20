import { useEffect } from 'react';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import styles from './SpotXrayScreen.module.css';

export function SpotXrayScreen(props: WorkCenterProps) {
  const { setRequiresWelder } = props;

  useEffect(() => {
    setRequiresWelder(false);
  }, []);

  return (
    <div className={styles.container}>
      <h2 className={styles.title}>Spot X-ray</h2>
      <p className={styles.message}>Specification pending — this work center will be configured in a future update.</p>
    </div>
  );
}
