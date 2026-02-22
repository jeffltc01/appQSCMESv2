export type BarcodePrefix =
  | 'SC'
  | 'D'
  | 'L'
  | 'FD'
  | 'INP'
  | 'KC'
  | 'TS'
  | 'S'
  | 'CL'
  | 'C'
  | 'O'
  | 'FLT'
  | 'NOSHELL';

export interface ParsedBarcode {
  prefix: BarcodePrefix;
  value: string;
  raw: string;
}

export interface ShellLabelParsed {
  serialNumber: string;
  labelSuffix: 'L1' | 'L2' | null;
}

export interface FullDefectParsed {
  defectCode: string;
  characteristic: string;
  location: string;
}

export function parseBarcode(raw: string): ParsedBarcode | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;

  const semicolonIndex = trimmed.indexOf(';');
  if (semicolonIndex === -1) return null;

  const prefix = trimmed.substring(0, semicolonIndex).toUpperCase();
  const value = trimmed.substring(semicolonIndex + 1);

  const validPrefixes: BarcodePrefix[] = [
    'SC', 'D', 'L', 'FD', 'INP', 'KC', 'TS', 'S', 'CL', 'C', 'O', 'FLT', 'NOSHELL',
  ];

  if (!validPrefixes.includes(prefix as BarcodePrefix)) return null;

  return { prefix: prefix as BarcodePrefix, value, raw: trimmed };
}

export function parseShellLabel(value: string): ShellLabelParsed {
  if (value.endsWith('/L1')) {
    return { serialNumber: value.slice(0, -3), labelSuffix: 'L1' };
  }
  if (value.endsWith('/L2')) {
    return { serialNumber: value.slice(0, -3), labelSuffix: 'L2' };
  }
  return { serialNumber: value, labelSuffix: null };
}

export function parseFullDefect(value: string): FullDefectParsed | null {
  const parts = value.split('-');
  if (parts.length !== 3) return null;
  return {
    defectCode: parts[0],
    characteristic: parts[1],
    location: parts[2],
  };
}
