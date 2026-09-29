'use client';

import Link from 'next/link';
import { useState } from 'react';
import { Icon } from './icons';
import { DonutChart, LegendRow, MetricCard, PageHeading, PanelHeader } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

export function AnalysisPage() {
  const { inspections, error } = useInspections();
  const [type, setType] = useState('Todos os tipos');
  const filtered = inspections.filter((item) => type === 'Todos os tipos' || item.type === type);
  const total = filtered.length;
  const completed = filtered.filter((item) => item.status === 'Concluída').length;
  const inProgress = total - completed;
  const moveIns = filtered.filter((item) => item.type === 'Entrada').length;
  const moveOuts = filtered.filter((item) => item.type === 'Saída').length;
  const completionRate = total ? Math.round(completed / total * 100) : 0;

  return <>
    <PageHeading title="Análises e indicadores" description="Indicadores calculados a partir das vistorias registradas na organização." />
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    <section className="filters-bar" aria-label="Filtros de análise"><label className="filter-field"><span>Tipo de vistoria</span><div className="filter-select"><Icon name="file" size={17} /><select value={type} onChange={(event) => setType(event.target.value)}><option>Todos os tipos</option><option>Entrada</option><option>Saída</option></select><Icon name="chevronDown" size={14} /></div></label></section>
    <section className="stats-grid"><MetricCard icon="building" label="Total de vistorias" value={String(total)} note="no filtro atual" /><MetricCard icon="clock" label="Em andamento" value={String(inProgress)} note="aguardando conclusão" tone="blue" /><MetricCard icon="check" label="Concluídas" value={String(completed)} note="no filtro atual" /><MetricCard icon="file" label="Taxa de conclusão" value={`${completionRate}%`} note="do total filtrado" tone="blue" /></section>
    <section className="analysis-grid"><article className="panel"><PanelHeader title="Vistorias por status" action={<Link className="chart-select" href="/triagens">Abrir lista <Icon name="arrowRight" size={14} /></Link>} /><div className="panel-body"><DonutChart value={completionRate} label={String(total)} unit="vistorias"><div className="legend"><LegendRow label="Concluídas" value={completed} percent={completionRate} color="green" /><LegendRow label="Em andamento" value={inProgress} percent={total ? 100 - completionRate : 0} color="yellow" /></div></DonutChart></div></article><article className="panel"><PanelHeader title="Tipos de vistoria" /><div className="panel-body"><p>Entrada: {moveIns}</p><p>Saída: {moveOuts}</p></div></article></section>
  </>;
}
