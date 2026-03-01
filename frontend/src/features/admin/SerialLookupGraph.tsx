import dagre from 'dagre';
import type { Edge, Node, NodeTypes } from 'reactflow';
import { Handle, MarkerType, Position } from 'reactflow';
import type { SerialNumberLookup, TraceabilityNode } from '../../types/domain.ts';
import { SerialLookupNodeCard } from './SerialLookupNodeCard.tsx';
import styles from './SerialNumberLookupScreen.module.css';

type TraceabilityGraphNode = {
  kind: 'traceability';
  traceNode: TraceabilityNode;
};

type ReassemblyOperationGraphNode = {
  kind: 'reassemblyOperation';
  testId: string;
  label: string;
  summary: string;
};

export type SerialLookupGraphNodeData = TraceabilityGraphNode | ReassemblyOperationGraphNode;
export type SerialLookupGraphNode = Node<SerialLookupGraphNodeData>;
export type SerialLookupGraphEdge = Edge;

const TRACEABILITY_NODE_WIDTH = 188;
const TRACEABILITY_NODE_HEIGHT = 184;
const OPERATION_NODE_WIDTH = 168;
const OPERATION_NODE_HEIGHT = 82;

function TraceabilityNodeView({ data }: { data: SerialLookupGraphNodeData }) {
  if (data.kind !== 'traceability') return null;
  return (
    <>
      <Handle type="target" position={Position.Left} className={styles.hiddenHandle} />
      <SerialLookupNodeCard node={data.traceNode} />
      <Handle type="source" position={Position.Right} className={styles.hiddenHandle} />
    </>
  );
}

function ReassemblyOperationNodeView({ data }: { data: SerialLookupGraphNodeData }) {
  if (data.kind !== 'reassemblyOperation') return null;
  return (
    <>
      <Handle type="target" position={Position.Left} className={styles.hiddenHandle} />
      <div className={styles.operationNode} data-testid={data.testId}>
        <div className={styles.operationTitle}>{data.label}</div>
        <div className={styles.operationSummary}>{data.summary}</div>
      </div>
      <Handle type="source" position={Position.Right} className={styles.hiddenHandle} />
    </>
  );
}

export const serialLookupNodeTypes: NodeTypes = {
  traceabilityNode: TraceabilityNodeView,
  reassemblyOperationNode: ReassemblyOperationNodeView,
};

type Relationship = {
  source: TraceabilityNode;
  target: TraceabilityNode;
};

function sanitizeIdPart(value: string): string {
  return value.replace(/[^a-zA-Z0-9_-]/g, '_');
}

function buildRelationships(treeNodes: TraceabilityNode[]) {
  const traceabilityNodes = new Map<string, TraceabilityNode>();
  const relationships: Relationship[] = [];
  const edgeKeys = new Set<string>();

  const walk = (
    node: TraceabilityNode,
    parent: TraceabilityNode | null,
    pathIds: Set<string>,
    includeLineageChildren: boolean,
  ) => {
    if (!traceabilityNodes.has(node.id)) {
      traceabilityNodes.set(node.id, node);
    }

    if (parent) {
      const key = `${parent.id}->${node.id}`;
      if (!edgeKeys.has(key)) {
        relationships.push({ source: parent, target: node });
        edgeKeys.add(key);
      }
    }

    if (pathIds.has(node.id)) return;
    const nextPathIds = new Set(pathIds);
    nextPathIds.add(node.id);

    if (!includeLineageChildren && node.nodeType === 'lineage') {
      return;
    }

    for (const child of node.children ?? []) {
      walk(child, node, nextPathIds, includeLineageChildren);
    }
  };

  return (includeLineageChildren: boolean) => {
    for (const root of treeNodes) {
      walk(root, null, new Set<string>(), includeLineageChildren);
    }
    return { traceabilityNodes, relationships };
  };
}

type GraphOptions = {
  includeLineageChildren?: boolean;
  expandedLineageNodeIds?: Set<string>;
};

function collectRelationships(treeNodes: TraceabilityNode[], options?: GraphOptions) {
  const includeLineageChildren = options?.includeLineageChildren ?? true;
  const expandedLineageNodeIds = options?.expandedLineageNodeIds ?? new Set<string>();
  const relationshipBuilder = buildRelationships(treeNodes);
  if (includeLineageChildren) {
    return relationshipBuilder(true);
  }

  const traceabilityNodes = new Map<string, TraceabilityNode>();
  const relationships: Relationship[] = [];
  const edgeKeys = new Set<string>();

  const walk = (node: TraceabilityNode, parent: TraceabilityNode | null, pathIds: Set<string>) => {
    if (!traceabilityNodes.has(node.id)) {
      traceabilityNodes.set(node.id, node);
    }

    if (parent) {
      const key = `${parent.id}->${node.id}`;
      if (!edgeKeys.has(key)) {
        relationships.push({ source: parent, target: node });
        edgeKeys.add(key);
      }
    }

    if (pathIds.has(node.id)) return;
    const nextPathIds = new Set(pathIds);
    nextPathIds.add(node.id);

    if (node.nodeType === 'lineage' && !expandedLineageNodeIds.has(node.id)) {
      return;
    }

    for (const child of node.children ?? []) {
      walk(child, node, nextPathIds);
    }
  };

  for (const root of treeNodes) {
    walk(root, null, new Set<string>());
  }

  return { traceabilityNodes, relationships };
}

export function hasLineageChildren(treeNodes: TraceabilityNode[]): boolean {
  const stack = [...treeNodes];
  while (stack.length > 0) {
    const current = stack.pop()!;
    if (current.nodeType === 'lineage' && (current.children?.length ?? 0) > 0) {
      return true;
    }
    for (const child of current.children ?? []) {
      stack.push(child);
    }
  }
  return false;
}

function isLineageRelation(relationship: Relationship): boolean {
  return relationship.source.nodeType === 'lineage' || relationship.target.nodeType === 'lineage';
}

function createTraceabilityRfNode(node: TraceabilityNode): SerialLookupGraphNode {
  return {
    id: node.id,
    type: 'traceabilityNode',
    data: {
      kind: 'traceability',
      traceNode: node,
    },
    position: { x: 0, y: 0 },
    sourcePosition: Position.Right,
    targetPosition: Position.Left,
  };
}

export function toReactFlowGraph(
  lookup: SerialNumberLookup,
  options?: GraphOptions,
): {
  nodes: SerialLookupGraphNode[];
  edges: SerialLookupGraphEdge[];
} {
  const { traceabilityNodes, relationships } = collectRelationships(lookup.treeNodes, options);
  const rfNodes: SerialLookupGraphNode[] = [];
  const rfEdges: SerialLookupGraphEdge[] = [];
  const existingNodeIds = new Set<string>();
  const existingEdgeIds = new Set<string>();

  for (const node of traceabilityNodes.values()) {
    const rfNode = createTraceabilityRfNode(node);
    rfNodes.push(rfNode);
    existingNodeIds.add(rfNode.id);
  }

  for (const relation of relationships) {
    const lineageRelation = isLineageRelation(relation);
    const baseEdgeStyle = lineageRelation ? { strokeDasharray: '6 4', stroke: '#495057' } : undefined;
    const componentNode = relation.target;
    const parentNode = relation.source;
    const defaultArrow = { type: MarkerType.ArrowClosed, color: '#495057' };

    if (!lineageRelation) {
      const edgeId = `edge-${componentNode.id}-${parentNode.id}`;
      if (existingEdgeIds.has(edgeId)) continue;
      existingEdgeIds.add(edgeId);
      rfEdges.push({
        id: edgeId,
        source: componentNode.id,
        target: parentNode.id,
        type: 'smoothstep',
        style: baseEdgeStyle,
        markerEnd: defaultArrow,
        data: { relationshipType: componentNode.nodeType },
      });
      continue;
    }

    const operationNodeId = `reassembly-op-${sanitizeIdPart(componentNode.id)}-${sanitizeIdPart(parentNode.id)}`;
    if (!existingNodeIds.has(operationNodeId)) {
      existingNodeIds.add(operationNodeId);
      rfNodes.push({
        id: operationNodeId,
        type: 'reassemblyOperationNode',
        data: {
          kind: 'reassemblyOperation',
          testId: operationNodeId,
          label: 'Reassembly',
          summary: `${componentNode.serial || componentNode.label} -> ${parentNode.serial || parentNode.label}`,
        },
        position: { x: 0, y: 0 },
        sourcePosition: Position.Right,
        targetPosition: Position.Left,
      });
    }

    const sourceToOpEdgeId = `edge-${componentNode.id}-${operationNodeId}`;
    if (!existingEdgeIds.has(sourceToOpEdgeId)) {
      existingEdgeIds.add(sourceToOpEdgeId);
      rfEdges.push({
        id: sourceToOpEdgeId,
        source: componentNode.id,
        target: operationNodeId,
        type: 'smoothstep',
        style: baseEdgeStyle,
        markerEnd: defaultArrow,
      });
    }

    const opToTargetEdgeId = `edge-${operationNodeId}-${parentNode.id}`;
    if (!existingEdgeIds.has(opToTargetEdgeId)) {
      existingEdgeIds.add(opToTargetEdgeId);
      rfEdges.push({
        id: opToTargetEdgeId,
        source: operationNodeId,
        target: parentNode.id,
        type: 'smoothstep',
        style: baseEdgeStyle,
        markerEnd: defaultArrow,
      });
    }
  }

  const dagreGraph = new dagre.graphlib.Graph();
  dagreGraph.setGraph({
    rankdir: 'LR',
    align: 'UL',
    ranker: 'tight-tree',
    nodesep: 70,
    ranksep: 96,
    edgesep: 24,
    marginx: 20,
    marginy: 20,
  });
  dagreGraph.setDefaultEdgeLabel(() => ({}));

  for (const node of rfNodes) {
    const width = node.data.kind === 'traceability' ? TRACEABILITY_NODE_WIDTH : OPERATION_NODE_WIDTH;
    const height = node.data.kind === 'traceability' ? TRACEABILITY_NODE_HEIGHT : OPERATION_NODE_HEIGHT;
    dagreGraph.setNode(node.id, { width, height });
  }

  for (const edge of rfEdges) {
    dagreGraph.setEdge(edge.source, edge.target);
  }

  dagre.layout(dagreGraph);

  const laidOutNodes = rfNodes.map((node) => {
    const dagreNode = dagreGraph.node(node.id);
    if (!dagreNode) return node;
    const width = node.data.kind === 'traceability' ? TRACEABILITY_NODE_WIDTH : OPERATION_NODE_WIDTH;
    const height = node.data.kind === 'traceability' ? TRACEABILITY_NODE_HEIGHT : OPERATION_NODE_HEIGHT;
    return {
      ...node,
      position: {
        x: dagreNode.x - width / 2,
        y: dagreNode.y - height / 2,
      },
    };
  });

  return {
    nodes: laidOutNodes,
    edges: rfEdges,
  };
}
