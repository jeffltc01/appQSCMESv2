import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { QueueHistory } from './QueueHistory';
import type { QueueTransaction } from '../../types/domain';
import { formatShortDateOnly, formatTimeOnly } from '../../utils/dateFormat';

function makeTxn(overrides: Partial<QueueTransaction> = {}): QueueTransaction {
  return {
    id: 'txn-1',
    action: 'added',
    itemSummary: 'Shell SH-001',
    operatorName: 'Op 1',
    timestamp: '2026-02-20T10:30:00Z',
    ...overrides,
  };
}

function renderQueueHistory(transactions: QueueTransaction[]) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <QueueHistory transactions={transactions} />
    </FluentProvider>,
  );
}

describe('QueueHistory', () => {
  it('renders "No recent activity" when empty', () => {
    renderQueueHistory([]);
    expect(screen.getByText('No recent activity')).toBeInTheDocument();
  });

  it('renders transactions with formatted date and summary', () => {
    const ts1 = '2026-02-20T10:30:00Z';
    const ts2 = '2026-02-20T14:00:00Z';
    renderQueueHistory([
      makeTxn({ timestamp: ts1, itemSummary: 'Shell SH-001' }),
      makeTxn({ id: 'txn-2', timestamp: ts2, itemSummary: 'Shell SH-002' }),
    ]);

    expect(screen.getByText('Shell SH-001')).toBeInTheDocument();
    expect(screen.getByText('Shell SH-002')).toBeInTheDocument();
    expect(screen.getAllByText(formatShortDateOnly(ts1)).length).toBeGreaterThan(0);
    expect(screen.getByText(formatTimeOnly(ts1))).toBeInTheDocument();
    expect(screen.queryByText('No recent activity')).not.toBeInTheDocument();
  });

  it('renders correct number of rows', () => {
    const txns = [
      makeTxn({ id: 'a' }),
      makeTxn({ id: 'b', itemSummary: 'Item B' }),
      makeTxn({ id: 'c', itemSummary: 'Item C' }),
    ];
    renderQueueHistory(txns);

    expect(screen.getByText('Shell SH-001')).toBeInTheDocument();
    expect(screen.getByText('Item B')).toBeInTheDocument();
    expect(screen.getByText('Item C')).toBeInTheDocument();
  });
});
