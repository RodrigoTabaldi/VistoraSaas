'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { Icon } from './icons';
import { MetricCard, PageHeading, PanelHeader } from './dashboard-primitives';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';

type Property = { id: string; name: string; address: string };
type Unit = { id: string; propertyId: string; identifier: string };
type Inspection = { id: string; unitId: string };

export function PropertiesPage() {
  const canManage = useCurrentUser().role === 'Admin';
  const [properties, setProperties] = useState<Property[]>([]);
  const [unitsByProperty, setUnitsByProperty] = useState<Record<string, Unit[]>>({});
  const [inspections, setInspections] = useState<Inspection[]>([]);
  const [query, setQuery] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [openPropertyId, setOpenPropertyId] = useState<string | null>(null);
  const [newUnit, setNewUnit] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const refresh = useCallback(async () => {
    const [propertyList, inspectionList] = await Promise.all([
      apiRequest<Property[]>('/api/v1/properties'),
      apiRequest<Inspection[]>('/api/v1/inspections'),
    ]);
    const units = await Promise.all(propertyList.map((property) =>
      apiRequest<Unit[]>(`/api/v1/properties/${property.id}/units`)));
    setProperties(propertyList);
    setInspections(inspectionList);
    setUnitsByProperty(Object.fromEntries(propertyList.map((property, index) => [property.id, units[index]])));
  }, []);

  useEffect(() => {
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar os imóveis.'))
      .finally(() => setLoading(false));
  }, [refresh]);

  const filtered = useMemo(() => properties.filter((property) =>
    `${property.name} ${property.address}`.toLowerCase().includes(query.toLowerCase())), [properties, query]);
  const unitCount = Object.values(unitsByProperty).reduce((sum, units) => sum + units.length, 0);

  async function createProperty(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      await apiRequest<Property>('/api/v1/properties', {
        method: 'POST', body: JSON.stringify({ name, address }),
      });
      await refresh();
      setName('');
      setAddress('');
      setShowForm(false);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível salvar o imóvel.');
    } finally {
      setSaving(false);
    }
  }

  async function createUnit(event: React.FormEvent<HTMLFormElement>, propertyId: string) {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      await apiRequest<Unit>(`/api/v1/properties/${propertyId}/units`, {
        method: 'POST', body: JSON.stringify({ identifier: newUnit }),
      });
      await refresh();
      setNewUnit('');
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível salvar a unidade.');
    } finally {
      setSaving(false);
    }
  }

  return <>
    <PageHeading title="Imóveis" description="Centralize imóveis e unidades da organização." />
    <div className="page-actions"><label className="inline-search"><Icon name="search" size={17} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar imóvel..." aria-label="Buscar imóvel" /></label>{canManage && <button className="button button--primary" type="button" onClick={() => setShowForm((value) => !value)}><Icon name="plus" size={17} /> Novo imóvel</button>}</div>
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    {canManage && showForm && <form className="panel compact-form" onSubmit={createProperty}><PanelHeader title="Cadastrar imóvel" /><div className="form-grid"><label className="form-field"><span>Nome</span><input value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required /></label><label className="form-field"><span>Endereço</span><input value={address} onChange={(event) => setAddress(event.target.value)} maxLength={1000} required /></label></div><div className="form-actions"><button className="button button--outline" type="button" onClick={() => setShowForm(false)}>Cancelar</button><button className="button button--primary" type="submit" disabled={saving}>Salvar imóvel</button></div></form>}
    <section className="stats-grid stats-grid--three"><MetricCard icon="building" label="Imóveis cadastrados" value={String(properties.length)} note="na organização" /><MetricCard icon="home" label="Unidades cadastradas" value={String(unitCount)} note="em todos os imóveis" tone="blue" /><MetricCard icon="check" label="Vistorias registradas" value={String(inspections.length)} note="na organização" /></section>
    <section className="panel"><PanelHeader title="Todos os imóveis" action={<span className="panel-caption">{filtered.length} registros</span>} /><div className="property-grid">{filtered.map((property) => {
      const units = unitsByProperty[property.id] ?? [];
      const unitIds = new Set(units.map((unit) => unit.id));
      const inspectionCount = inspections.filter((inspection) => unitIds.has(inspection.unitId)).length;
      const isOpen = openPropertyId === property.id;
      return <article className="property-card" key={property.id}><div className="property-card-icon"><Icon name="building" size={22} /></div><div className="property-card-copy"><h2>{property.name}</h2><p><Icon name="mapPin" size={14} /> {property.address}</p></div><div className="property-card-stats"><span><strong>{units.length}</strong> unidades</span><span><strong>{inspectionCount}</strong> vistorias</span></div><button className="button button--soft" type="button" onClick={() => { setOpenPropertyId(isOpen ? null : property.id); setNewUnit(''); }}>{isOpen ? 'Fechar' : 'Ver unidades'} <Icon name="arrowRight" size={15} /></button>{isOpen && <div><ul>{units.map((unit) => <li key={unit.id}>{unit.identifier}</li>)}</ul>{units.length === 0 && <p>Nenhuma unidade cadastrada.</p>}{canManage && <form onSubmit={(event) => createUnit(event, property.id)}><label className="form-field"><span>Nova unidade</span><input value={newUnit} onChange={(event) => setNewUnit(event.target.value)} maxLength={100} placeholder="Ex.: Apto 101" required /></label><button className="button button--soft" type="submit" disabled={saving}>Adicionar unidade</button></form>}</div>}</article>;
    })}</div>{!loading && !filtered.length && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="search" size={26} /></div><h2>Nenhum imóvel encontrado</h2><p>Altere a busca ou cadastre um novo imóvel.</p></div></div>}</section>
  </>;
}
