import Link from 'next/link';
import { Icon, type IconName } from '../../../components/icons';
import { Avatar, DonutChart, LegendRow, MetricCard, PageHeading, PanelHeader, ProgressBar, StatusBadge } from '../../../components/dashboard-primitives';
import { dashboardSchedule, recentActivities, typeMetrics } from '../../../lib/mock-data';

export default function DashboardPage() {
  return (
    <>
      <PageHeading title="Bem-vindo à Vistora" description="Gerencie suas vistorias imobiliárias de forma simples, organizada e eficiente." side="“Imóveis bem cuidados criam grandes histórias.”" />

      <section className="stats-grid" aria-label="Indicadores principais">
        <MetricCard icon="building" label="Total de imóveis" value="248" trend="↗ +12%" note="em relação ao mês anterior" />
        <MetricCard icon="file" label="Vistorias pendentes" value="18" trend="↗ +6%" trendTone="bad" note="em relação ao mês anterior" tone="blue" />
        <MetricCard icon="check" label="Vistorias concluídas" value="142" trend="↗ +28%" note="em relação ao mês anterior" />
        <MetricCard icon="clock" label="Tempo médio" value="1h 32min" trend="↓ -18%" trendTone="down" note="em relação ao mês anterior" tone="blue" />
      </section>

      <section className="dashboard-grid">
        <article className="panel">
          <PanelHeader title="Próximas vistorias" action={<Link className="panel-link" href="/vistorias">Ver todas <Icon name="arrowRight" size={16} /></Link>} />
          <div className="panel-body">
            <table className="data-table data-table--schedule">
              <thead><tr><th>Data</th><th>Horário</th><th>Imóvel</th><th>Tipo</th><th>Responsável</th><th>Status</th></tr></thead>
              <tbody>
                {dashboardSchedule.map((item) => (
                  <tr key={`${item.date}-${item.time}`}>
                    <td className="date-cell"><strong>{item.day}</strong><span>{item.date}</span></td>
                    <td>{item.time}</td>
                    <td className="property-cell"><strong>{item.property}</strong><span>{item.city}</span></td>
                    <td>{item.type}</td>
                    <td><div className="responsible-cell"><Avatar initials={item.initials} tone={item.tone} />{item.responsible}</div></td>
                    <td><StatusBadge status={item.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Status das vistorias"><button className="chart-select" type="button">Este mês <Icon name="chevronDown" size={14} /></button></PanelHeader>
          <div className="panel-body">
            <DonutChart value={68} label="208">
              <div className="legend">
                <LegendRow label="Concluídas" value="142" percent={68} color="green" />
                <LegendRow label="Pendentes" value="18" percent={9} color="yellow" />
                <LegendRow label="Em andamento" value="24" percent={12} color="gray" />
                <LegendRow label="Canceladas" value="16" percent={8} color="light" />
              </div>
            </DonutChart>
            <div className="chart-callout"><span className="dashboard-callout-icon"><Icon name="chart" size={22} /></span><span><strong>+28% de vistorias concluídas</strong><br />em relação ao mês anterior</span><Icon className="callout-arrow" name="chevronRight" size={17} /></div>
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
                <div><strong>{activity.title}</strong><span>{activity.subtitle}</span></div>
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
              <div className="map-label">248 imóveis<span>em 12 cidades</span></div>
            </div>
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Tipos de vistorias"><button className="chart-select" type="button">Este mês <Icon name="chevronDown" size={14} /></button></PanelHeader>
          <div className="panel-body">
            <div className="horizontal-bars">
              {typeMetrics.map((item) => <div className="hbar-row" key={item.label}><span>{item.label}</span><div className="hbar-track"><div className={`hbar-fill hbar-fill--${item.color}`} style={{ width: `${item.percent * 2.7}%` }} /></div><span className="hbar-value">{item.value}</span><span className="hbar-percent">{item.percent}%</span></div>)}
            </div>
            <div className="chart-callout"><span className="dashboard-callout-icon"><Icon name="file" size={20} /></span><span><strong>Exporte relatórios completos</strong><br />em PDF ou Excel</span><Icon className="callout-arrow" name="chevronRight" size={17} /></div>
          </div>
        </article>
      </section>
    </>
  );
}
