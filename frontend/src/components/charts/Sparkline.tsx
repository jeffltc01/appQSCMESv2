type SparklineProps = {
  values: Array<number | null | undefined>;
  color?: string;
  testId?: string;
  ariaLabel?: string;
};

const WIDTH = 110;
const HEIGHT = 28;
const PADDING = 2;

export function Sparkline({
  values,
  color = '#7dc3ff',
  testId,
  ariaLabel = 'KPI trend',
}: SparklineProps) {
  const normalized = values.map((value) => (value == null ? null : Number(value)));
  const numericValues = normalized.filter((value): value is number => value !== null && Number.isFinite(value));

  if (normalized.length === 0 || numericValues.length === 0) {
    return (
      <svg
        data-testid={testId}
        role="img"
        aria-label={ariaLabel}
        viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      />
    );
  }

  const min = Math.min(...numericValues);
  const max = Math.max(...numericValues);
  const valueRange = max - min;
  const stepX = normalized.length > 1 ? (WIDTH - PADDING * 2) / (normalized.length - 1) : 0;

  const points = normalized
    .map((value, idx) => {
      if (value == null || !Number.isFinite(value)) return null;
      const x = PADDING + idx * stepX;
      const y = valueRange === 0
        ? HEIGHT / 2
        : HEIGHT - PADDING - ((value - min) / valueRange) * (HEIGHT - PADDING * 2);
      return `${x},${y}`;
    })
    .filter((point): point is string => point !== null)
    .join(' ');

  return (
    <svg
      data-testid={testId}
      role="img"
      aria-label={ariaLabel}
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      preserveAspectRatio="none"
    >
      <polyline
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        points={points}
      />
    </svg>
  );
}
