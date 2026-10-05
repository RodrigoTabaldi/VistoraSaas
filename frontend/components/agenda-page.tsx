'use client';
import Link from 'next/link';
import { useState } from 'react';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspectionPage } from '../lib/use-inspection-page';

function AgendaList({ scheduled }: { scheduled: boolean }) {
  const [page, setPage] = useState(1);
  const { items, total, loading, error } = useInspectionPage(new URLSearchParams({ status: 'Draft', schedule: scheduled ? 'scheduled' : 'unscheduled', page: String(page), pageSize: '20' }).toString());
  const pages = Math.max(1, Math.ceil(total / 20));
  return <section className="panel"><div className="panel-header"><h2 className="panel-title">{scheduled ? 'Vistorias agendadas' : 'Sem horário definido'}</h2><span className="panel-caption">{total} vistorias</span></div>
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    {loading ? <p role="status">Carregando agenda...</p> : <div className="agenda-list">{items.map((item) => <Link className="agenda-row agenda-row--compact" href={`/vistorias/${item.id}`} key={item.id}>
      <span className="agenda-time">{item.scheduledAtUtc ? <>{new Date(item.scheduledAtUtc).toLocaleDateString('pt-BR')}<br />{new Date(item.scheduledAtUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</> : 'Sem data'}</span>
      <span className="agenda-marker" /><span className="agenda-row-copy"><strong>{item.property}</strong><span>{item.city} · {item.type}</span></span><StatusBadge status={item.status} /><Icon name="chevronRight" size={16} />
    </Link>)}</div>}
    {!loading && !error && !items.length && <p className="panel-body">{scheduled ? 'Nenhuma vistoria agendada.' : 'Nenhuma vistoria sem data.'}</p>}
    <div className="pagination"><span>Página {page} de {pages}</span><div className="pagination-controls"><button type="button" disabled={loading || page === 1} onClick={() => setPage(page - 1)}>Anterior</button><button type="button" disabled={loading || page >= pages} onClick={() => setPage(page + 1)}>Próxima</button></div></div>
  </section>;
}

export function AgendaPage() {
  return <><PageHeading title="Agenda" description="Vistorias abertas ordenadas pelo horário agendado." /><div className="page-actions"><Link className="button button--primary" href="/vistorias/nova"><Icon name="plus" size={17} /> Nova vistoria</Link></div><AgendaList scheduled /><AgendaList scheduled={false} /></>;
}
