import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AnnotationDialog } from './AnnotationDialog';

vi.mock('../../api/endpoints', () => ({
  adminAnnotationTypeApi: {
    getAll: vi.fn(),
  },
  logViewerApi: {
    createAnnotation: vi.fn(),
  },
}));

import { adminAnnotationTypeApi, logViewerApi } from '../../api/endpoints';

const MOCK_TYPES = [
  { id: 'type-defect', name: 'Defect', abbreviation: 'D', requiresResolution: true, operatorCanCreate: true, displayColor: '#ff0000' },
  { id: 'type-correction', name: 'Correction Needed', abbreviation: 'C', requiresResolution: true, operatorCanCreate: true, displayColor: '#ffff00' },
  { id: 'type-note', name: 'Note', abbreviation: 'N', requiresResolution: false, operatorCanCreate: false, displayColor: '#cc00ff' },
];

const defaultProps = {
  open: true,
  onClose: vi.fn(),
  productionRecordId: 'pr-1',
  serialOrIdentifier: 'SH-TEST-001',
  operatorId: 'op-1',
  onCreated: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(adminAnnotationTypeApi.getAll).mockResolvedValue(MOCK_TYPES);
  vi.mocked(logViewerApi.createAnnotation).mockResolvedValue({} as never);
});

describe('AnnotationDialog', () => {
  it('shows dialog title and record identifier', async () => {
    render(<AnnotationDialog {...defaultProps} />);
    expect(screen.getByRole('heading', { name: 'Create Annotation' })).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText(/SH-TEST-001/)).toBeInTheDocument();
    });
  });

  it('loads operator-allowed and canned-message-referenced annotation types', async () => {
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => {
      expect(adminAnnotationTypeApi.getAll).toHaveBeenCalled();
    });
    const dropdown = screen.getByRole('combobox');
    expect(dropdown).toHaveTextContent('Correction Needed');
  });

  it('defaults to Correction Needed type', async () => {
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => {
      const dropdown = screen.getByRole('combobox');
      expect(dropdown).toHaveTextContent('Correction Needed');
    });
  });

  it('renders canned message buttons', async () => {
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => {
      expect(screen.getByText('Data entry error')).toBeInTheDocument();
      expect(screen.getByText('Defective material identified')).toBeInTheDocument();
      expect(screen.getByText('Wrong Shell or Tank scanned')).toBeInTheDocument();
      expect(screen.getByText('See me for note')).toBeInTheDocument();
    });
  });

  it('sets notes to canned message on click', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Wrong Shell or Tank scanned'));
    await user.click(screen.getByText('Wrong Shell or Tank scanned'));

    const textarea = screen.getByPlaceholderText(/type a message/i);
    expect(textarea).toHaveValue('Wrong Shell or Tank scanned');
  });

  it('replaces notes when current notes are a canned message', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Data entry error'));

    await user.click(screen.getByText('Data entry error'));
    const textarea = screen.getByPlaceholderText(/type a message/i);
    expect(textarea).toHaveValue('Data entry error');

    await user.click(screen.getByText('See me for note'));
    expect(textarea).toHaveValue('See me for note');
  });

  it('appends canned message when notes contain custom text', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Data entry error'));

    const textarea = screen.getByPlaceholderText(/type a message/i);
    fireEvent.change(textarea, { target: { value: 'My custom remark' } });
    expect(textarea).toHaveValue('My custom remark');

    await user.click(screen.getByText('Data entry error'));
    expect(textarea).toHaveValue('My custom remark\nData entry error');
  });

  it('switches type to Correction Needed when "Data entry error" is clicked', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Defective material identified'));

    await user.click(screen.getByText('Defective material identified'));
    expect(screen.getByRole('combobox')).toHaveTextContent('Defect');

    await user.click(screen.getByText('Data entry error'));
    expect(screen.getByRole('combobox')).toHaveTextContent('Correction Needed');
  });

  it('switches type to Correction Needed when "Wrong Shell or Tank scanned" is clicked', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('See me for note'));

    await user.click(screen.getByText('See me for note'));
    expect(screen.getByRole('combobox')).toHaveTextContent('Note');

    await user.click(screen.getByText('Wrong Shell or Tank scanned'));
    expect(screen.getByRole('combobox')).toHaveTextContent('Correction Needed');
  });

  it('switches type to Defect when "Defective material identified" is clicked', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Defective material identified'));
    await user.click(screen.getByText('Defective material identified'));

    const dropdown = screen.getByRole('combobox');
    expect(dropdown).toHaveTextContent('Defect');
  });

  it('switches type to Note when "See me for note" is clicked', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('See me for note'));
    await user.click(screen.getByText('See me for note'));

    const dropdown = screen.getByRole('combobox');
    expect(dropdown).toHaveTextContent('Note');
  });

  it('shows error when submitting without notes', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByRole('button', { name: 'Create Annotation' }));
    await user.click(screen.getByRole('button', { name: 'Create Annotation' }));
    expect(screen.getByText('Enter or select a message.')).toBeInTheDocument();
  });

  it('submits annotation and calls onCreated', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Data entry error'));

    await user.click(screen.getByText('Data entry error'));
    await user.click(screen.getByRole('button', { name: 'Create Annotation' }));

    await waitFor(() => {
      expect(logViewerApi.createAnnotation).toHaveBeenCalledWith({
        productionRecordId: 'pr-1',
        annotationTypeId: 'type-correction',
        notes: 'Data entry error',
        initiatedByUserId: 'op-1',
      });
    });
    expect(defaultProps.onCreated).toHaveBeenCalled();
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it('shows error when API call fails', async () => {
    vi.mocked(logViewerApi.createAnnotation).mockRejectedValue({ message: 'Production record not found.' });
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Data entry error'));

    await user.click(screen.getByText('Data entry error'));
    await user.click(screen.getByRole('button', { name: 'Create Annotation' }));

    await waitFor(() => {
      expect(screen.getByText('Production record not found.')).toBeInTheDocument();
    });
  });

  it('calls onClose when Cancel is clicked', async () => {
    const user = userEvent.setup();
    render(<AnnotationDialog {...defaultProps} />);
    await waitFor(() => screen.getByText('Cancel'));
    await user.click(screen.getByText('Cancel'));
    expect(defaultProps.onClose).toHaveBeenCalled();
  });
});
