import { useEffect, useState } from 'react';
import { Button, Dropdown, Input, Label, Option, Switch } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { ncrApi, workflowApi } from '../../api/endpoints.ts';
import type { NcrType, WorkflowDefinition } from '../../types/domain.ts';

export function NcrTypesMaintenanceScreen() {
  const [types, setTypes] = useState<NcrType[]>([]);
  const [workflows, setWorkflows] = useState<WorkflowDefinition[]>([]);
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isVendorRelated, setIsVendorRelated] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const [workflowDefinitionId, setWorkflowDefinitionId] = useState('');

  useEffect(() => {
    void ncrApi.getNcrTypes(true).then(setTypes);
    void workflowApi.getDefinitions('Ncr').then(setWorkflows);
  }, []);

  return (
    <AdminLayout title="NCR Type Maintenance">
      <div style={{ display: 'grid', gap: 8 }}>
        <Label>Code</Label>
        <Input value={code} onChange={(_, d) => setCode(d.value)} />
        <Label>Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Description</Label>
        <Input value={description} onChange={(_, d) => setDescription(d.value)} />
        <Switch label="Vendor Related" checked={isVendorRelated} onChange={(_, d) => setIsVendorRelated(d.checked)} />
        <Switch label="Active" checked={isActive} onChange={(_, d) => setIsActive(d.checked)} />
        <Label>Mapped Workflow Definition</Label>
        <Dropdown value={workflows.find((x) => x.id === workflowDefinitionId)?.id ?? ''} selectedOptions={[workflowDefinitionId]} onOptionSelect={(_, d) => setWorkflowDefinitionId(d.optionValue ?? '')}>
          {workflows.map((w) => <Option key={w.id} value={w.id}>{`${w.workflowType} v${w.version}`}</Option>)}
        </Dropdown>
        <Button appearance="primary" disabled={!code || !name || !workflowDefinitionId} onClick={() => {
          void ncrApi.upsertNcrType({
            code,
            name,
            description,
            isVendorRelated,
            isActive,
            workflowDefinitionId,
          }).then(() => ncrApi.getNcrTypes(true).then(setTypes));
        }}>Save NCR Type</Button>
      </div>
      <div style={{ marginTop: 16 }}>
        {types.map((t) => (
          <div key={t.id} style={{ borderBottom: '1px solid #ddd', padding: '8px 0' }}>
            <strong>{t.code}</strong> - {t.name} ({t.isVendorRelated ? 'Vendor' : 'Standard'}) {t.isActive ? '' : '[Inactive]'}
          </div>
        ))}
      </div>
    </AdminLayout>
  );
}
