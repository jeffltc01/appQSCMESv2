import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

vi.stubGlobal('ResizeObserver', ResizeObserverMock);

if (!window.HTMLElement.prototype.scrollTo) {
  window.HTMLElement.prototype.scrollTo = () => {};
}

if (!('getBBox' in window.SVGElement.prototype)) {
  Object.defineProperty(window.SVGElement.prototype, 'getBBox', {
    value: () => ({
      x: 0,
      y: 0,
      width: 0,
      height: 0,
    }),
  });
}
