'use client';

import Link from 'next/link';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

export function AgendaPage() {
  const { inspections, error } = useInspections();
  const scheduled = inspections.filter((item) => item.scheduledAtUtc)
    .sort((first, second) => Date.parse(first.scheduledAtUtc!) - Date.parse(second.scheduledAtUtc!));
  const unscheduled = inspections.filter((item) => !item.scheduledAtUtc && item.status !== 'Concluída');

  return <>
    <PageHeading title="Agenda" description="Vistorias ordenadas pelo horário agendado na organização." />
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <div className="page-actions"><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={17} /> Criar vistoria</Link></div>
    <section className="panel">
      <div className="panel-header"><h2 className="panel-title">Próximas vistorias</h2><span className="panel-caption">{scheduled.length} agendadas</span></div>
      <div className="agenda-list">{scheduled.map((item) => {
        const when = new Date(item.scheduledAtUtc!);
        return <Link className="agenda-row agenda-row--compact" href={`/vistorias/${item.id}`} key={item.id}>
          <span className="agenda-time">{when.toLocaleDateString('pt-BR')}<br />{when.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</span>
          <span className="agenda-marker" />
          <span className="agenda-row-copy"><strong>{item.property}</strong><span>{item.city} · {item.type}</span></span>
          <StatusBadge status={item.status} /><Icon name="chevronRight" size={16} />
        </Link>;
      })}</div>
      {scheduled.length === 0 && <div className="empty-state"><div className="empty-state-inner"><h2>Nenhuma vistoria agendada</h2><p>Abra uma vistoria e informe data e hora para incluí-la na agenda.</p><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={16} /> Criar vistoria</Link></div></div>}
    </section>
    <section className="panel compact-form">
      <div className="panel-header"><h2 className="panel-title">Sem horário definido</h2><span className="panel-caption">{unscheduled.length} vistorias</span></div>
      {unscheduled.map((item) => <Link className="agenda-row agenda-row--compact" href={`/vistorias/${item.id}`} key={item.id}>
        <span className="agenda-time">Sem data</span><span className="agenda-marker" />
        <span className="agenda-row-copy"><strong>{item.property}</strong><span>{item.city} · {item.type}</span></span>
        <span className="panel-link">Agendar</span><Icon name="chevronRight" size={16} />
      </Link>)}
      {unscheduled.length === 0 && <p>Todas as vistorias têm horário definido.</p>}
    </section>
  </>;
}
