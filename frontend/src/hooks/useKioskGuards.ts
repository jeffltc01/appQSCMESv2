import { useEffect } from 'react';

function shouldBlockShortcut(event: KeyboardEvent): boolean {
  if (event.key === 'F5') return true;
  if (event.altKey && (event.key === 'ArrowLeft' || event.key === 'ArrowRight')) return true;

  if (!event.ctrlKey && !event.metaKey) return false;

  const key = event.key.toLowerCase();
  return key === 'r' || key === 'w' || key === 't' || key === 'n' || key === 'l';
}

export function useKioskGuards(enabled: boolean): void {
  useEffect(() => {
    if (!enabled) {
      document.documentElement.classList.remove('kiosk-mode');
      document.body.classList.remove('kiosk-mode');
      return;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (shouldBlockShortcut(event)) {
        event.preventDefault();
        event.stopPropagation();
      }
    };

    const preventDefault = (event: Event) => {
      event.preventDefault();
    };

    const onTouchMove = (event: TouchEvent) => {
      if (event.touches.length > 1) {
        event.preventDefault();
      }
    };

    document.documentElement.classList.add('kiosk-mode');
    document.body.classList.add('kiosk-mode');

    window.addEventListener('keydown', onKeyDown, { capture: true });
    window.addEventListener('contextmenu', preventDefault, { capture: true });
    window.addEventListener('dragstart', preventDefault, { capture: true });
    window.addEventListener('touchmove', onTouchMove, { passive: false, capture: true });

    return () => {
      document.documentElement.classList.remove('kiosk-mode');
      document.body.classList.remove('kiosk-mode');
      window.removeEventListener('keydown', onKeyDown, { capture: true });
      window.removeEventListener('contextmenu', preventDefault, { capture: true });
      window.removeEventListener('dragstart', preventDefault, { capture: true });
      window.removeEventListener('touchmove', onTouchMove, { capture: true });
    };
  }, [enabled]);
}
