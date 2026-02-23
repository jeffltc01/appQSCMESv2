import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { useBarcode } from './useBarcode';

vi.mock('../types/barcode.ts', () => ({
  parseBarcode: vi.fn((raw: string) => ({ type: 'serial', value: raw })),
}));

import { parseBarcode } from '../types/barcode.ts';

interface UseBarcodeOptions {
  enabled: boolean;
  onScan: (barcode: unknown, raw: string) => void;
}

function TestComponent({ enabled, onScan }: UseBarcodeOptions) {
  const { inputRef, handleKeyDown, focusLost } = useBarcode({ enabled, onScan });
  return React.createElement('div', null,
    React.createElement('input', {
      ref: inputRef,
      onKeyDown: handleKeyDown,
      'data-testid': 'barcode-input',
    }),
    React.createElement('span', { 'data-testid': 'focus-lost' }, String(focusLost)),
  );
}

describe('useBarcode', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns inputRef, handleKeyDown, and focusLost', () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useBarcode({ enabled: false, onScan }));

    expect(result.current.inputRef).toBeDefined();
    expect(typeof result.current.handleKeyDown).toBe('function');
    expect(result.current.focusLost).toBe(false);
  });

  it('handleKeyDown with Enter triggers onScan with parsed barcode', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;
    input.value = 'ABC123';

    fireEvent.keyDown(input, { key: 'Enter' });

    expect(parseBarcode).toHaveBeenCalledWith('ABC123');
    expect(onScan).toHaveBeenCalledWith({ type: 'serial', value: 'ABC123' }, 'ABC123');
    expect(input.value).toBe('');
  });

  it('handleKeyDown with non-Enter key does nothing', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;
    input.value = 'ABC123';

    fireEvent.keyDown(input, { key: 'a' });

    expect(onScan).not.toHaveBeenCalled();
    expect(input.value).toBe('ABC123');
  });

  it('empty input value on Enter does not trigger onScan', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;
    input.value = '';

    fireEvent.keyDown(input, { key: 'Enter' });

    expect(onScan).not.toHaveBeenCalled();
  });

  it('whitespace-only input on Enter does not trigger onScan', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;
    input.value = '   ';

    fireEvent.keyDown(input, { key: 'Enter' });

    expect(onScan).not.toHaveBeenCalled();
  });

  it('when disabled, cleanup removes event listeners', () => {
    vi.useRealTimers();
    const onScan = vi.fn();
    const removeSpy = vi.spyOn(document, 'removeEventListener');

    const { unmount } = render(React.createElement(TestComponent, { enabled: true, onScan }));
    unmount();

    const removedEvents = removeSpy.mock.calls.map((c) => c[0]);
    expect(removedEvents).toContain('focusin');
    expect(removedEvents).toContain('click');

    removeSpy.mockRestore();
  });

  it('focusLost is false when input has focus', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    act(() => { vi.advanceTimersByTime(1000); });

    expect(screen.getByTestId('focus-lost').textContent).toBe('false');
  });

  it('focusLost becomes true when focus cannot be reclaimed after grace period', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;

    input.focus = vi.fn();

    const rival = document.createElement('button');
    document.body.appendChild(rival);
    act(() => { rival.focus(); });

    act(() => { vi.advanceTimersByTime(900); });

    expect(screen.getByTestId('focus-lost').textContent).toBe('true');

    document.body.removeChild(rival);
  });

  it('focusLost resets to false when focus is restored', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: true, onScan }));

    const input = screen.getByTestId('barcode-input') as HTMLInputElement;
    const originalFocus = HTMLInputElement.prototype.focus.bind(input);

    input.focus = vi.fn();

    const rival = document.createElement('button');
    document.body.appendChild(rival);
    act(() => { rival.focus(); });

    act(() => { vi.advanceTimersByTime(900); });
    expect(screen.getByTestId('focus-lost').textContent).toBe('true');

    input.focus = originalFocus;
    act(() => { vi.advanceTimersByTime(300); });
    expect(screen.getByTestId('focus-lost').textContent).toBe('false');

    document.body.removeChild(rival);
  });

  it('focusLost is false when not enabled', () => {
    const onScan = vi.fn();
    render(React.createElement(TestComponent, { enabled: false, onScan }));

    act(() => { vi.advanceTimersByTime(1000); });

    expect(screen.getByTestId('focus-lost').textContent).toBe('false');
  });
});
