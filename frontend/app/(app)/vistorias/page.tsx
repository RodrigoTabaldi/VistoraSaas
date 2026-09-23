import type { CSSProperties } from 'react';
import { Icon } from '../../../components/icons';
import { MetricCard, PageHeading, PanelHeader } from '../../../components/dashboard-primitives';
import { InspectionsTable } from '../../../components/inspections-table';

export default function InspectionsPage() {
  return (
    <>
      <PageHeading title="Vistorias" description="Acompanhe, organize e execute inspeções dos seus imóveis de forma simples e eficiente." side="“Transparência hoje, mais confiança amanhã.”" />
      <section className="stats-grid stats-grid--three">
        <MetricCard icon="calendar" label="Vistorias hoje" value="8" trend="↗ 33%" note="em relação ao mesmo dia anterior" />
        <MetricCard icon="file" label="Relatórios pendentes" value="18" trend="↗ 12%" trendTone="bad" note="aguardando finalização" tone="blue" />
        <MetricCard icon="check" label="Taxa de conclusão" value="78%" trend="↗ 8%" note="no período dos últimos 30 dias" />
      </section>

      <section className="list-layout">
        <article className="panel">
          <PanelHeader title="Todas as vistorias" />
          <div className="panel-body"><InspectionsTable /></div>
        </article>
        <aside className="aside-stack">
          <article className="panel agenda-card">
            <PanelHeader title="Agenda de hoje" action={<a className="panel-link" href="#agenda">Ver agenda completa <Icon name="arrowRight" size={15} /></a>} />
            <div className="agenda-date"><button type="button" aria-label="Dia anterior"><Icon name="chevronLeft" size={15} /></button><span>12 de abril de 2024</span><button type="button" aria-label="Próximo dia"><Icon name="chevronRight" size={15} /></button></div>
            <div className="agenda-items"><div className="agenda-item"><time className="agenda-time">09:00</time><span className="agenda-marker" /><div><strong>Residencial Vista Verde</strong><span>Entrada · São Paulo, SP</span></div><Icon className="chevron" name="chevronRight" size={15} /></div><div className="agenda-item"><time className="agenda-time">11:00</time><span className="agenda-marker" /><div><strong>Condomínio das Acácias</strong><span>Saída · Diadema, SP</span></div><Icon className="chevron" name="chevronRight" size={15} /></div><div className="agenda-item"><time className="agenda-time">14:30</time><span className="agenda-marker agenda-marker--yellow" /><div><strong>Condomínio Parque das Flores</strong><span>Periódica · Osasco, SP</span></div><Icon className="chevron" name="chevronRight" size={15} /></div><div className="agenda-item"><time className="agenda-time">16:00</time><span className="agenda-marker agenda-marker--yellow" /><div><strong>Condomínio Bela Vista</strong><span>Entrada · Santo André, SP</span></div><Icon className="chevron" name="chevronRight" size={15} /></div></div>
          </article>

          <article className="panel">
            <PanelHeader title="Status das vistorias"><button className="chart-select" type="button">Este mês <Icon name="chevronDown" size={14} /></button></PanelHeader>
            <div className="panel-body"><div className="donut-layout"><div className="donut" style={{ '--donut-value': '38%', '--donut-color': '#11a870' } as CSSProperties}><div className="donut-center"><strong>48</strong><span>vistorias</span></div></div><div className="legend"><div className="legend-row"><span className="legend-dot legend-dot--green" /><span>Concluídas</span><strong>18</strong><span>38%</span></div><div className="legend-row"><span className="legend-dot legend-dot--yellow" /><span>Em andamento</span><strong>8</strong><span>17%</span></div><div className="legend-row"><span className="legend-dot legend-dot--orange" /><span>Pendentes</span><strong>12</strong><span>25%</span></div><div className="legend-row"><span className="legend-dot legend-dot--red" /><span>Atrasadas</span><strong>6</strong><span>12%</span></div><div className="legend-row"><span className="legend-dot legend-dot--gray" /><span>Canceladas</span><strong>4</strong><span>8%</span></div></div></div><div className="chart-callout"><span className="dashboard-callout-icon"><Icon name="chart" size={19} /></span><span><strong>+ 8% de vistorias concluídas</strong><br />em relação ao mês anterior</span><Icon className="callout-arrow" name="chevronRight" size={17} /></div></div>
          </article>

          <article className="panel"><PanelHeader title="Ações rápidas" /><div className="quick-actions"><button className="quick-action" type="button"><Icon name="plus" size={20} />Nova vistoria</button><button className="quick-action" type="button"><Icon name="file" size={20} />Gerar relatório</button><button className="quick-action" type="button"><Icon name="calendar" size={20} />Ver agenda</button><button className="quick-action" type="button"><Icon name="download" size={20} />Exportar dados</button></div></article>
        </aside>
      </section>
    </>
  );
}
