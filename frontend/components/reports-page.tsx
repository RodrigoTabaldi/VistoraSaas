'use client';

import { useMemo, useState } from 'react';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

function downloadSummary(code: string, property: string, status: string) {
  const content = `VISTORA\nRelatório de inspeção ${code}\nImóvel: ${property}\nStatus: ${status}\nGerado em: ${new Date().toLocaleString('pt-BR')}`;
  const url = URL.createObjectURL(new Blob([content], { type: 'text/plain;charset=utf-8' }));
  const link = document.createElement('a'); link.href = url; link.download = `${code}-resumo.txt`; link.click(); URL.revokeObjectURL(url);
}

export function ReportsPage() {
  const { inspections } = useInspections();
  const [query, setQuery] = useState('');
  const reports = useMemo(() => inspections.filter((item) => item.status.toLowerCase().includes('conclu') && `${item.code} ${item.property}`.toLowerCase().includes(query.toLowerCase())), [inspections, query]);
  return <><PageHeading title="Relatórios" description="Acompanhe os documentos gerados e baixe um resumo de cada inspeção concluída." /><div className="page-actions"><label className="inline-search"><Icon name="search" size={17} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar relatório..." aria-label="Buscar relatório" /></label><button className="button button--soft" type="button" onClick={() => reports.forEach((item) => downloadSummary(item.code, item.property, item.status))}><Icon name="download" size={16} /> Baixar selecionados</button></div><section className="report-grid">{reports.map((item) => <article className="panel report-card" key={item.id}><div className="report-icon"><Icon name="file" size={23} /></div><div className="report-copy"><span>{item.code} · relatório de inspeção</span><h2>{item.property}</h2><p>{item.city} · concluído em {item.date}</p></div><StatusBadge status="Pronto" /><button className="button button--outline" type="button" onClick={() => downloadSummary(item.code, item.property, item.status)}><Icon name="download" size={15} /> Baixar resumo</button></article>)}</section>{!reports.length && <div className="panel empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="file" size={26} /></div><h2>Nenhum relatório pronto</h2><p>Conclua uma triagem para gerar o próximo documento.</p></div></div>}</>;
}
