import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { TopBar } from './TopBar';

function renderTopBar(overrides = {}) {
  const defaults = {
    workCenterName: 'Long Seam',
    productionLineName: 'Main Line',
    assetName: 'Longseam A',
    operatorName: 'John D.',
    welders: [],
    onAddWelder: vi.fn(),
    onRemoveWelder: vi.fn(),
    externalInput: false,
    ...overrides,
  };
  return render(
    <FluentProvider theme={webLightTheme}>
      <TopBar {...defaults} />
    </FluentProvider>,
  );
}

describe('TopBar', () => {
  it('displays work center name, line, and asset', () => {
    renderTopBar();
    expect(screen.getByText('Long Seam')).toBeInTheDocument();
    expect(screen.getByText(/Main Line · Longseam A/)).toBeInTheDocument();
  });

  it('displays operator name', () => {
    renderTopBar();
    expect(screen.getByText('John D.')).toBeInTheDocument();
  });

  it('shows "No Welders" when welder list is empty', () => {
    renderTopBar();
    expect(screen.getByText('No Welders')).toBeInTheDocument();
  });

  it('displays welder names', () => {
    renderTopBar({
      welders: [
        { userId: 'w1', displayName: 'Alice', employeeNumber: '001' },
        { userId: 'w2', displayName: 'Bob', employeeNumber: '002' },
      ],
    });
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('calls onRemoveWelder when X is clicked', async () => {
    const user = userEvent.setup();
    const onRemoveWelder = vi.fn();
    renderTopBar({
      welders: [{ userId: 'w1', displayName: 'Alice', employeeNumber: '001' }],
      onRemoveWelder,
    });

    const removeBtn = screen.getByLabelText('Remove Alice');
    await user.click(removeBtn);
    expect(onRemoveWelder).toHaveBeenCalledWith('w1');
  });

  it('hides remove buttons in external input mode', () => {
    renderTopBar({
      welders: [{ userId: 'w1', displayName: 'Alice', employeeNumber: '001' }],
      externalInput: true,
    });
    expect(screen.queryByLabelText('Remove Alice')).not.toBeInTheDocument();
  });
});
