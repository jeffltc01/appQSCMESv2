import {
  CheckmarkCircleFilled,
  DismissCircleFilled,
  SubtractCircleFilled,
} from '@fluentui/react-icons';
import type { ManufacturingEvent, TraceabilityNode } from '../../types/domain.ts';
import styles from './SerialNumberLookupScreen.module.css';

export const NODE_TYPE_COLORS: Record<string, { bg: string; label: string }> = {
  sellable: { bg: '#28a745', label: 'Finished SN' },
  assembled: { bg: '#606ca3', label: 'Fitup' },
  shell: { bg: '#e41e2f', label: 'Shells' },
  plate: { bg: '#ffc107', label: 'Plate' },
  leftHead: { bg: '#ba68c8', label: 'Heads' },
  rightHead: { bg: '#ba68c8', label: 'Heads' },
  valve: { bg: '#17a2b8', label: 'Valves' },
  nameplate: { bg: '#ff8c00', label: 'Nameplate' },
  lineage: { bg: '#6c757d', label: 'Lineage' },
};

function getCardColorClass(nodeType: string): string {
  switch (nodeType) {
    case 'sellable':
      return styles.cardSellable;
    case 'assembled':
      return styles.cardAssembled;
    case 'shell':
      return styles.cardShell;
    case 'plate':
      return styles.cardPlate;
    case 'leftHead':
    case 'rightHead':
      return styles.cardHead;
    case 'valve':
      return styles.cardValve;
    case 'nameplate':
      return styles.cardNameplate;
    case 'lineage':
      return styles.cardDefault;
    default:
      return styles.cardDefault;
  }
}

export function getNodeTypeLabel(nodeType: string): string {
  return NODE_TYPE_COLORS[nodeType]?.label ?? nodeType;
}

type GateStatus = 'pass' | 'fail' | 'none';

function deriveGateStatus(node: TraceabilityNode): GateStatus | null {
  const { nodeType } = node;
  if (nodeType !== 'shell' && nodeType !== 'assembled' && nodeType !== 'sellable') return null;

  const events = node.events ?? [];

  const matches = (e: ManufacturingEvent, ...keywords: string[]) => {
    const source = `${e.type} ${e.workCenterName}`.toLowerCase();
    return keywords.some((keyword) => source.includes(keyword));
  };

  let gateEvents: ManufacturingEvent[];
  if (nodeType === 'shell') {
    gateEvents = events.filter(
      (e) =>
        e.inspectionResult != null &&
        !matches(e, 'queue') &&
        (matches(e, 'rt', 'realtime', 'real-time') || (matches(e, 'x-ray', 'xray') && !matches(e, 'spot'))),
    );
  } else if (nodeType === 'assembled') {
    gateEvents = events.filter((e) => e.inspectionResult != null && matches(e, 'spot'));
  } else {
    gateEvents = events.filter((e) => e.inspectionResult != null && matches(e, 'hydro'));
  }

  if (gateEvents.length === 0) return 'none';

  const hasReject = gateEvents.some((e) => {
    const result = e.inspectionResult?.toLowerCase() ?? '';
    return result.includes('reject') || result.includes('fail') || result === 'nogo';
  });
  return hasReject ? 'fail' : 'pass';
}

function formatCardTitle(node: TraceabilityNode): string {
  const serial = node.serial || node.label;
  if (node.nodeType === 'assembled' && node.childSerials && node.childSerials.length > 0) {
    return `${serial} (${node.childSerials.join(', ')})`;
  }
  return serial;
}

export function SerialLookupNodeCard({ node }: { node: TraceabilityNode }) {
  const colorClass = getCardColorClass(node.nodeType);
  const badgeColor = NODE_TYPE_COLORS[node.nodeType]?.bg;
  const gateStatus = deriveGateStatus(node);
  const defects = node.defectCount ?? 0;
  const annotations = node.annotationCount ?? 0;

  const cardClasses = [styles.heroCard, colorClass].filter(Boolean).join(' ');

  return (
    <div className={cardClasses} data-testid={`hero-card-${node.id}`}>
      {gateStatus != null && (
        <span className={styles.gateIcon} data-testid={`gate-${node.id}`}>
          {gateStatus === 'pass' ? (
            <CheckmarkCircleFilled className={styles.gatePass} />
          ) : gateStatus === 'fail' ? (
            <DismissCircleFilled className={styles.gateFail} />
          ) : (
            <SubtractCircleFilled className={styles.gateNone} />
          )}
        </span>
      )}
      <div
        className={styles.cardTypeBadge}
        style={{ background: badgeColor ? `${badgeColor}18` : undefined, color: badgeColor }}
      >
        {getNodeTypeLabel(node.nodeType)}
      </div>
      <div className={styles.cardSerial} title={formatCardTitle(node)}>
        {formatCardTitle(node)}
      </div>
      <div className={styles.cardInfo}>
        {node.tankSize != null && <span title={`${node.tankSize} gal`}>{node.tankSize} gal</span>}
        {node.heatNumber && <span title={`Heat: ${node.heatNumber}`}>Heat: {node.heatNumber}</span>}
        {node.coilNumber && <span title={`Coil: ${node.coilNumber}`}>Coil: {node.coilNumber}</span>}
        {node.lotNumber && <span title={`Lot: ${node.lotNumber}`}>Lot: {node.lotNumber}</span>}
      </div>
      <div className={styles.cardFooter}>
        <div className={styles.cardStats}>
          <span className={styles.statItem} title="Defects">
            <span className={styles.statLabel}>Defects</span>
            <span className={defects > 0 ? styles.statBad : styles.statGood}>{defects}</span>
          </span>
          <span className={styles.statItem} title="Annotations">
            <span className={styles.statLabel}>Notes</span>
            <span className={styles.statNeutral}>{annotations}</span>
          </span>
        </div>
      </div>
    </div>
  );
}
