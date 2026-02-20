import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { WCHistory } from './WCHistory';

describe('WCHistory', () => {
  it('displays day count in header', () => {
    render(
      <WCHistory data={{ dayCount: 42, recentRecords: [] }} />,
    );
    expect(screen.getByText('History')).toBeInTheDocument();
    expect(screen.getByText(/Today's Count: 42/)).toBeInTheDocument();
  });

  it('shows "No records today" when empty', () => {
    render(
      <WCHistory data={{ dayCount: 0, recentRecords: [] }} />,
    );
    expect(screen.getByText('No records today')).toBeInTheDocument();
  });

  it('displays recent records with serial numbers and tank sizes', () => {
    render(
      <WCHistory
        data={{
          dayCount: 3,
          recentRecords: [
            { id: '1', timestamp: '2026-02-19T14:30:00Z', serialOrIdentifier: 'SH001', tankSize: 120, hasAnnotation: false },
            { id: '2', timestamp: '2026-02-19T14:25:00Z', serialOrIdentifier: 'SH002', tankSize: 250, hasAnnotation: true },
          ],
        }}
      />,
    );
    expect(screen.getByText('SH001')).toBeInTheDocument();
    expect(screen.getByText('SH002')).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getByText(/Today's Count: 3/)).toBeInTheDocument();
  });
});
