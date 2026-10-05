'use client';

import Link from 'next/link';
import { Icon } from './icons';
import { DonutChart, LegendRow, MetricCard, PageHeading, PanelHeader, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

export function DashboardHome() {
  const { inspections, statistics, hydrated, error } = useInspections();
  const total = statistics.reduce((sum, item) => sum + item.total, 0);
  const completed = statistics.reduce((sum, item) => sum + item.completed, 0);
  const inProgress = total - completed;
  const completionRate = total ? Math.round(completed / total * 100) : 0;
  const recent = inspections.slice(0, 5);

  return <>
    <PageHeading title="Bem-vindo à Vistora" description="Acompanhe as vistorias da sua organização." />
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <section className="stats-grid" aria-label="Indicadores principais">
      <MetricCard icon="building" label="Total de vistorias" value={String(total)} note="na organização" />
      <MetricCard icon="file" label="Em andamento" value={String(inProgress)} note="aguardando conclusão" tone="blue" />
      <MetricCard icon="check" label="Concluídas" value={String(completed)} note="vistorias registradas" />
      <MetricCard icon="clock" label="Taxa de conclusão" value={`${completionRate}%`} note="do total registrado" tone="blue" />
    </section>
    <section className="dashboard-grid">
      <article className="panel"><PanelHeader title="Vistorias recentes" action={<Link className="panel-link" href="/triagens">Ver todas <Icon name="arrowRight" size={16} /></Link>} /><div className="panel-body"><table className="data-table data-table--schedule"><thead><tr><th>Criação</th><th>Imóvel</th><th>Tipo</th><th>Status</th><th>Ação</th></tr></thead><tbody>{recent.map((item) => <tr key={item.id}><td className="date-cell"><strong>{item.date}</strong><span>{item.time}</span></td><td className="property-cell"><Link href={`/vistorias/${item.id}`}><strong>{item.property}</strong><span>{item.city}</span></Link></td><td>{item.type}</td><td><StatusBadge status={item.status} /></td><td className="actions-cell"><Link className="schedule-action" href={`/vistorias/${item.id}`} aria-label={`Abrir ${item.code}`}><Icon name="eye" size={15} /></Link></td></tr>)}</tbody></table>{hydrated && !recent.length && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="calendar" size={27} /></div><h2>Nenhuma vistoria registrada</h2><p>Informe o endereço e crie sua primeira vistoria, sem cadastro prévio.</p><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={16} /> Criar vistoria</Link></div></div>}</div></article>
      <article className="panel"><PanelHeader title="Status das vistorias" /><div className="panel-body"><DonutChart value={completionRate} label={String(total)} unit="vistorias"><div className="legend"><LegendRow label="Concluídas" value={completed} percent={completionRate} color="green" /><LegendRow label="Em andamento" value={inProgress} percent={total ? 100 - completionRate : 0} color="yellow" /></div></DonutChart></div></article>
    </section>
  </>;
}
