import { useEffect, useState } from 'react';
import { isPhoneViewport } from '../auth/mobilePolicy.ts';

function getViewportWidth(): number {
  if (typeof window === 'undefined') return 9999;
  return window.innerWidth;
}

export function useIsPhoneViewport(): boolean {
  const [isPhone, setIsPhone] = useState(() => isPhoneViewport(getViewportWidth()));

  useEffect(() => {
    const onResize = () => {
      setIsPhone(isPhoneViewport(getViewportWidth()));
    };
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  return isPhone;
}
