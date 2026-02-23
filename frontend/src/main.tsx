import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { FluentProvider } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { qscTheme } from './theme/qscTheme.ts';
import { AuthProvider } from './auth/AuthContext.tsx';
import { App } from './App.tsx';
import { initializeTelemetry } from './telemetry/telemetryClient.ts';
import { RuntimeErrorBoundary } from './telemetry/RuntimeErrorBoundary.tsx';
import './global.css';

initializeTelemetry();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <FluentProvider theme={qscTheme} style={{ height: '100%' }}>
      <MemoryRouter>
        <AuthProvider>
          <RuntimeErrorBoundary>
            <App />
          </RuntimeErrorBoundary>
        </AuthProvider>
      </MemoryRouter>
    </FluentProvider>
  </StrictMode>,
);
