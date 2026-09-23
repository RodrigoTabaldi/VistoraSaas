'use client';

import { useMemo, useState } from 'react';
import { Icon } from './icons';
import { MetricCard, PageHeading, PanelHeader } from './dashboard-primitives';
import { useWorkspace } from '../lib/workspace-store';

export function PropertiesPage() {
  const { properties, addProperty } = useWorkspace();
  const [query, setQuery] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [units, setUnits] = useState('');
  const filtered = useMemo(() => properties.filter((property) => `${property.name} ${property.address}`.toLowerCase().includes(query.toLowerCase())), [properties, query]);

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    addProperty({ name, address, units: Number(units) || 0 });
    setName(''); setAddress(''); setUnits(''); setShowForm(false);
  }

  return <>
    <PageHeading title="Imóveis" description="Centralize imóveis, unidades e o histórico de inspeções da operação." />
    <div className="page-actions"><label className="inline-search"><Icon name="search" size={17} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar imóvel..." aria-label="Buscar imóvel" /></label><button className="button button--primary" type="button" onClick={() => setShowForm((value) => !value)}><Icon name="plus" size={17} /> Novo imóvel</button></div>
    {showForm && <form className="panel compact-form" onSubmit={submit}><PanelHeader title="Cadastrar imóvel" /><div className="form-grid"><label className="form-field"><span>Nome</span><input value={name} onChange={(event) => setName(event.target.value)} required /></label><label className="form-field"><span>Endereço</span><input value={address} onChange={(event) => setAddress(event.target.value)} required /></label><label className="form-field"><span>Unidades</span><input type="number" min="0" value={units} onChange={(event) => setUnits(event.target.value)} required /></label></div><div className="form-actions"><button className="button button--outline" type="button" onClick={() => setShowForm(false)}>Cancelar</button><button className="button button--primary" type="submit">Salvar imóvel</button></div></form>}
    <section className="stats-grid stats-grid--three"><MetricCard icon="building" label="Imóveis cadastrados" value={String(properties.length)} note="na organização" /><MetricCard icon="home" label="Unidades monitoradas" value={String(properties.reduce((sum, item) => sum + item.units, 0))} note="em todos os empreendimentos" tone="blue" /><MetricCard icon="check" label="Ocupação média" value="90%" note="dos imóveis ativos" /></section>
    <section className="panel"><PanelHeader title="Todos os imóveis" action={<span className="panel-caption">{filtered.length} registros</span>} /><div className="property-grid">{filtered.map((property) => <article className="property-card" key={property.id}><div className="property-card-icon"><Icon name="building" size={22} /></div><div className="property-card-copy"><h2>{property.name}</h2><p><Icon name="mapPin" size={14} /> {property.address}</p></div><div className="property-card-stats"><span><strong>{property.units}</strong> unidades</span><span><strong>{property.inspections}</strong> triagens</span><span><strong>{property.occupancy}</strong> ocupação</span></div><button className="button button--soft" type="button">Abrir imóvel <Icon name="arrowRight" size={15} /></button></article>)}</div>{!filtered.length && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="search" size={26} /></div><h2>Nenhum imóvel encontrado</h2><p>Altere a busca ou cadastre um novo imóvel.</p></div></div>}</section>
  </>;
}
