import { useRef, useEffect, useCallback } from 'react';
import { parseBarcode, type ParsedBarcode } from '../types/barcode.ts';

interface UseBarcodeOptions {
  enabled: boolean;
  onScan: (barcode: ParsedBarcode | null, raw: string) => void;
}

export function useBarcode({ enabled, onScan }: UseBarcodeOptions) {
  const inputRef = useRef<HTMLInputElement>(null);

  const focusInput = useCallback(() => {
    if (inputRef.current && enabled) {
      inputRef.current.focus();
    }
  }, [enabled]);

  useEffect(() => {
    if (enabled) {
      focusInput();
      const interval = setInterval(focusInput, 500);
      return () => clearInterval(interval);
    }
  }, [enabled, focusInput]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        const raw = inputRef.current?.value ?? '';
        if (raw.trim()) {
          const parsed = parseBarcode(raw);
          onScan(parsed, raw);
        }
        if (inputRef.current) {
          inputRef.current.value = '';
        }
      }
    },
    [onScan],
  );

  return { inputRef, handleKeyDown };
}
