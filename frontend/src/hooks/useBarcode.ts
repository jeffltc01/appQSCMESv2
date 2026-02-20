import { useRef, useEffect, useCallback } from 'react';
import { parseBarcode, type ParsedBarcode } from '../types/barcode.ts';

interface UseBarcodeOptions {
  enabled: boolean;
  onScan: (barcode: ParsedBarcode | null, raw: string) => void;
}

export function useBarcode({ enabled, onScan }: UseBarcodeOptions) {
  const inputRef = useRef<HTMLInputElement>(null);
  const onScanRef = useRef(onScan);
  onScanRef.current = onScan;

  useEffect(() => {
    if (!enabled) return;

    const input = inputRef.current;
    if (!input) return;

    input.focus();

    const reclaim = () => {
      if (document.activeElement !== input) {
        input.focus();
      }
    };

    const interval = setInterval(reclaim, 100);

    document.addEventListener('focusin', reclaim, true);

    return () => {
      clearInterval(interval);
      document.removeEventListener('focusin', reclaim, true);
    };
  }, [enabled]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        const raw = inputRef.current?.value ?? '';
        if (inputRef.current) {
          inputRef.current.value = '';
        }
        if (raw.trim()) {
          const parsed = parseBarcode(raw);
          onScanRef.current(parsed, raw);
        }
      }
    },
    [],
  );

  return { inputRef, handleKeyDown };
}
