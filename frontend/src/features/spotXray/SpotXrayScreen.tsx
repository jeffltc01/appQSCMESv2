import { useState, useCallback } from 'react';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import type { SpotXrayIncrementSummary } from '../../types/domain';
import { SpotXrayCreateView } from './SpotXrayCreateView';
import { SpotXrayResultsView } from './SpotXrayResultsView';

type View = 'create' | 'results';

export function SpotXrayScreen(props: WorkCenterProps) {
  const [view, setView] = useState<View>('create');
  const [activeIncrements, setActiveIncrements] = useState<SpotXrayIncrementSummary[]>([]);

  const handleIncrementsCreated = useCallback((increments: SpotXrayIncrementSummary[]) => {
    setActiveIncrements(increments);
    setView('results');
  }, []);

  const handleOpenDraft = useCallback((draft: SpotXrayIncrementSummary) => {
    setActiveIncrements([draft]);
    setView('results');
  }, []);

  const handleBackToCreate = useCallback(() => {
    setView('create');
    setActiveIncrements([]);
  }, []);

  if (view === 'results' && activeIncrements.length > 0) {
    return (
      <SpotXrayResultsView
        incrementSummaries={activeIncrements}
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
      onOpenDraft={handleOpenDraft}
    />
  );
}
