'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';
import { Icon } from './icons';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { useInspections } from '../lib/inspection-store';

type Property = { id: string; name: string };
type Unit = { id: string; identifier: string };
type Template = { id: string; name: string };

export function TriageCreateForm() {
  const requestKeys = useRef(new Map<string, string>());
  function requestKey(payload: string) {
    const key = requestKeys.current.get(payload) ?? crypto.randomUUID();
    requestKeys.current.set(payload, key);
    return key;
  }
  const role = useCurrentUser().role;
  const canManageTemplates = role === 'Admin';
  const canCreateInspection = role === 'Admin' || role === 'Vistoriador';
  const router = useRouter();
  const { refresh } = useInspections();
  const [properties, setProperties] = useState<Property[]>([]);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [locationMode, setLocationMode] = useState<'new' | 'existing'>('new');
  const [address, setAddress] = useState('');
  const [locationName, setLocationName] = useState('');
  const [unitIdentifier, setUnitIdentifier] = useState('');
  const [loaded, setLoaded] = useState(false);
  const [propertyId, setPropertyId] = useState('');
  const [unitId, setUnitId] = useState('');
  const [templateId, setTemplateId] = useState('');
  const [type, setType] = useState<'MoveIn' | 'MoveOut'>('MoveIn');
  const [scheduledAtLocal, setScheduledAtLocal] = useState('');
  const [templateName, setTemplateName] = useState('Checklist inicial');
  const [roomName, setRoomName] = useState('Ambiente geral');
  const [itemDescription, setItemDescription] = useState('Estado geral de conservação');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let active = true;
    Promise.all([
      apiRequest<Property[]>('/api/v1/properties'),
      apiRequest<Template[]>('/api/v1/checklist-templates'),
    ]).then(([propertyList, templateList]) => {
      if (!active) return;
      setLoaded(true);
      setProperties(propertyList);
      setTemplates(templateList);
      setPropertyId(propertyList[0]?.id ?? '');
      setTemplateId(templateList[0]?.id ?? 'basic');
    }).catch((cause: unknown) => { if (active) setError(cause instanceof Error ? cause.message : 'Não foi possível carregar o formulário.'); });
    return () => { active = false; };
  }, [canManageTemplates]);

  useEffect(() => {
    if (locationMode !== 'existing' || !propertyId) { setUnits([]); setUnitId(''); return; }
    setUnits([]);
    setUnitId('');
    let active = true;
    apiRequest<Unit[]>(`/api/v1/properties/${propertyId}/units`)
      .then((items) => { if (active) { setUnits(items); setUnitId(items[0]?.id ?? ''); } })
      .catch((cause: unknown) => { if (active) setError(cause instanceof Error ? cause.message : 'Não foi possível carregar as unidades.'); });
    return () => { active = false; };
  }, [propertyId, locationMode]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locationMode === 'existing' && !unitId) { setError('Cadastre uma unidade para este imóvel antes de criar a vistoria.'); return; }
    if (locationMode === 'new' && (!address.trim() || address.trim().length > (locationName.trim() ? 1000 : 200))) {
      setError('Informe um endereço válido. Para endereços com mais de 200 caracteres, informe também um nome para o imóvel.'); return;
    }
    if (scheduledAtLocal && (!Number.isFinite(Date.parse(scheduledAtLocal)) || Date.parse(scheduledAtLocal) <= Date.now())) {
      setError('Escolha uma data e hora futuras ou deixe o agendamento vazio.'); return;
    }
    setSaving(true);
    setError('');
    try {
      let selectedTemplateId = templateId;
      if (templateId === 'new') {
        if (!canManageTemplates) throw new Error('Somente administradores podem criar modelos de checklist.');
        if (!templateName.trim() || !roomName.trim() || !itemDescription.trim()) {
          throw new Error('Informe o nome do checklist, um ambiente e um item.');
        }
        const created = await apiRequest<{ checklistTemplateId: string }>('/api/v1/checklist-templates', {
          method: 'POST',
          headers: { 'Idempotency-Key': requestKey(JSON.stringify({ operation: 'template', templateName, roomName, itemDescription })) },
          body: JSON.stringify({ name: templateName, rooms: [{ name: roomName, position: 0, items: [{ description: itemDescription, position: 0 }] }] }),
        });
        selectedTemplateId = created.checklistTemplateId;
        setTemplates((current) => [...current, { id: selectedTemplateId, name: templateName }]);
        setTemplateId(selectedTemplateId);
      }
      if (!selectedTemplateId) throw new Error('Selecione ou crie um modelo de checklist.');
      const scheduledAtUtc = scheduledAtLocal ? new Date(scheduledAtLocal).toISOString() : null;
      const created = await apiRequest<{ inspectionId: string }>('/api/v1/inspections/from-template', {
        method: 'POST',
        headers: { 'Idempotency-Key': requestKey(JSON.stringify({ operation: 'inspection', locationMode, unitId, address, locationName, unitIdentifier, selectedTemplateId, type, scheduledAtUtc })) },
        body: JSON.stringify({ unitId: locationMode === 'existing' ? unitId : '00000000-0000-0000-0000-000000000000', location: locationMode === 'new' ? { address: address.trim(), name: locationName.trim(), unitIdentifier: unitIdentifier.trim() } : null, checklistTemplateId: selectedTemplateId === 'basic' ? '00000000-0000-0000-0000-000000000000' : selectedTemplateId, type, scheduledAtUtc }),
      });
      router.push(`/vistorias/${created.inspectionId}`);
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível criar a vistoria.');
    } finally {
      setSaving(false);
    }
  }

  if (!canCreateInspection) return <p>Seu perfil permite apenas consultar vistorias.</p>;

  return <form className="triage-form" onSubmit={handleSubmit}>
    <div className="triage-form-intro"><span className="triage-form-icon"><Icon name="clipboard" size={22} /></span><div><h2>Dados da vistoria</h2><p>Informe o endereço ou escolha um imóvel cadastrado. O agendamento é opcional.</p></div></div>
    <div className="form-grid">
      <label className="form-field form-field--full"><span>Local da vistoria</span><select value={locationMode} onChange={(event) => setLocationMode(event.target.value as 'new' | 'existing')}><option value="new">Informar endereço agora</option><option value="existing">Usar imóvel cadastrado</option></select></label>
      {locationMode === 'new' ? <>
        <label className="form-field form-field--full"><span>Endereço</span><input value={address} onChange={(event) => setAddress(event.target.value)} placeholder="Rua, número, bairro e cidade" maxLength={locationName.trim() ? 1000 : 200} required /></label>
        <label className="form-field"><span>Nome do imóvel (opcional)</span><input value={locationName} onChange={(event) => setLocationName(event.target.value)} placeholder="Usaremos o endereço se ficar vazio" maxLength={200} /></label>
        <label className="form-field"><span>Unidade / complemento (opcional)</span><input value={unitIdentifier} onChange={(event) => setUnitIdentifier(event.target.value)} placeholder="Ex.: apartamento 101" maxLength={100} /></label>
        <p className="form-field--full">O imóvel será salvo junto com a vistoria, sem cadastro separado.</p>
      </> : <>
      <label className="form-field"><span>Imóvel</span><select value={propertyId} onChange={(event) => setPropertyId(event.target.value)} required>{properties.map((property) => <option key={property.id} value={property.id}>{property.name}</option>)}</select></label>
      <label className="form-field"><span>Unidade</span><select value={unitId} onChange={(event) => setUnitId(event.target.value)} required>{units.map((unit) => <option key={unit.id} value={unit.id}>{unit.identifier}</option>)}</select></label>
      </>}
      <label className="form-field"><span>Tipo de vistoria</span><select value={type} onChange={(event) => setType(event.target.value as 'MoveIn' | 'MoveOut')}><option value="MoveIn">Entrada</option><option value="MoveOut">Saída</option></select></label>
      <label className="form-field"><span>Checklist base</span><select value={templateId} onChange={(event) => setTemplateId(event.target.value)} required><option value="basic">Checklist inicial (sem cadastrar modelo)</option>{templates.map((template) => <option key={template.id} value={template.id}>{template.name}</option>)}{canManageTemplates && <option value="new">Criar novo modelo</option>}</select></label>
      <label className="form-field form-field--full"><span>Data e hora (opcional)</span><input type="datetime-local" value={scheduledAtLocal} onChange={(event) => setScheduledAtLocal(event.target.value)} /></label>
      {templateId === 'new' && <><label className="form-field"><span>Nome do checklist</span><input value={templateName} onChange={(event) => setTemplateName(event.target.value)} maxLength={200} required /></label><label className="form-field"><span>Primeiro ambiente</span><input value={roomName} onChange={(event) => setRoomName(event.target.value)} maxLength={200} required /></label><label className="form-field form-field--full"><span>Primeiro item</span><input value={itemDescription} onChange={(event) => setItemDescription(event.target.value)} maxLength={1000} required /></label></>}
    </div>
    {locationMode === 'existing' && loaded && !properties.length && <p>Cadastre um imóvel e uma unidade na página <Link href="/imoveis">Imóveis</Link>.</p>}
    {templateId === 'basic' && <p>O checklist inicial tem um ambiente e um item de conservação. Adicione os ambientes e itens necessários durante a vistoria.</p>}
    {locationMode === 'existing' && propertyId && !units.length && <p>O imóvel selecionado ainda não tem unidades.</p>}
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <div className="triage-form-actions"><Link className="button button--outline" href="/triagens">Cancelar</Link><button className="button button--primary" type="submit" disabled={saving || !loaded || (locationMode === 'existing' && !unitId)}><Icon name="plus" size={17} /> {saving ? 'Criando...' : 'Criar vistoria'}</button></div>
  </form>;
}
