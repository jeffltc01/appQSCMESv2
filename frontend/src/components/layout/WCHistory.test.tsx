import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { WCHistory } from './WCHistory';
import type { WCHistoryEntry } from '../../types/domain';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantTimeZoneId: 'America/Denver' },
  }),
}));

vi.mock('../../api/endpoints', () => ({
  adminAnnotationTypeApi: {
    getAll: vi.fn().mockResolvedValue([
      { id: 't1', name: 'Correction Needed', abbreviation: 'C', requiresResolution: true, operatorCanCreate: true, displayColor: '#ffff00' },
    ]),
  },
  logViewerApi: {
    createAnnotation: vi.fn().mockResolvedValue({}),
  },
}));

function makeRecord(overrides: Partial<WCHistoryEntry> & { id: string }): WCHistoryEntry {
  return {
    productionRecordId: overrides.id,
    timestamp: '2026-02-19T14:30:00Z',
    serialOrIdentifier: 'SH001',
    hasAnnotation: false,
    ...overrides,
  };
}

function renderWCHistory(props: Parameters<typeof WCHistory>[0]) {
  return render(
    <MemoryRouter>
      <WCHistory {...props} />
    </MemoryRouter>,
  );
}

describe('WCHistory', () => {
  it('shows history title', () => {
    renderWCHistory({ data: { dayCount: 0, recentRecords: [] } });
    expect(screen.getByText('Last 5 Transactions')).toBeInTheDocument();
  });

  it('does not display day count header text', () => {
    renderWCHistory({ data: { dayCount: 42, recentRecords: [] } });
    expect(screen.queryByText(/Today's Count:/)).not.toBeInTheDocument();
  });

  it('shows "No History Found" when empty', () => {
    renderWCHistory({ data: { dayCount: 0, recentRecords: [] } });
    expect(screen.getByText('No History Found')).toBeInTheDocument();
  });

  it('displays recent records with serial numbers and tank sizes', () => {
    renderWCHistory({
      data: {
        dayCount: 3,
        recentRecords: [
          makeRecord({ id: '1', serialOrIdentifier: 'SH001', tankSize: 120 }),
          makeRecord({ id: '2', serialOrIdentifier: 'SH002', tankSize: 250, hasAnnotation: true, annotationColor: '#ff0000' }),
        ],
      },
    });
    expect(screen.getByText('SH001')).toBeInTheDocument();
    expect(screen.getByText('SH002')).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getAllByText('2/19').length).toBeGreaterThan(0);
    expect(screen.getAllByText('7:30 AM').length).toBeGreaterThan(0);
    expect(screen.queryByText(/Today's Count:/)).not.toBeInTheDocument();
  });

  it('renders annotation flag with the correct color from annotationColor', () => {
    const { container } = renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          makeRecord({ id: '1', hasAnnotation: true, annotationColor: '#ff0000' }),
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
          makeRecord({ id: '1', hasAnnotation: false }),
        ],
      },
    });
    const flagSvg = container.querySelector('svg');
    expect(flagSvg).not.toBeNull();
    expect(flagSvg!.style.color).not.toBe('rgb(255, 0, 0)');
  });

  it('shows screen-specific log CTA when logCta is provided and external input is off', () => {
    renderWCHistory({
      data: { dayCount: 0, recentRecords: [] },
      logCta: { label: 'View Rolls Log', logType: 'rolls' },
      externalInput: false,
    });
    expect(screen.getByText('View Rolls Log')).toBeInTheDocument();
  });

  it('hides log CTA when logCta is not provided', () => {
    renderWCHistory({ data: { dayCount: 0, recentRecords: [] } });
    expect(screen.queryByText('View Rolls Log')).not.toBeInTheDocument();
  });

  it('hides log CTA when external input is on', () => {
    renderWCHistory({
      data: { dayCount: 0, recentRecords: [] },
      logCta: { label: 'View Rolls Log', logType: 'rolls' },
      externalInput: true,
    });
    expect(screen.queryByText('View Rolls Log')).not.toBeInTheDocument();
  });

  it('renders flag as a clickable button with aria-label', () => {
    renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          makeRecord({ id: '1', serialOrIdentifier: 'SH001' }),
        ],
      },
      operatorId: 'op-1',
    });
    const btn = screen.getByRole('button', { name: /add annotation for SH001/i });
    expect(btn).toBeInTheDocument();
    expect(btn).not.toBeDisabled();
  });

  it('disables flag button when operatorId is not provided', () => {
    renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          makeRecord({ id: '1', serialOrIdentifier: 'SH001' }),
        ],
      },
    });
    const btn = screen.getByRole('button', { name: /add annotation for SH001/i });
    expect(btn).toBeDisabled();
  });

  it('opens annotation dialog when flag is clicked', async () => {
    const user = userEvent.setup();
    renderWCHistory({
      data: {
        dayCount: 1,
        recentRecords: [
          makeRecord({ id: '1', productionRecordId: 'pr-1', serialOrIdentifier: 'SH001' }),
        ],
      },
      operatorId: 'op-1',
    });
    const btn = screen.getByRole('button', { name: /add annotation for SH001/i });
    await user.click(btn);
    expect(screen.getByRole('heading', { name: 'Create Annotation' })).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
