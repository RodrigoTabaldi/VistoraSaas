'use client';

import Link from 'next/link';
import { useInspections } from '../lib/inspection-store';
import { MetricCard, PageHeading, PanelHeader } from './dashboard-primitives';
import { InspectionsTable } from './inspections-table';
import { useInspectionPage } from '../lib/use-inspection-page';
import { Icon } from './icons';

export function InspectionsOverview() {
  const { statistics, hydrated, inspections: revision } = useInspections();
  const today = new Date().toLocaleDateString('pt-BR');
  const start = new Date(); start.setHours(0, 0, 0, 0);
  const end = new Date(start); end.setDate(end.getDate() + 1);
  const parameters = new URLSearchParams({ status: 'Draft', schedule: 'scheduled', from: start.toISOString(), to: end.toISOString(), pageSize: '20' });
  const agenda = useInspectionPage(parameters.toString(), revision);
  const scheduledToday = agenda.items;
  const total = statistics.reduce((sum, item) => sum + item.total, 0);
  const completed = statistics.reduce((sum, item) => sum + item.completed, 0);
  const pendingReports = statistics.reduce((sum, item) => sum + item.pendingReports, 0);
  const rate = total ? Math.round(completed * 100 / total) : 0;
  return <>
    <PageHeading title="Vistorias" description="Crie pelo endereço e acompanhe o checklist, a agenda e os laudos." />
    <section className="stats-grid stats-grid--three">
      <MetricCard icon="calendar" label="Agendadas para hoje" value={hydrated ? String(agenda.total) : '…'} note="vistorias ainda abertas" />
      <MetricCard icon="file" label="Laudos pendentes" value={hydrated ? String(pendingReports) : '…'} note="vistorias concluídas sem laudo" tone="blue" />
      <MetricCard icon="check" label="Taxa de conclusão" value={hydrated ? `${rate}%` : '…'} note="sobre todas as vistorias registradas" />
    </section>
    <section className="list-layout">
      <article className="panel"><PanelHeader title="Todas as vistorias" /><div className="panel-body"><InspectionsTable /></div></article>
      <aside className="aside-stack">
        <article className="panel"><PanelHeader title="Agenda de hoje" action={<Link className="panel-link" href="/agenda">Ver agenda</Link>} /><div className="panel-body">
          <p>{today}</p>
          {agenda.error && <p role="alert">{agenda.error}</p>}{agenda.loading ? <p role="status">Carregando agenda...</p> : scheduledToday.length ? scheduledToday.map((item) => <Link className="agenda-item" href={`/vistorias/${item.id}`} key={item.id}><time className="agenda-time">{new Date(item.scheduledAtUtc!).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</time><div><strong>{item.property}</strong><p>{item.type}</p></div></Link>) : <p>Nenhuma vistoria agendada para hoje.</p>}
          {agenda.total > scheduledToday.length && <Link href="/agenda">Ver as {agenda.total} vistorias de hoje na agenda</Link>}
        </div></article>
        <article className="panel"><PanelHeader title="Ações rápidas" /><div className="quick-actions"><Link className="quick-action" href="/vistorias/nova"><Icon name="plus" size={20} />Nova vistoria</Link><Link className="quick-action" href="/relatorios"><Icon name="file" size={20} />Ver laudos</Link><Link className="quick-action" href="/agenda"><Icon name="calendar" size={20} />Ver agenda</Link></div></article>
      </aside>
    </section>
  </>;
}
