'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { Icon } from './icons';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { useInspections } from '../lib/inspection-store';

type Property = { id: string; name: string };
type Unit = { id: string; identifier: string };
type Template = { id: string; name: string };

export function TriageCreateForm() {
  const role = useCurrentUser().role;
  const canManageTemplates = role === 'Admin';
  const canCreateInspection = role === 'Admin' || role === 'Vistoriador';
  const router = useRouter();
  const { refresh } = useInspections();
  const [properties, setProperties] = useState<Property[]>([]);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [propertyId, setPropertyId] = useState('');
  const [unitId, setUnitId] = useState('');
  const [templateId, setTemplateId] = useState('');
  const [type, setType] = useState<'MoveIn' | 'MoveOut'>('MoveIn');
  const [scheduledAtLocal, setScheduledAtLocal] = useState('');
  const [templateName, setTemplateName] = useState('');
  const [roomName, setRoomName] = useState('');
  const [itemDescription, setItemDescription] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([
      apiRequest<Property[]>('/api/v1/properties'),
      apiRequest<Template[]>('/api/v1/checklist-templates'),
    ]).then(([propertyList, templateList]) => {
      setProperties(propertyList);
      setTemplates(templateList);
      setPropertyId(propertyList[0]?.id ?? '');
      setTemplateId(templateList[0]?.id ?? (canManageTemplates ? 'new' : ''));
    }).catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar o formulário.'));
  }, [canManageTemplates]);

  useEffect(() => {
    if (!propertyId) { setUnits([]); setUnitId(''); return; }
    let active = true;
    apiRequest<Unit[]>(`/api/v1/properties/${propertyId}/units`)
      .then((items) => { if (active) { setUnits(items); setUnitId(items[0]?.id ?? ''); } })
      .catch((cause: unknown) => { if (active) setError(cause instanceof Error ? cause.message : 'Não foi possível carregar as unidades.'); });
    return () => { active = false; };
  }, [propertyId]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!unitId) { setError('Cadastre uma unidade para este imóvel antes de criar a vistoria.'); return; }
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
          headers: { 'Idempotency-Key': crypto.randomUUID() },
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
        headers: { 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({ unitId, checklistTemplateId: selectedTemplateId, type, scheduledAtUtc }),
      });
      await refresh();
      router.push(`/vistorias/${created.inspectionId}`);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível criar a vistoria.');
    } finally {
      setSaving(false);
    }
  }

  if (!canCreateInspection) return <p>Seu perfil permite apenas consultar vistorias.</p>;

  return <form className="triage-form" onSubmit={handleSubmit}>
    <div className="triage-form-intro"><span className="triage-form-icon"><Icon name="clipboard" size={22} /></span><div><h2>Dados da vistoria</h2><p>Escolha uma unidade e um checklist da sua organização.</p></div></div>
    <div className="form-grid">
      <label className="form-field"><span>Imóvel</span><select value={propertyId} onChange={(event) => setPropertyId(event.target.value)} required>{properties.map((property) => <option key={property.id} value={property.id}>{property.name}</option>)}</select></label>
      <label className="form-field"><span>Unidade</span><select value={unitId} onChange={(event) => setUnitId(event.target.value)} required>{units.map((unit) => <option key={unit.id} value={unit.id}>{unit.identifier}</option>)}</select></label>
      <label className="form-field"><span>Tipo de vistoria</span><select value={type} onChange={(event) => setType(event.target.value as 'MoveIn' | 'MoveOut')}><option value="MoveIn">Entrada</option><option value="MoveOut">Saída</option></select></label>
      <label className="form-field"><span>Checklist base</span><select value={templateId} onChange={(event) => setTemplateId(event.target.value)} required>{templates.map((template) => <option key={template.id} value={template.id}>{template.name}</option>)}{canManageTemplates && <option value="new">Criar novo modelo</option>}</select></label>
      <label className="form-field form-field--full"><span>Data e hora (opcional)</span><input type="datetime-local" value={scheduledAtLocal} onChange={(event) => setScheduledAtLocal(event.target.value)} /></label>
      {templateId === 'new' && <><label className="form-field"><span>Nome do checklist</span><input value={templateName} onChange={(event) => setTemplateName(event.target.value)} maxLength={200} required /></label><label className="form-field"><span>Primeiro ambiente</span><input value={roomName} onChange={(event) => setRoomName(event.target.value)} maxLength={200} required /></label><label className="form-field form-field--full"><span>Primeiro item</span><input value={itemDescription} onChange={(event) => setItemDescription(event.target.value)} maxLength={1000} required /></label></>}
    </div>
    {!properties.length && <p>Cadastre um imóvel e uma unidade na página <Link href="/imoveis">Imóveis</Link>.</p>}
    {!templates.length && !canManageTemplates && <p>Peça a um administrador para cadastrar um modelo de checklist.</p>}
    {propertyId && !units.length && <p>O imóvel selecionado ainda não tem unidades.</p>}
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <div className="triage-form-actions"><Link className="button button--outline" href="/triagens">Cancelar</Link><button className="button button--primary" type="submit" disabled={saving || !unitId}><Icon name="plus" size={17} /> {saving ? 'Criando...' : 'Criar vistoria'}</button></div>
  </form>;
}
