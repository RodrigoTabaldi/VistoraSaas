import Link from 'next/link';
import { Icon } from '../../../components/icons';
import { DonutChart, LegendRow, MetricCard, PageHeading, PanelHeader } from '../../../components/dashboard-primitives';
import { cityMetrics, occurrenceTypes } from '../../../lib/mock-data';

const linePoints = '0,160 30,140 60,151 90,119 120,129 150,108 180,119 210,96 240,105 270,74 300,89 330,65 360,48 390,71 420,113 450,88 480,105 510,77 540,94 570,72 600,83 630,60 660,76 690,53 720,69';

export default function AnalysisPage() {
  return (
    <>
      <PageHeading title="Análises e indicadores" description="Acompanhe o desempenho das vistorias imobiliárias e tome decisões com base em dados reais." side="Última atualização\n14 de abr. de 2025, 10:24" />

      <section className="filters-bar" aria-label="Filtros de análise">
        <label className="filter-field"><span>Período</span><div className="filter-select"><Icon name="calendar" size={17} /><select defaultValue="Abril de 2025"><option>Março de 2025</option><option>Abril de 2025</option><option>Maio de 2025</option></select><Icon name="chevronDown" size={14} /></div></label>
        <label className="filter-field"><span>Cidade</span><div className="filter-select"><Icon name="mapPin" size={17} /><select defaultValue="Todas as cidades"><option>Todas as cidades</option><option>São Paulo</option><option>Osasco</option></select><Icon name="chevronDown" size={14} /></div></label>
        <label className="filter-field"><span>Tipo de vistoria</span><div className="filter-select"><Icon name="file" size={17} /><select defaultValue="Todos os tipos"><option>Todos os tipos</option><option>Entrada</option><option>Saída</option><option>Periódica</option></select><Icon name="chevronDown" size={14} /></div></label>
        <label className="filter-field"><span>Equipe</span><div className="filter-select"><Icon name="users" size={17} /><select defaultValue="Todas as equipes"><option>Todas as equipes</option><option>Equipe São Paulo</option><option>Equipe ABC</option></select><Icon name="chevronDown" size={14} /></div></label>
        <button className="filter-reset" type="button"><Icon name="refresh" size={16} /> Limpar filtros</button>
      </section>

      <section className="stats-grid">
        <MetricCard icon="building" label="Total de vistorias no mês" value="248" trend="↗ 12%" note="em relação a março" />
        <MetricCard icon="clock" label="SLA médio (conclusão)" value="1h 32min" trend="↓ 18%" trendTone="down" note="em relação a março" tone="blue" />
        <MetricCard icon="check" label="Taxa de conformidade" value="92%" trend="↗ 6 p.p." note="em relação a março" />
        <MetricCard icon="file" label="Relatórios emitidos" value="236" trend="↗ 14%" note="em relação a março" tone="blue" />
      </section>

      <section className="analysis-grid">
        <article className="panel">
          <PanelHeader title="Vistorias ao longo do tempo"><button className="chart-select" type="button">Últimos 30 dias <Icon name="chevronDown" size={14} /></button></PanelHeader>
          <div className="analysis-chart">
            <div className="line-chart">
              <div className="chart-axis"><span>50</span><span>40</span><span>30</span><span>20</span><span>10</span><span>0</span></div>
              <svg viewBox="0 0 720 180" preserveAspectRatio="none" aria-label="Gráfico de vistorias ao longo dos últimos 30 dias" role="img">
                <defs><linearGradient id="areaFill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stopColor="#aee8ca" stopOpacity=".65" /><stop offset="100%" stopColor="#e9f9f1" stopOpacity=".22" /></linearGradient></defs>
                <polygon points={`0,180 ${linePoints} 720,180`} fill="url(#areaFill)" />
                <polyline points={linePoints} fill="none" stroke="#12a56d" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
                <circle cx="360" cy="48" r="5" fill="#12a56d" stroke="#fff" strokeWidth="3" />
              </svg>
              <div className="chart-tooltip">02 de abr. de 2025<strong>42 vistorias</strong></div>
            </div>
            <div className="line-chart-labels"><span>15 mar</span><span>20 mar</span><span>25 mar</span><span>30 mar</span><span>04 abr</span><span>09 abr</span><span>14 abr</span></div>
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Vistorias por cidade" action={<a className="panel-link" href="#cidades">Ver todas <Icon name="arrowRight" size={15} /></a>} />
          <div className="panel-body horizontal-bars">
            {cityMetrics.map((item, index) => <div className="hbar-row" key={item.city}><span>{item.city}</span><div className="hbar-track"><div className="hbar-fill" style={{ width: `${item.percent * 3.2}%`, background: index < 2 ? '#07825f' : '#52c998' }} /></div><span className="hbar-value">{item.value}</span><span className="hbar-percent">{item.percent}%</span></div>)}
          </div>
        </article>

        <article className="panel analysis-donut">
          <PanelHeader title="Principais tipos de ocorrência" />
          <div className="panel-body">
            <DonutChart value={31} label="124">
              <div className="legend">
                {occurrenceTypes.map((item) => <LegendRow key={item.label} label={item.label} value={item.value} percent={item.percent} color={item.color as 'green' | 'yellow' | 'gray' | 'light' | 'red' | 'orange'} />)}
              </div>
            </DonutChart>
          </div>
        </article>
      </section>

      <section className="analysis-grid analysis-grid--bottom">
        <article className="panel">
          <PanelHeader title="Vistorias por tipo" />
          <div className="stacked-chart">
            {[['20%', '16%', '11%', '12%'], ['23%', '18%', '13%', '10%'], ['25%', '20%', '12%', '11%'], ['29%', '21%', '14%', '13%']].map((segments, index) => <div className="stacked-col" key={index}><span className="stack-segment stack-segment--gray" style={{ height: segments[3] }} /><span className="stack-segment stack-segment--yellow" style={{ height: segments[2] }} /><span className="stack-segment stack-segment--mint" style={{ height: segments[1] }} /><span className="stack-segment stack-segment--green" style={{ height: segments[0] }} /><span className="stack-segment stack-segment--dark" style={{ height: `${18 + index * 4}%` }} /></div>)}
          </div>
          <div className="stack-labels"><span>Jan</span><span>Fev</span><span>Mar</span><span>Abr</span></div>
        </article>

        <article className="panel analysis-insight">
          <span className="insight-icon"><Icon name="chart" size={23} /></span>
          <h3>+ 28% nas vistorias em abril</h3>
          <p>O número de vistorias realizadas este mês é 28% maior que no mês anterior, com destaque para a cidade de São Paulo, com aumento de 35%.</p>
          <Link className="button button--outline" href="#analise-completa">Ver análise completa <Icon name="arrowRight" size={15} /></Link>
        </article>

        <article className="panel">
          <PanelHeader title="Ranking de imóveis com mais ocorrências" action={<a className="panel-link" href="#ranking">Ver todos <Icon name="arrowRight" size={15} /></a>} />
          <div className="panel-body">
            <table className="data-table ranking-table"><thead><tr><th>#</th><th>Imóvel</th><th>Cidade</th><th>Ocorrências</th><th>Última vistoria</th></tr></thead><tbody>
              <tr><td>1</td><td>Cond. Parque das Flores</td><td>Osasco</td><td><strong>12</strong></td><td>12 abr 2025</td></tr>
              <tr><td>2</td><td>Edifício Solar Paulista</td><td>São Paulo</td><td><strong>9</strong></td><td>11 abr 2025</td></tr>
              <tr><td>3</td><td>Residencial Vista Verde</td><td>São Paulo</td><td><strong>8</strong></td><td>10 abr 2025</td></tr>
              <tr><td>4</td><td>Condomínio Bela Vista</td><td>Santo André</td><td><strong>7</strong></td><td>09 abr 2025</td></tr>
              <tr><td>5</td><td>Resid. Harmonia</td><td>São Bernardo</td><td><strong>6</strong></td><td>08 abr 2025</td></tr>
            </tbody></table>
          </div>
        </article>
      </section>

      <section className="analysis-grid analysis-grid--lower">
        <article className="panel">
          <PanelHeader title="Taxa de conformidade ao longo do tempo" />
          <div className="analysis-chart">
            <div className="line-chart" style={{ minHeight: 125 }}>
              <div className="chart-axis"><span>100%</span><span>80%</span><span>60%</span></div>
              <svg viewBox="0 0 500 110" preserveAspectRatio="none" aria-label="Taxa de conformidade" role="img"><polyline points="30,83 180,55 330,47 470,32" fill="none" stroke="#0d9f69" strokeWidth="3" strokeLinecap="round" /><circle cx="470" cy="32" r="5" fill="#087b5c" stroke="#fff" strokeWidth="3" /></svg><strong style={{ position: 'absolute', right: 2, top: 14, color: '#163147', fontSize: 12 }}>92%</strong>
            </div>
            <div className="line-chart-labels"><span>Jan</span><span>Fev</span><span>Mar</span><span>Abr</span></div>
          </div>
        </article>

        <article className="panel">
          <PanelHeader title="Tempo médio por tipo de vistoria" />
          <div className="mini-bars">
            {[['Entrada', '1h 10min', '74%', '#0c8d65'], ['Saída', '1h 48min', '94%', '#13ac74'], ['Periódica', '1h 32min', '82%', '#52c995'], ['Entrega', '1h 15min', '77%', '#7ddab4'], ['Outros', '2h 05min', '100%', '#aab8c5']].map(([label, time, width, color]) => <div className="hbar-row" key={label}><span>{label}</span><div className="progress-line"><span style={{ width, background: color }} /></div><span className="hbar-value">{time}</span></div>)}
          </div>
        </article>

        <article className="panel period-donut">
          <PanelHeader title="Status das vistorias no período" />
          <div className="panel-body"><DonutChart value={57} label="248"><div className="legend"><LegendRow label="Concluídas" value="142" percent={57} color="green" /><LegendRow label="Em andamento" value="48" percent={19} color="yellow" /><LegendRow label="Pendentes" value="36" percent={15} color="orange" /><LegendRow label="Canceladas" value="22" percent={9} color="red" /></div></DonutChart></div>
        </article>
      </section>
    </>
  );
}
