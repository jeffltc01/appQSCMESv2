import { useEffect, useState } from 'react';
import { Button, Dropdown, Input, Label, Option } from '@fluentui/react-components';
import { ncrApi, workflowApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { Ncr, NcrType, WorkflowEvent } from '../../types/domain.ts';

export function NcrScreen() {
  const { user } = useAuth();
  const [types, setTypes] = useState<NcrType[]>([]);
  const [list, setList] = useState<Ncr[]>([]);
  const [selected, setSelected] = useState<Ncr | null>(null);
  const [events, setEvents] = useState<WorkflowEvent[]>([]);
  const [ncrTypeId, setNcrTypeId] = useState('');
  const [problemDescription, setProblemDescription] = useState('');
  const [rejectComments, setRejectComments] = useState('');

  const load = () => ncrApi.getList(user?.plantCode).then(setList);
  useEffect(() => {
    void ncrApi.getNcrTypes(false).then(setTypes);
    void load();
  }, [user?.plantCode]);

  const open = (item: Ncr) => {
    setSelected(item);
    void workflowApi.getEvents(item.workflowInstanceId).then(setEvents);
  };

  return (
    <div style={{ padding: 16 }}>
      <h2>NCR</h2>
      <div style={{ display: 'grid', gap: 8, marginBottom: 16 }}>
        <Label>Create Direct NCR</Label>
        <Dropdown value={types.find((t) => t.id === ncrTypeId)?.name ?? ''} selectedOptions={[ncrTypeId]} onOptionSelect={(_, d) => setNcrTypeId(d.optionValue ?? '')}>
          {types.map((t) => <Option key={t.id} value={t.id}>{t.name}</Option>)}
        </Dropdown>
        <Input placeholder="Problem description" value={problemDescription} onChange={(_, d) => setProblemDescription(d.value)} />
        <Button appearance="primary" disabled={!user?.id || !user?.plantCode || !ncrTypeId || !problemDescription.trim()} onClick={() => {
          if (!user?.id || !user?.plantCode) return;
          void ncrApi.create({
            sourceType: 'DirectQuality',
            siteCode: user.plantCode,
            detectedByUserId: user.id,
            submitterUserId: user.id,
            coordinatorUserId: user.id,
            ncrTypeId,
            dateUtc: new Date().toISOString(),
            problemDescription,
            createdByUserId: user.id,
          }).then(load);
        }}>Create NCR</Button>
      </div>

      <div style={{ display: 'grid', gap: 8 }}>
        {list.map((ncr) => (
          <button key={ncr.id} style={{ textAlign: 'left', padding: 8 }} onClick={() => open(ncr)}>
            NCR-{ncr.ncrNumber} {ncr.problemDescription} [{ncr.currentStepCode}]
          </button>
        ))}
      </div>

      {selected && (
        <div style={{ marginTop: 16, display: 'grid', gap: 8 }}>
          <h3>NCR-{selected.ncrNumber}</h3>
          <Button onClick={() => {
            if (!user?.id) return;
            void ncrApi.addAttachment({
              ncrId: selected.id,
              fileName: `attachment-${Date.now()}.jpg`,
              contentType: 'image/jpeg',
              storagePath: `ncr/${selected.id}/mock-${Date.now()}.jpg`,
              uploadedByUserId: user.id,
            });
          }}>Add Image Attachment (metadata)</Button>
          <div style={{ display: 'flex', gap: 8 }}>
            <Button onClick={() => user?.id && ncrApi.submitStep({ ncrId: selected.id, actionCode: 'SubmitNcrStep', actorUserId: user.id }).then(load)}>Submit Step</Button>
            <Button onClick={() => user?.id && ncrApi.approveStep({ ncrId: selected.id, stepCode: selected.currentStepCode, actorUserId: user.id, comments: 'Approved' }).then(load)}>Approve</Button>
          </div>
          <Input placeholder="Reject comments" value={rejectComments} onChange={(_, d) => setRejectComments(d.value)} />
          <Button onClick={() => user?.id && rejectComments.trim() && ncrApi.rejectStep({ ncrId: selected.id, stepCode: selected.currentStepCode, actorUserId: user.id, comments: rejectComments }).then(load)}>Reject/Rework</Button>
          <Button onClick={() => user?.id && ncrApi.voidNcr({ ncrId: selected.id, reason: 'User requested void', actorUserId: user.id }).then(load)}>Void NCR</Button>
          <h4>Timeline</h4>
          {events.map((e) => <div key={e.id}>{new Date(e.eventAtUtc).toLocaleString()} - {e.eventType}</div>)}
        </div>
      )}
    </div>
  );
}
