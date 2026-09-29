'use client';

import Link from 'next/link';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

export function AgendaPage() {
  const { inspections, error } = useInspections();
  return <>
    <PageHeading title="Agenda" description="O agendamento ainda não está disponível. Acompanhe abaixo as vistorias registradas." />
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <div className="page-actions"><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={17} /> Criar vistoria</Link></div>
    <section className="panel"><div className="panel-header"><h2 className="panel-title">Vistorias por data de criação</h2></div><div className="agenda-list">{inspections.map((item) => <Link className="agenda-row" href={`/vistorias/${item.id}`} key={item.id}><span className="agenda-time">{item.date}<br />{item.time}</span><span className="agenda-marker" /><span className="agenda-row-copy"><strong>{item.property}</strong><span>{item.city} · {item.type}</span></span><StatusBadge status={item.status} /><Icon name="chevronRight" size={16} /></Link>)}</div>{inspections.length === 0 && <div className="empty-state"><div className="empty-state-inner"><h2>Nenhuma vistoria registrada</h2><p>Crie uma vistoria para começar.</p></div></div>}</section>
  </>;
}
