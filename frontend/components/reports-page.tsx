'use client';

import { useEffect, useMemo, useState } from 'react';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';
import { apiRequest } from '../lib/api-client';

type Report = { id: string; inspectionId: string; version: number; createdAtUtc: string };
type Download = { url: string };

export function ReportsPage() {
  const { inspections, hydrated } = useInspections();
  const [reports, setReports] = useState<Report[]>([]);
  const [query, setQuery] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (!hydrated) return;
    let active = true;
    Promise.all(inspections.filter((item) => item.status === 'Concluída').map((item) =>
      apiRequest<Report[]>(`/api/v1/inspections/${item.id}/reports`)))
      .then((groups) => { if (active) { setReports(groups.flat()); setError(''); } })
      .catch((cause: unknown) => { if (active) setError(cause instanceof Error ? cause.message : 'Não foi possível carregar os laudos.'); });
    return () => { active = false; };
  }, [hydrated, inspections]);

  const inspectionById = useMemo(() => new Map(inspections.map((item) => [item.id, item])), [inspections]);
  const filtered = reports.filter((report) => {
    const inspection = inspectionById.get(report.inspectionId);
    return `${inspection?.code ?? ''} ${inspection?.property ?? ''}`.toLowerCase().includes(query.toLowerCase());
  });

  async function download(reportId: string) {
    try {
      const result = await apiRequest<Download>(`/api/v1/reports/${reportId}/download`);
      const link = document.createElement('a');
      link.href = result.url;
      link.target = '_blank';
      link.rel = 'noopener noreferrer';
      link.click();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível abrir o laudo.');
    }
  }

  return <><PageHeading title="Relatórios" description="Laudos disponíveis para as vistorias concluídas." /><div className="page-actions"><label className="inline-search"><Icon name="search" size={17} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar laudo..." aria-label="Buscar laudo" /></label></div>{error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}<section className="report-grid">{filtered.map((report) => {
    const inspection = inspectionById.get(report.inspectionId);
    return <article className="panel report-card" key={report.id}><div className="report-icon"><Icon name="file" size={23} /></div><div className="report-copy"><span>{inspection?.code} · versão {report.version}</span><h2>{inspection?.property ?? 'Imóvel'}</h2><p>Gerado em {new Date(report.createdAtUtc).toLocaleDateString('pt-BR')}</p></div><StatusBadge status="Pronto" /><button className="button button--outline" type="button" onClick={() => download(report.id)}><Icon name="download" size={15} /> Abrir PDF</button></article>;
  })}</section>{hydrated && !filtered.length && <div className="panel empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="file" size={26} /></div><h2>Nenhum laudo pronto</h2><p>Conclua uma vistoria e aguarde a geração do PDF.</p></div></div>}</>;
}
