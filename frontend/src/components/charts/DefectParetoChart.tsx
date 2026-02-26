import { useEffect, useRef, useState } from 'react';

type DefectParetoItem = {
  defectCode: string;
  defectName: string;
  count: number;
  cumulativePercent: number;
};

type DefectParetoChartProps = {
  items: DefectParetoItem[];
  testId?: string;
  ariaLabel?: string;
};

const DEFAULT_WIDTH = 820;
const DEFAULT_HEIGHT = 380;
const PADDING_LEFT = 70;
const PADDING_RIGHT = 74;
const PADDING_TOP = 28;
const PADDING_BOTTOM = 92;
const BAR_GAP = 12;

function clampPercent(value: number): number {
  if (!Number.isFinite(value)) return 0;
  if (value < 0) return 0;
  if (value > 100) return 100;
  return value;
}

export function DefectParetoChart({
  items,
  testId,
  ariaLabel = 'Defect Pareto chart',
}: DefectParetoChartProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [size, setSize] = useState({ width: DEFAULT_WIDTH, height: DEFAULT_HEIGHT });

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const applySize = () => {
      const nextWidth = Math.max(640, Math.round(el.clientWidth));
      const nextHeight = Math.max(220, Math.round(el.clientHeight));
      setSize((prev) => (
        prev.width === nextWidth && prev.height === nextHeight
          ? prev
          : { width: nextWidth, height: nextHeight }
      ));
    };

    applySize();
    const observer = new ResizeObserver(applySize);
    observer.observe(el);

    return () => observer.disconnect();
  }, []);

  const width = size.width;
  const height = size.height;
  const plotWidth = Math.max(80, width - PADDING_LEFT - PADDING_RIGHT);
  const plotHeight = Math.max(80, height - PADDING_TOP - PADDING_BOTTOM);

  const maxCount = Math.max(...items.map((item) => item.count), 1);
  const paddedMaxCount = Math.ceil(maxCount * 1.15);
  const axisStep = paddedMaxCount <= 10 ? 1 : paddedMaxCount <= 50 ? 5 : 10;
  const yAxisMax = Math.max(axisStep, Math.ceil(paddedMaxCount / axisStep) * axisStep);
  const rawBarWidth = plotWidth / Math.max(items.length * 1.9, 1);
  const effectiveBarWidth = Math.max(24, Math.min(96, rawBarWidth));
  const barsTotalWidth = effectiveBarWidth * items.length + BAR_GAP * Math.max(items.length - 1, 0);
  const barsStartX = PADDING_LEFT + Math.max(0, (plotWidth - barsTotalWidth) / 2);
  const leftTickCount = 8;
  const leftAxisTicks = Array.from({ length: leftTickCount + 1 }, (_, idx) => {
    const ratio = idx / leftTickCount;
    return {
      y: PADDING_TOP + plotHeight * (1 - ratio),
      label: Math.round((yAxisMax * ratio) / axisStep) * axisStep,
    };
  });
  const rightAxisTicks = Array.from({ length: 11 }, (_, idx) => {
    const pct = idx * 10;
    return {
      y: PADDING_TOP + plotHeight - (pct / 100) * plotHeight,
      label: `${pct.toFixed(1)}%`,
    };
  });

  const yForCount = (count: number) => (
    PADDING_TOP + plotHeight - (count / yAxisMax) * plotHeight
  );
  const yForPercent = (pct: number) => (
    PADDING_TOP + plotHeight - (clampPercent(pct) / 100) * plotHeight
  );

  const linePoints = items.map((item, idx) => {
    const x = barsStartX + idx * (effectiveBarWidth + BAR_GAP) + effectiveBarWidth / 2;
    const y = yForPercent(item.cumulativePercent);
    return `${x},${y}`;
  }).join(' ');

  return (
    <div ref={containerRef} style={{ width: '100%', height: '100%' }}>
      <svg
        data-testid={testId}
        role="img"
        aria-label={ariaLabel}
        viewBox={`0 0 ${width} ${height}`}
        width={width}
        height={height}
        style={{ display: 'block' }}
        textRendering="geometricPrecision"
      >
        {items.length === 0 ? null : (
          <>
      <rect
        x={PADDING_LEFT}
        y={PADDING_TOP}
        width={plotWidth}
        height={plotHeight}
        fill="#ffffff"
        stroke="#b8c0cc"
        strokeWidth="1"
      />
      {leftAxisTicks.map((tick) => (
        <g key={`tick-${tick.y}`}>
          <line
            x1={PADDING_LEFT}
            y1={tick.y}
            x2={PADDING_LEFT + plotWidth}
            y2={tick.y}
            stroke="#d5dbe5"
            strokeWidth="1"
          />
          <text
            x={PADDING_LEFT - 8}
            y={tick.y + 4}
            textAnchor="end"
            fontSize="12"
            fill="#4b5563"
          >
            {tick.label}
          </text>
        </g>
      ))}
      {items.map((item, idx) => {
        const x = barsStartX + idx * (effectiveBarWidth + BAR_GAP);
        const y = yForCount(item.count);
        const h = PADDING_TOP + plotHeight - y;
        const axisLabelSource = item.defectName?.trim() || item.defectCode;
        const label = axisLabelSource.length > 14
          ? `${axisLabelSource.slice(0, 14)}…`
          : axisLabelSource;
        return (
          <g key={`${item.defectCode}-${idx}`}>
            <rect
              x={x}
              y={y}
              width={effectiveBarWidth}
              height={h}
              fill="#4f81bd"
              stroke="#2f5f96"
              strokeWidth="1"
            />
            <text
              x={x + effectiveBarWidth / 2}
              y={Math.max(y - 6, PADDING_TOP + 12)}
              textAnchor="middle"
              fontSize="12"
              fontWeight="700"
              fill="#1f3657"
            >
              {item.count}
            </text>
            <text
              x={x + effectiveBarWidth / 2}
              y={height - 36}
              textAnchor="middle"
              fontSize="13"
              fill="#1f2937"
              transform={`rotate(-45 ${x + effectiveBarWidth / 2} ${height - 36})`}
            >
              {label || item.defectCode}
            </text>
          </g>
        );
      })}
      <polyline
        fill="none"
        stroke="#b32025"
        strokeWidth="3.5"
        strokeLinecap="round"
        strokeLinejoin="round"
        points={linePoints}
      />
      {items.map((item, idx) => {
        const x = barsStartX + idx * (effectiveBarWidth + BAR_GAP) + effectiveBarWidth / 2;
        const y = yForPercent(item.cumulativePercent);
        return (
          <circle
            key={`point-${item.defectCode}-${idx}`}
            cx={x}
            cy={y}
            r="5"
            fill="#b32025"
            stroke="#f1f5f9"
            strokeWidth="1.5"
          />
        );
      })}
      {items.map((item, idx) => {
        const x = barsStartX + idx * (effectiveBarWidth + BAR_GAP) + effectiveBarWidth / 2;
        const y = yForPercent(item.cumulativePercent);
        const percentLabel = `${clampPercent(item.cumulativePercent).toFixed(1)}%`;
        return (
          <text
            key={`pct-${item.defectCode}-${idx}`}
            x={x}
            y={Math.max(y - 12, PADDING_TOP + 12)}
            textAnchor="middle"
            fontSize="12"
            fontWeight="700"
            fill="#b32025"
          >
            {percentLabel}
          </text>
        );
      })}
      {rightAxisTicks.map((tick) => (
        <text
          key={`right-${tick.label}`}
          x={PADDING_LEFT + plotWidth + 8}
          y={tick.y + 4}
          textAnchor="start"
          fontSize="12"
          fill="#4b5563"
        >
          {tick.label}
        </text>
      ))}
      <text
        x={24}
        y={PADDING_TOP + plotHeight / 2}
        textAnchor="middle"
        fontSize="14"
        fontWeight="700"
        fill="#111827"
        transform={`rotate(-90 24 ${PADDING_TOP + plotHeight / 2})`}
      >
        Quantity
      </text>
      <text
        x={width - 16}
        y={PADDING_TOP + plotHeight / 2}
        textAnchor="middle"
        fontSize="14"
        fontWeight="700"
        fill="#111827"
        transform={`rotate(90 ${width - 16} ${PADDING_TOP + plotHeight / 2})`}
      >
        Percentage
      </text>
      <text
        x={PADDING_LEFT + plotWidth / 2}
        y={height - 6}
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#111827"
      >
        Defects
      </text>
          </>
        )}
      </svg>
    </div>
  );
}
