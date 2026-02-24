import { useEffect, useRef } from 'react';
import JsBarcode from 'jsbarcode';

interface Code128BarcodeProps {
  value: string;
  className?: string;
}

export function Code128Barcode({ value, className }: Code128BarcodeProps) {
  const svgRef = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!svgRef.current) return;

    JsBarcode(svgRef.current, value, {
      format: 'CODE128',
      lineColor: '#000000',
      width: 2,
      height: 84,
      margin: 10,
      displayValue: false,
      background: '#ffffff',
    });
  }, [value]);

  return <svg ref={svgRef} className={className} aria-label={`barcode-${value}`} role="img" />;
}
