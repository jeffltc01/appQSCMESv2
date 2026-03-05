import { getEnvironmentWatermarkLabel, getRuntimeEnvironment } from '../../config/runtimeEnvironment.ts';
import styles from './EnvironmentWatermark.module.css';

export function EnvironmentWatermark() {
  const label = getEnvironmentWatermarkLabel(getRuntimeEnvironment());
  if (!label) {
    return null;
  }

  const toneClass = label === 'TEST' ? styles.test : styles.dev;

  return (
    <div
      className={`${styles.watermark} ${toneClass}`}
      aria-hidden="true"
      data-testid="environment-watermark"
    >
      <span>{label}</span>
    </div>
  );
}
