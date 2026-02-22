import { describe, it, expect } from 'vitest';
import { parseBarcode, parseShellLabel, parseFullDefect } from './barcode';

describe('parseBarcode', () => {
  it('parses SC prefix', () => {
    const result = parseBarcode('SC;000001/L1');
    expect(result).toEqual({ prefix: 'SC', value: '000001/L1', raw: 'SC;000001/L1' });
  });

  it('parses D prefix', () => {
    const result = parseBarcode('D;042');
    expect(result).toEqual({ prefix: 'D', value: '042', raw: 'D;042' });
  });

  it('parses L prefix', () => {
    const result = parseBarcode('L;003');
    expect(result).toEqual({ prefix: 'L', value: '003', raw: 'L;003' });
  });

  it('parses FD compound defect', () => {
    const result = parseBarcode('FD;042-007-003');
    expect(result).toEqual({ prefix: 'FD', value: '042-007-003', raw: 'FD;042-007-003' });
  });

  it('parses INP commands', () => {
    expect(parseBarcode('INP;1')).toEqual({ prefix: 'INP', value: '1', raw: 'INP;1' });
    expect(parseBarcode('INP;3')).toEqual({ prefix: 'INP', value: '3', raw: 'INP;3' });
    expect(parseBarcode('INP;4')).toEqual({ prefix: 'INP', value: '4', raw: 'INP;4' });
  });

  it('parses KC kanban card', () => {
    const result = parseBarcode('KC;03');
    expect(result).toEqual({ prefix: 'KC', value: '03', raw: 'KC;03' });
  });

  it('parses TS tank size', () => {
    const result = parseBarcode('TS;120');
    expect(result).toEqual({ prefix: 'TS', value: '120', raw: 'TS;120' });
  });

  it('parses S save', () => {
    const result = parseBarcode('S;1');
    expect(result).toEqual({ prefix: 'S', value: '1', raw: 'S;1' });
  });

  it('parses CL clear', () => {
    const result = parseBarcode('CL;1');
    expect(result).toEqual({ prefix: 'CL', value: '1', raw: 'CL;1' });
  });

  it('parses O override', () => {
    const result = parseBarcode('O;1');
    expect(result).toEqual({ prefix: 'O', value: '1', raw: 'O;1' });
  });

  it('parses FLT fault', () => {
    const result = parseBarcode('FLT;Button Stuck');
    expect(result).toEqual({ prefix: 'FLT', value: 'Button Stuck', raw: 'FLT;Button Stuck' });
  });

  it('parses NOSHELL', () => {
    const result = parseBarcode('NOSHELL;0');
    expect(result).toEqual({ prefix: 'NOSHELL', value: '0', raw: 'NOSHELL;0' });
  });

  it('parses C characteristic prefix', () => {
    const result = parseBarcode('C;001');
    expect(result).toEqual({ prefix: 'C', value: '001', raw: 'C;001' });
  });

  it('returns null for empty string', () => {
    expect(parseBarcode('')).toBeNull();
  });

  it('returns null for string without semicolon', () => {
    expect(parseBarcode('ABCDEF')).toBeNull();
  });

  it('returns null for unrecognized prefix', () => {
    expect(parseBarcode('UNKNOWN;123')).toBeNull();
  });

  it('handles case-insensitive prefixes', () => {
    const result = parseBarcode('sc;000001/L1');
    expect(result).toEqual({ prefix: 'SC', value: '000001/L1', raw: 'sc;000001/L1' });
  });

  it('trims whitespace', () => {
    const result = parseBarcode('  SC;000001  ');
    expect(result).toEqual({ prefix: 'SC', value: '000001', raw: 'SC;000001' });
  });
});

describe('parseShellLabel', () => {
  it('extracts serial and L1 suffix', () => {
    const result = parseShellLabel('000001/L1');
    expect(result).toEqual({ serialNumber: '000001', labelSuffix: 'L1' });
  });

  it('extracts serial and L2 suffix', () => {
    const result = parseShellLabel('000001/L2');
    expect(result).toEqual({ serialNumber: '000001', labelSuffix: 'L2' });
  });

  it('returns null suffix for no label', () => {
    const result = parseShellLabel('000001');
    expect(result).toEqual({ serialNumber: '000001', labelSuffix: null });
  });

  it('handles various serial number lengths', () => {
    const result = parseShellLabel('ABCD1234567/L1');
    expect(result).toEqual({ serialNumber: 'ABCD1234567', labelSuffix: 'L1' });
  });
});

describe('parseFullDefect', () => {
  it('parses three-part defect code', () => {
    const result = parseFullDefect('042-007-003');
    expect(result).toEqual({
      defectCode: '042',
      characteristic: '007',
      location: '003',
    });
  });

  it('returns null for wrong number of parts', () => {
    expect(parseFullDefect('042-007')).toBeNull();
    expect(parseFullDefect('042')).toBeNull();
    expect(parseFullDefect('042-007-003-999')).toBeNull();
  });
});
