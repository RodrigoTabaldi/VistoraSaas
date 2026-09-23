'use client';

import Link from 'next/link';
import { Icon, type IconName } from './icons';
import { Avatar, DonutChart, LegendRow, MetricCard, PageHeading, PanelHeader, StatusBadge } from './dashboard-primitives';
import { recentActivities, typeMetrics } from '../lib/mock-data';
import { useInspections } from '../lib/inspection-store';

function isCompleted(status: string) {
  return status.toLowerCase().includes('conclu');
}

function formatPercent(value: number, total: number) {
  return total ? Math.round((value / total) * 100) : 0;
}

export function DashboardHome() {
  const { inspections, completeInspection } = useInspections();
  const total = inspections.length;
  const completed = inspections.filter((item) => isCompleted(item.status)).length;
  const inProgress = inspections.filter((item) => item.status.toLowerCase().includes('andamento')).length;
  const overdue = inspections.filter((item) => item.status.toLowerCase().includes('atras')).length;
  const pending = inspections.filter((item) => ['agendada', 'pendente'].some((status) => item.status.toLowerCase().includes(status))).length;
  const upcoming = inspections.slice(0, 5);

  return (
    <>
      <PageHeading title="Bem-vindo à Vistora" description="Gerencie suas triagens imobiliárias de forma simples, organizada e eficiente." side="“Imóveis bem cuidados criam grandes histórias.”" />

      <section className="stats-grid" aria-label="Indicadores principais">
        <MetricCard icon="building" label="Total de triagens" value={String(total)} trend="↗ +12%" note="em relação ao mês anterior" />
        <MetricCard icon="file" label="Triagens pendentes" value={String(pending)} trend="↗ +6%" trendTone="bad" note="aguardando execução" tone="blue" />
        <MetricCard icon="check" label="Triagens concluídas" value={String(completed)} trend="↗ +28%" note="em relação ao mês anterior" />
        <MetricCard icon="clock" label="Em andamento" value={String(inProgress)} trend="↓ -18%" trendTone="down" note="com execução iniciada" tone="blue" />
      </section>

      <section className="dashboard-grid">
        <article className="panel">
          <PanelHeader title="Próximas triagens" action={<Link className="panel-link" href="/triagens">Ver todas <Icon name="arrowRight" size={16} /></Link>} />
          <div className="panel-body">
            <table className="data-table data-table--schedule">
              <thead><tr><th>Data</th><th>Horário</th><th>Imóvel</th><th>Tipo</th><th>Responsável</th><th>Status</th><th>Ação</th></tr></thead>
              <tbody>
                {upcoming.map((item) => (
                  <tr key={item.id}>
                    <td className="date-cell"><strong>{item.date}</strong></td>
                    <td>{item.time}</td>
                    <td className="property-cell"><Link href={`/vistorias/${item.id}`}><strong>{item.property}</strong><span>{item.city}</span></Link></td>
                    <td>{item.type}</td>
                    <td><div className="responsible-cell"><Avatar initials={item.responsibleInitials} tone={item.responsibleTone} />{item.responsible}</div></td>
                    <td><StatusBadge status={item.status} /></td>
                    <td className="actions-cell">
                      {isCompleted(item.status)
                        ? <Link className="schedule-action schedule-action--done" href={`/vistorias/${item.id}`} aria-label={`Abrir ${item.code}`}><Icon name="eye" size={15} /></Link>
                        : <button className="schedule-action" type="button" onClick={() => completeInspection(item.id)} aria-label={`Concluir ${item.code}`}><Icon name="check" size={15} /></button>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {upcoming.length === 0 && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="calendar" size={27} /></div><h2>Nenhuma triagem agendada</h2><p>Crie a primeira triagem para começar a acompanhar sua operação.</p><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={16} /> Criar triagem</Link></div></div>}
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Status das triagens"><Link className="chart-select" href="/triagens">Ver lista <Icon name="arrowRight" size={14} /></Link></PanelHeader>
          <div className="panel-body">
            <DonutChart value={formatPercent(completed, total)} label={String(total)} unit="triagens">
              <div className="legend">
                <LegendRow label="Concluídas" value={completed} percent={formatPercent(completed, total)} color="green" />
                <LegendRow label="Em andamento" value={inProgress} percent={formatPercent(inProgress, total)} color="yellow" />
                <LegendRow label="Pendentes" value={pending} percent={formatPercent(pending, total)} color="orange" />
                <LegendRow label="Atrasadas" value={overdue} percent={formatPercent(overdue, total)} color="red" />
              </div>
            </DonutChart>
            <div className="chart-callout"><span className="dashboard-callout-icon"><Icon name="chart" size={22} /></span><span><strong>{completed} triagens concluídas</strong><br />continue mantendo seus registros em dia</span><Icon className="callout-arrow" name="chevronRight" size={17} /></div>
          </div>
        </article>
      </section>

      <section className="dashboard-grid dashboard-grid--bottom">
        <article className="panel">
          <PanelHeader title="Atividades recentes" action={<a className="panel-link" href="#atividades">Ver todas <Icon name="arrowRight" size={16} /></a>} />
          <div className="panel-body activity-list">
            {recentActivities.map((activity) => (
              <div className="activity-item" key={activity.title}>
                <span className="activity-icon"><Icon name={activity.icon as IconName} size={18} /></span>
                <div><strong>{activity.title.replace('Vistoria', 'Triagem')}</strong><span>{activity.subtitle}</span></div>
                <time className="activity-time">{activity.time}</time>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Mapa de imóveis" action={<a className="panel-link" href="#mapa">Ver todos <Icon name="arrowRight" size={16} /></a>} />
          <div className="panel-body">
            <div className="map-placeholder" aria-label="Mapa ilustrativo de imóveis em São Paulo">
              <span className="map-pin map-pin--1" /><span className="map-pin map-pin--2" /><span className="map-pin map-pin--3" /><span className="map-pin map-pin--4" /><span className="map-pin map-pin--5" /><span className="map-pin map-pin--6" /><span className="map-pin map-pin--main"><Icon name="home" size={13} /></span>
              <div className="map-label">{total * 24} imóveis<span>em 12 cidades</span></div>
            </div>
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Tipos de triagens"><Link className="chart-select" href="/triagens">Ver detalhes <Icon name="arrowRight" size={14} /></Link></PanelHeader>
          <div className="panel-body">
            <div className="horizontal-bars">
              {typeMetrics.map((item) => <div className="hbar-row" key={item.label}><span>{item.label}</span><div className="hbar-track"><div className={`hbar-fill hbar-fill--${item.color}`} style={{ width: `${item.percent * 2.7}%` }} /></div><span className="hbar-value">{item.value}</span><span className="hbar-percent">{item.percent}%</span></div>)}
            </div>
            <Link className="chart-callout chart-callout--link" href="/triagens/nova"><span className="dashboard-callout-icon"><Icon name="plus" size={20} /></span><span><strong>Criar nova triagem</strong><br />a partir de um checklist</span><Icon className="callout-arrow" name="chevronRight" size={17} /></Link>
          </div>
        </article>
      </section>
    </>
  );
}
