'use client';

import Link from 'next/link';
import { useState } from 'react';
import { Icon } from './icons';
import { Avatar, ProgressBar, StatusBadge } from './dashboard-primitives';
import type { InspectionStatus } from '../lib/mock-data';
import { csvCell } from '../lib/inspection-presentation';
import { useInspectionPage } from '../lib/use-inspection-page';
import { useInspections } from '../lib/inspection-store';

const statusOptions: Array<'Todos os status' | InspectionStatus> = ['Todos os status', 'Agendada', 'Em andamento', 'Atrasada', 'Concluída'];

export function InspectionsTable({ entityLabel = 'vistorias' }: Readonly<{ entityLabel?: 'triagens' | 'vistorias' }>) {
  const { inspections: revision } = useInspections();
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState<(typeof statusOptions)[number]>('Todos os status');
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list');

  const parameters = new URLSearchParams({ page: String(page), pageSize: String(pageSize), query });
  if (status !== 'Todos os status') parameters.set('status', status);
  const { items: pageItems, total, loading, error } = useInspectionPage(parameters.toString(), revision);
  const filteredInspections = pageItems;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const currentPage = page;
  const hydrated = !loading;

  function exportInspections() {
    const header = ['Código', 'Imóvel', 'Tipo', 'Responsável', 'Criada em', 'Status', 'Progresso'];
    const rows = filteredInspections.map((inspection) => [inspection.code, inspection.property, inspection.type, inspection.responsible, `${inspection.date} ${inspection.time}`, inspection.status, `${inspection.progress}%`]);
    const csv = [header, ...rows].map((row) => row.map(csvCell).join(';')).join('\n');
    const url = URL.createObjectURL(new Blob([`\ufeff${csv}`], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `${entityLabel}-vistora.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  return (
    <>
      <div className="list-toolbar">
        <label className="list-search"><Icon name="search" size={18} /><input maxLength={200} value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} placeholder={`Buscar ${entityLabel}...`} aria-label={`Buscar ${entityLabel}`} /></label>
        <label className="toolbar-select"><select value={status} onChange={(event) => { setStatus(event.target.value as (typeof statusOptions)[number]); setPage(1); }} aria-label="Filtrar por status">{statusOptions.map((option) => <option key={option}>{option}</option>)}</select><Icon name="chevronDown" size={14} /></label>
        <button className="button button--soft" type="button" disabled={loading || !pageItems.length} onClick={exportInspections}><Icon name="download" size={16} /> Exportar página</button>
        <div className="toolbar-spacer" /><Link className="button button--primary" href="/vistorias/nova"><Icon name="plus" size={16} /> Nova vistoria</Link>
        <div className="view-switch" aria-label="Modo de visualização"><button className={viewMode === 'list' ? 'active' : ''} type="button" aria-label="Visualização em lista" onClick={() => setViewMode('list')}><Icon name="list" size={17} /></button><button className={viewMode === 'grid' ? 'active' : ''} type="button" aria-label="Visualização em cards" onClick={() => setViewMode('grid')}><Icon name="grid" size={16} /></button></div>
      </div>
      {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}

      {loading && <p role="status">Carregando vistorias...</p>}
      {!loading && (viewMode === 'list' ? (
        <div className="table-panel">
          <table className="data-table list-table"><thead><tr><th>Código</th><th>Imóvel</th><th>Tipo</th><th>Responsável</th><th>Criada em</th><th>Status</th><th>Progresso</th><th>Ações</th></tr></thead>
            <tbody>{pageItems.map((inspection) => <tr key={inspection.id}>
              <td className="code-cell"><Link href={`/vistorias/${inspection.id}`}>{inspection.code}</Link></td>
              <td className="property-cell"><Link href={`/vistorias/${inspection.id}`}><strong>{inspection.property}</strong><span>{inspection.city}</span></Link></td>
              <td className="type-cell">{inspection.type}</td>
              <td><div className="responsible-cell"><Avatar initials={inspection.responsibleInitials} tone={inspection.responsibleTone} />{inspection.responsible}</div></td>
              <td className="date-cell"><strong>{inspection.date}</strong><span>{inspection.time}</span></td>
              <td><StatusBadge status={inspection.status} /></td>
              <td><div className={`progress-cell ${inspection.progress === 100 ? 'progress-cell--complete' : ''}`}><ProgressBar value={inspection.progress} complete={inspection.progress === 100} /><span>{inspection.progress}%</span></div></td>
              <td className="actions-cell"><div className="table-actions"><Link className="icon-button" href={`/vistorias/${inspection.id}`} aria-label={`Abrir ${inspection.code}`}><Icon name="eye" size={17} /></Link>{inspection.status === 'Concluída' && <span className="table-complete"><Icon name="check" size={14} /></span>}</div></td>
            </tr>)}</tbody>
          </table>

        </div>
      ) : (
        <div className="inspection-grid-view">
          {pageItems.map((inspection) => <Link className="inspection-grid-card" key={inspection.id} href={`/vistorias/${inspection.id}`}><div className="inspection-grid-card__top"><span className="code-cell">{inspection.code}</span><StatusBadge status={inspection.status} /></div><h3>{inspection.property}</h3><p>{inspection.city}</p><div className="inspection-grid-card__meta"><span><Icon name="calendar" size={14} /> {inspection.date}</span><span><Icon name="clock" size={14} /> {inspection.time}</span></div><div className="progress-cell"><ProgressBar value={inspection.progress} complete={inspection.progress === 100} /><span>{inspection.progress}%</span></div></Link>)}
        </div>
      ))}
      {!loading && <div className="pagination"><span>Mostrando {total ? (currentPage - 1) * pageSize + 1 : 0} a {Math.min(currentPage * pageSize, total)} de {total} {entityLabel}</span><div className="pagination-controls"><button className="arrow" type="button" aria-label="Página anterior" disabled={loading || currentPage === 1} onClick={() => setPage(currentPage - 1)}><Icon name="chevronLeft" size={15} /></button><span>Página {currentPage} de {pageCount}</span><button className="arrow" type="button" aria-label="Próxima página" disabled={loading || currentPage >= pageCount} onClick={() => setPage(currentPage + 1)}><Icon name="chevronRight" size={15} /></button></div></div>}
      {hydrated && !error && filteredInspections.length === 0 && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="search" size={27} /></div><h2>Nenhuma {entityLabel === 'triagens' ? 'triagem' : 'vistoria'} encontrada</h2><p>{query || status !== 'Todos os status' ? 'Ajuste a busca ou o filtro de status para encontrar outros registros.' : 'Comece pelo endereço, sem cadastrar um imóvel antes.'}</p></div></div>}
    </>
  );
}
