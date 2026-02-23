import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { TestCoverageScreen } from './TestCoverageScreen.tsx';
import { coverageApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api/endpoints.ts')>();
  return {
    ...actual,
    coverageApi: {
      getSummary: vi.fn(),
      getReportHtml: vi.fn(),
    },
  };
});

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };

const mockSummary = {
  generatedAt: '2026-02-23T14:15:00Z',
  backend: {
    lineRate: 82.5,
    branchRate: 71.3,
    linesValid: 2400,
    linesCovered: 1980,
    branchesValid: 500,
    branchesCovered: 356,
  },
  frontend: {
    lineRate: 91.2,
    branchRate: 85.0,
    linesValid: 3100,
    linesCovered: 2827,
    branchesValid: 800,
    branchesCovered: 680,
  },
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <TestCoverageScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('TestCoverageScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(coverageApi.getSummary).mockResolvedValue(mockSummary);
    vi.mocked(coverageApi.getReportHtml).mockResolvedValue('<html><body>Report</body></html>');
  });

  it('renders the page title', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Test Coverage')).toBeInTheDocument();
    });
  });

  it('displays backend summary card', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Backend (.NET)')).toBeInTheDocument();
      expect(screen.getByText('82.5%')).toBeInTheDocument();
    });
  });

  it('displays frontend summary card', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Frontend (React)')).toBeInTheDocument();
      expect(screen.getByText('91.2%')).toBeInTheDocument();
    });
  });

  it('shows tab bar with Backend and Frontend tabs', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Backend Report')).toBeInTheDocument();
      expect(screen.getByText('Frontend Report')).toBeInTheDocument();
    });
  });

  it('loads backend report by default', async () => {
    renderScreen();
    await waitFor(() => {
      expect(coverageApi.getReportHtml).toHaveBeenCalledWith('backend');
    });
  });

  it('switches to frontend report on tab click', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Frontend Report')).toBeInTheDocument();
    });

    await userEvent.click(screen.getByText('Frontend Report'));

    await waitFor(() => {
      expect(coverageApi.getReportHtml).toHaveBeenCalledWith('frontend');
    });
  });

  it('shows error state when summary fails to load', async () => {
    vi.mocked(coverageApi.getSummary).mockRejectedValue({ message: 'Not configured' });

    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Not configured')).toBeInTheDocument();
    });
  });

  it('displays last updated timestamp', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Last updated/)).toBeInTheDocument();
    });
  });
});
