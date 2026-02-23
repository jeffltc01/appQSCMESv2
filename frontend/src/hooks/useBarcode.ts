import { useRef, useEffect, useCallback, useState } from 'react';
import { parseBarcode, type ParsedBarcode } from '../types/barcode.ts';
import { reportException, reportTelemetry } from '../telemetry/telemetryClient.ts';

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
  const focusLossLoggedRef = useRef(false);

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
        if (Date.now() - lostSinceRef.current >= GRACE_MS) {
          setFocusLost(true);
          if (!focusLossLoggedRef.current) {
            focusLossLoggedRef.current = true;
            reportTelemetry({
              category: 'scanner_error',
              source: 'barcode_focus_lost',
              severity: 'warning',
              isReactRuntimeOverlayCandidate: false,
              message: 'Scanner input focus was lost',
            });
          }
        }
      } else {
        lostSinceRef.current = null;
        setFocusLost(false);
        focusLossLoggedRef.current = false;
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
          try {
            const parsed = parseBarcode(raw);
            onScanRef.current(parsed, raw);
          } catch (error) {
            reportException(error, {
              category: 'scanner_error',
              source: 'barcode_scan_handler',
              severity: 'error',
              isReactRuntimeOverlayCandidate: false,
              message: 'Scanner parse/handler failed',
              metadataJson: JSON.stringify({ raw }),
            });
          }
        }
      }
    },
    [],
  );

  return { inputRef, handleKeyDown, focusLost };
}
