import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { FluentProvider } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { qscTheme } from './theme/qscTheme.ts';
import { AuthProvider } from './auth/AuthContext.tsx';
import { App } from './App.tsx';
import './global.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <FluentProvider theme={qscTheme} style={{ height: '100%' }}>
      <MemoryRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </MemoryRouter>
    </FluentProvider>
  </StrictMode>,
);
