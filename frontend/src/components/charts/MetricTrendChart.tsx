type TrendPoint = {
  date: string;
  value: number | null;
};

type MetricTrendChartProps = {
  points: TrendPoint[];
  color?: string;
  testId?: string;
  ariaLabel?: string;
};

const WIDTH = 760;
const HEIGHT = 260;
const PADDING_LEFT = 46;
const PADDING_RIGHT = 12;
const PADDING_TOP = 14;
const PADDING_BOTTOM = 36;

function formatAxisNumber(value: number): string {
  if (!Number.isFinite(value)) return '--';
  return Number.isInteger(value) ? String(value) : value.toFixed(1);
}

export function MetricTrendChart({
  points,
  color = '#2b64ca',
  testId,
  ariaLabel = 'Metric trend for last 30 days',
}: MetricTrendChartProps) {
  const normalized = points.map((point) => (
    point.value == null || !Number.isFinite(point.value)
      ? { ...point, value: null }
      : point
  ));
  const numericValues = normalized
    .map((point) => point.value)
    .filter((value): value is number => value !== null);

  const plotWidth = WIDTH - PADDING_LEFT - PADDING_RIGHT;
  const plotHeight = HEIGHT - PADDING_TOP - PADDING_BOTTOM;

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

  let min = Math.min(...numericValues);
  let max = Math.max(...numericValues);

  if (min === max) {
    min -= 1;
    max += 1;
  }

  const valueRange = max - min;
  const stepX = normalized.length > 1 ? plotWidth / (normalized.length - 1) : 0;

  const yFor = (value: number) => (
    PADDING_TOP + plotHeight - ((value - min) / valueRange) * plotHeight
  );
  const xFor = (idx: number) => PADDING_LEFT + idx * stepX;

  const lineSegments: string[] = [];
  let activeSegment: string[] = [];

  normalized.forEach((point, idx) => {
    if (point.value === null) {
      if (activeSegment.length > 1) {
        lineSegments.push(activeSegment.join(' '));
      }
      activeSegment = [];
      return;
    }

    activeSegment.push(`${xFor(idx)},${yFor(point.value)}`);
  });

  if (activeSegment.length > 1) {
    lineSegments.push(activeSegment.join(' '));
  }

  const yTicks = [0, 0.5, 1].map((ratio) => {
    const y = PADDING_TOP + plotHeight * ratio;
    const val = max - ratio * valueRange;
    return { y, label: formatAxisNumber(val) };
  });

  const firstPoint = normalized[0];
  const lastPoint = normalized[normalized.length - 1];

  return (
    <svg
      data-testid={testId}
      role="img"
      aria-label={ariaLabel}
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      preserveAspectRatio="none"
    >
      <rect
        x={PADDING_LEFT}
        y={PADDING_TOP}
        width={plotWidth}
        height={plotHeight}
        fill="transparent"
        stroke="#d3d9e6"
        strokeWidth="1"
      />

      {yTicks.map((tick) => (
        <g key={tick.y}>
          <line
            x1={PADDING_LEFT}
            y1={tick.y}
            x2={PADDING_LEFT + plotWidth}
            y2={tick.y}
            stroke="#e4e9f3"
            strokeWidth="1"
          />
          <text
            x={PADDING_LEFT - 8}
            y={tick.y + 4}
            textAnchor="end"
            fontSize="11"
            fill="#5f6f8f"
          >
            {tick.label}
          </text>
        </g>
      ))}

      {lineSegments.map((segment, idx) => (
        <polyline
          key={`segment-${idx}`}
          fill="none"
          stroke={color}
          strokeWidth="3"
          strokeLinecap="round"
          strokeLinejoin="round"
          points={segment}
        />
      ))}

      <text
        x={PADDING_LEFT}
        y={HEIGHT - 12}
        textAnchor="start"
        fontSize="11"
        fill="#5f6f8f"
      >
        {firstPoint?.date ?? ''}
      </text>
      <text
        x={PADDING_LEFT + plotWidth}
        y={HEIGHT - 12}
        textAnchor="end"
        fontSize="11"
        fill="#5f6f8f"
      >
        {lastPoint?.date ?? ''}
      </text>
    </svg>
  );
}
