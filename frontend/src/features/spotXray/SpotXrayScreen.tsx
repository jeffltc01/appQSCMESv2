import { useState, useCallback } from 'react';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import type { SpotXrayIncrementSummary } from '../../types/domain';
import { SpotXrayCreateView } from './SpotXrayCreateView';
import { SpotXrayResultsView } from './SpotXrayResultsView';

type View = 'create' | 'results';

export function SpotXrayScreen(props: WorkCenterProps) {
  const [view, setView] = useState<View>('create');
  const [createdIncrements, setCreatedIncrements] = useState<SpotXrayIncrementSummary[]>([]);

  const handleIncrementsCreated = useCallback((increments: SpotXrayIncrementSummary[]) => {
    setCreatedIncrements(increments);
    setView('results');
  }, []);

  const handleBackToCreate = useCallback(() => {
    setView('create');
    setCreatedIncrements([]);
  }, []);

  if (view === 'results' && createdIncrements.length > 0) {
    return (
      <SpotXrayResultsView
        incrementSummaries={createdIncrements}
        operatorId={props.operatorId}
        plantId={props.plantId}
        onBackToCreate={handleBackToCreate}
      />
    );
  }

  return (
    <SpotXrayCreateView
      workCenterId={props.workCenterId}
      productionLineId={props.productionLineId}
      operatorId={props.operatorId}
      onIncrementsCreated={handleIncrementsCreated}
    />
  );
}
