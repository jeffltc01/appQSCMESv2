export interface DemoScanAction {
  id: string;
  label: string;
  barcodeRaw: string;
}

const ACTIONS_BY_DATA_ENTRY_TYPE: Record<string, DemoScanAction[]> = {
  Rolls: [
    { id: 'rolls-advance', label: 'Advance Queue', barcodeRaw: 'INP;2' },
    { id: 'rolls-yes', label: 'Yes', barcodeRaw: 'INP;3' },
    { id: 'rolls-no', label: 'No', barcodeRaw: 'INP;4' },
  ],
  'Barcode-LongSeam': [],
  'Barcode-LongSeamInsp': [
    { id: 'lsi-save', label: 'Save', barcodeRaw: 'S;1' },
    { id: 'lsi-clear', label: 'Clear', barcodeRaw: 'CL;1' },
  ],
};

export function getDemoActionsForDataEntryType(dataEntryType: string): DemoScanAction[] {
  return ACTIONS_BY_DATA_ENTRY_TYPE[dataEntryType] ?? [];
}
