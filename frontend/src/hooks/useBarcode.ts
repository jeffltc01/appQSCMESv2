import { useRef, useEffect, useCallback, useState } from 'react';
import { parseBarcode, type ParsedBarcode } from '../types/barcode.ts';

interface UseBarcodeOptions {
  enabled: boolean;
  onScan: (barcode: ParsedBarcode | null, raw: string) => void;
}

const POLL_MS = 300;
const GRACE_MS = 500;

export function useBarcode({ enabled, onScan }: UseBarcodeOptions) {
  const inputRef = useRef<HTMLInputElement>(null);
  const onScanRef = useRef(onScan);
  onScanRef.current = onScan;

  const [focusLost, setFocusLost] = useState(false);
  const lostSinceRef = useRef<number | null>(null);

  useEffect(() => {
    if (!enabled) {
      setFocusLost(false);
      lostSinceRef.current = null;
      return;
    }

    const input = inputRef.current;
    if (!input) return;

    input.focus();

    const reclaim = () => {
      if (document.activeElement !== input) {
        input.focus();
      }
    };

    const checkFocus = () => {
      if (document.activeElement !== input) {
        input.focus();
      }
      const stillLost = document.activeElement !== input;
      if (stillLost) {
        if (lostSinceRef.current == null) lostSinceRef.current = Date.now();
        if (Date.now() - lostSinceRef.current >= GRACE_MS) setFocusLost(true);
      } else {
        lostSinceRef.current = null;
        setFocusLost(false);
      }
    };

    const interval = setInterval(checkFocus, POLL_MS);

    document.addEventListener('focusin', reclaim, true);
    document.addEventListener('click', reclaim, true);
    window.addEventListener('focus', reclaim);

    return () => {
      clearInterval(interval);
      document.removeEventListener('focusin', reclaim, true);
      document.removeEventListener('click', reclaim, true);
      window.removeEventListener('focus', reclaim);
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

  return { inputRef, handleKeyDown, focusLost };
}
