import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { WCHistory } from './WCHistory';

function renderWCHistory(props: Parameters<typeof WCHistory>[0]) {
  return render(
    <MemoryRouter>
      <WCHistory {...props} />
    </MemoryRouter>,
  );
}

describe('WCHistory', () => {
  it('displays day count in header', () => {
    renderWCHistory({ data: { dayCount: 42, recentRecords: [] } });
    expect(screen.getByText(/Today's Count: 42/)).toBeInTheDocument();
  });

  it('shows "No records today" when empty', () => {
    renderWCHistory({ data: { dayCount: 0, recentRecords: [] } });
    expect(screen.getByText('No records today')).toBeInTheDocument();
  });

  it('displays recent records with serial numbers and tank sizes', () => {
    renderWCHistory({
      data: {
        dayCount: 3,
        recentRecords: [
          { id: '1', timestamp: '2026-02-19T14:30:00Z', serialOrIdentifier: 'SH001', tankSize: 120, hasAnnotation: false },
          { id: '2', timestamp: '2026-02-19T14:25:00Z', serialOrIdentifier: 'SH002', tankSize: 250, hasAnnotation: true, annotationColor: '#ff0000' },
        ],
      },
    });
    expect(screen.getByText('SH001')).toBeInTheDocument();
    expect(screen.getByText('SH002')).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getByText(/Today's Count: 3/)).toBeInTheDocument();
  });

  it('renders annotation flag with the correct color from annotationColor', () => {
    const { container } = renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          { id: '1', timestamp: '2026-02-19T14:30:00Z', serialOrIdentifier: 'SH001', tankSize: 120, hasAnnotation: true, annotationColor: '#ff0000' },
        ],
      },
    });
    const flagSvg = container.querySelector('svg');
    expect(flagSvg).not.toBeNull();
    expect(flagSvg!.style.color).toBe('rgb(255, 0, 0)');
  });

  it('renders muted flag when no annotation exists', () => {
    const { container } = renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          { id: '1', timestamp: '2026-02-19T14:30:00Z', serialOrIdentifier: 'SH001', tankSize: 120, hasAnnotation: false },
        ],
      },
    });
    const flagSvg = container.querySelector('svg');
    expect(flagSvg).not.toBeNull();
    expect(flagSvg!.style.color).not.toBe('rgb(255, 0, 0)');
  });

  it('shows View Full Log link when logType is provided', () => {
    renderWCHistory({
      data: { dayCount: 0, recentRecords: [] },
      logType: 'rolls',
    });
    expect(screen.getByText('View Full Log')).toBeInTheDocument();
  });

  it('hides View Full Log link when logType is not provided', () => {
    renderWCHistory({ data: { dayCount: 0, recentRecords: [] } });
    expect(screen.queryByText('View Full Log')).not.toBeInTheDocument();
  });
});
