import { useEffect, useState } from 'react';
import { Button, Dropdown, Input, Label, Option } from '@fluentui/react-components';
import { holdTagApi, ncrApi, workflowApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { HoldTag, Ncr, WorkflowEvent } from '../../types/domain.ts';

export function HoldTagsScreen() {
  const { user } = useAuth();
  const [list, setList] = useState<HoldTag[]>([]);
  const [events, setEvents] = useState<WorkflowEvent[]>([]);
  const [selected, setSelected] = useState<HoldTag | null>(null);
  const [disposition, setDisposition] = useState<'ReleaseAsIs' | 'Repair' | 'Scrap'>('ReleaseAsIs');
  const [notes, setNotes] = useState('');
  const [justification, setJustification] = useState('');
  const [linkedNcrId, setLinkedNcrId] = useState('');
  const [availableNcrs, setAvailableNcrs] = useState<Ncr[]>([]);
  const [voidReason, setVoidReason] = useState('');

  const load = () => holdTagApi.getList(user?.plantCode).then(setList);
  useEffect(() => { void load(); }, [user?.plantCode]);
  useEffect(() => { void ncrApi.getList(user?.plantCode).then(setAvailableNcrs); }, [user?.plantCode]);

  const loadEvents = (item: HoldTag) => {
    setSelected(item);
    void workflowApi.getEvents(item.workflowInstanceId).then(setEvents);
  };

  return (
    <div style={{ padding: 16 }}>
      <h2>Hold Tags</h2>
      <div style={{ display: 'grid', gap: 8 }}>
        {list.map((item) => (
          <button key={item.id} onClick={() => loadEvents(item)} style={{ textAlign: 'left', padding: 8 }}>
            #{item.holdTagNumber} - {item.problemDescription} [{item.businessStatus}]
          </button>
        ))}
      </div>

      {selected && (
        <div style={{ marginTop: 16, display: 'grid', gap: 8 }}>
          <h3>Selected Hold Tag #{selected.holdTagNumber}</h3>
          <Label>Disposition</Label>
          <Dropdown value={disposition} selectedOptions={[disposition]} onOptionSelect={(_, d) => setDisposition((d.optionValue as 'ReleaseAsIs' | 'Repair' | 'Scrap') ?? 'ReleaseAsIs')}>
            <Option value="ReleaseAsIs">ReleaseAsIs</Option>
            <Option value="Repair">Repair</Option>
            <Option value="Scrap">Scrap</Option>
          </Dropdown>
          <Input placeholder="Disposition notes" value={notes} onChange={(_, d) => setNotes(d.value)} />
          {disposition === 'ReleaseAsIs' && (
            <Input placeholder="Release justification" value={justification} onChange={(_, d) => setJustification(d.value)} />
          )}
          {(disposition === 'Repair' || disposition === 'Scrap') && (
            <>
              <Label>Linked NCR</Label>
              <Dropdown value={availableNcrs.find((x) => x.id === linkedNcrId)?.ncrNumber?.toString() ?? ''} selectedOptions={[linkedNcrId]} onOptionSelect={(_, d) => setLinkedNcrId(d.optionValue ?? '')}>
                {availableNcrs.map((n) => <Option key={n.id} value={n.id}>{`NCR-${n.ncrNumber}`}</Option>)}
              </Dropdown>
            </>
          )}
          <div style={{ display: 'flex', gap: 8 }}>
            <Button appearance="primary" disabled={!user?.id} onClick={() => {
              if (!user?.id) return;
              void holdTagApi.setDisposition({
                holdTagId: selected.id,
                disposition,
                dispositionNotes: notes || undefined,
                releaseJustification: justification || undefined,
                actorUserId: user.id,
              }).then(load);
            }}>Set Disposition</Button>
            <Button disabled={!user?.id || !linkedNcrId} onClick={() => {
              if (!user?.id || !linkedNcrId) return;
              void holdTagApi.linkNcr({ holdTagId: selected.id, linkedNcrId, actorUserId: user.id }).then(load);
            }}>Link NCR</Button>
            <Button disabled={!user?.id} onClick={() => {
              if (!user?.id) return;
              void holdTagApi.resolve({ holdTagId: selected.id, actorUserId: user.id }).then(load);
            }}>Resolve</Button>
          </div>
          <Input placeholder="Void reason" value={voidReason} onChange={(_, d) => setVoidReason(d.value)} />
          <Button disabled={!user?.id || !voidReason.trim()} onClick={() => {
            if (!user?.id) return;
            void holdTagApi.void({ holdTagId: selected.id, reason: voidReason, actorUserId: user.id }).then(load);
          }}>Void Hold Tag</Button>
          <h4>Timeline</h4>
          {events.map((e) => <div key={e.id}>{new Date(e.eventAtUtc).toLocaleString()} - {e.eventType}</div>)}
        </div>
      )}
    </div>
  );
}
