'use client';

import { useEffect, useState } from 'react';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { apiRequest } from '../lib/api-client';

type Report = { id: string; inspectionId: string; version: number; createdAtUtc: string; propertyName: string };
type Download = { url: string };

export function ReportsPage() {
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [reports, setReports] = useState<Report[]>([]);
  const [query, setQuery] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true);
    const parameters = new URLSearchParams({ page: String(page), pageSize: '20', query });
    apiRequest<{ items: Report[]; total: number }>(`/api/v1/reports?${parameters}`)
      .then((result) => { if (active) { setReports(result.items); setTotal(result.total); setError(''); } })
      .catch((cause: unknown) => { if (active) { setReports([]); setTotal(0); setError(cause instanceof Error ? cause.message : 'Falha ao carregar os laudos.'); } })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [page, query]);

  const filtered = reports;
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

  return <><PageHeading title="Relatórios" description="Laudos disponíveis para as vistorias concluídas." /><div className="page-actions"><label className="inline-search"><Icon name="search" size={17} /><input maxLength={200} value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} placeholder="Buscar laudo..." aria-label="Buscar laudo" /></label></div>{error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}{loading && <p role="status">Carregando laudos...</p>}<section className="report-grid">{!loading && filtered.map((report) => {
    return <article className="panel report-card" key={report.id}><div className="report-icon"><Icon name="file" size={23} /></div><div className="report-copy"><span>{`VIS-${report.inspectionId.slice(0, 8).toUpperCase()}`} · versão {report.version}</span><h2>{report.propertyName}</h2><p>Gerado em {new Date(report.createdAtUtc).toLocaleDateString('pt-BR')}</p></div><StatusBadge status="Pronto" /><button className="button button--outline" type="button" onClick={() => download(report.id)}><Icon name="download" size={15} /> Abrir PDF</button></article>;
  })}</section>{!loading && !error && !filtered.length && <div className="panel empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="file" size={26} /></div><h2>Nenhum laudo pronto</h2><p>Conclua uma vistoria e aguarde a geração do PDF.</p></div></div>}<div className="pagination"><span>{total} laudos · página {page}</span><div className="pagination-controls"><button type="button" disabled={loading || page === 1} onClick={() => setPage(page - 1)}>Anterior</button><button type="button" disabled={loading || page * 20 >= total} onClick={() => setPage(page + 1)}>Próxima</button></div></div></>;
}
