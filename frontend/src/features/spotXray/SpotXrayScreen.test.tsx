import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SpotXrayScreen } from './SpotXrayScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-spot', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1', welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

describe('SpotXrayScreen', () => {
  it('renders placeholder message', () => {
    const props = createProps();
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...props} /></FluentProvider>);
    expect(screen.getByText(/spot x-ray/i)).toBeInTheDocument();
    expect(screen.getByText(/specification pending/i)).toBeInTheDocument();
  });

});
