'use client';

import Link from 'next/link';
import { useMemo, useState } from 'react';
import { Icon } from './icons';
import { Avatar, ProgressBar, StatusBadge } from './dashboard-primitives';
import type { InspectionStatus } from '../lib/mock-data';
import { useInspections } from '../lib/inspection-store';

const statusOptions: Array<'Todos os status' | InspectionStatus> = ['Todos os status', 'Agendada', 'Em andamento', 'Concluída', 'Pendente', 'Atrasada'];

export function InspectionsTable({ entityLabel = 'vistorias' }: Readonly<{ entityLabel?: 'triagens' | 'vistorias' }>) {
  const { inspections, completeInspection } = useInspections();
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState<(typeof statusOptions)[number]>('Todos os status');
  const [responsible, setResponsible] = useState('Todos os responsáveis');
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list');

  const filteredInspections = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return inspections.filter((inspection) => {
      const matchesStatus = status === 'Todos os status' || inspection.status === status;
      const matchesResponsible = responsible === 'Todos os responsáveis' || inspection.responsible === responsible;
      const matchesQuery = !normalizedQuery || [inspection.code, inspection.property, inspection.city, inspection.responsible].some((field) => field.toLowerCase().includes(normalizedQuery));
      return matchesStatus && matchesResponsible && matchesQuery;
    });
  }, [inspections, query, responsible, status]);

  function exportInspections() {
    const header = ['Código', 'Imóvel', 'Tipo', 'Responsável', 'Data', 'Status', 'Progresso'];
    const rows = filteredInspections.map((inspection) => [inspection.code, inspection.property, inspection.type, inspection.responsible, `${inspection.date} ${inspection.time}`, inspection.status, `${inspection.progress}%`]);
    const csv = [header, ...rows].map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(';')).join('\n');
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
        <label className="list-search"><Icon name="search" size={18} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder={`Buscar ${entityLabel}...`} aria-label={`Buscar ${entityLabel}`} /></label>
        <label className="toolbar-select"><select value={status} onChange={(event) => setStatus(event.target.value as (typeof statusOptions)[number])} aria-label="Filtrar por status">{statusOptions.map((option) => <option key={option}>{option}</option>)}</select><Icon name="chevronDown" size={14} /></label>
        <label className="toolbar-select"><select value={responsible} onChange={(event) => setResponsible(event.target.value)} aria-label="Filtrar por responsável"><option>Todos os responsáveis</option><option>Carla Mendes</option><option>Rafael Lima</option><option>Juliana Costa</option></select><Icon name="chevronDown" size={14} /></label>
        <label className="toolbar-select"><span>01/04/2024 - 30/04/2024</span><Icon name="calendar" size={15} /></label>
        <button className="button button--soft" type="button" onClick={exportInspections}><Icon name="download" size={16} /> Exportar</button>
        <div className="toolbar-spacer" />
        <div className="view-switch" aria-label="Modo de visualização"><button className={viewMode === 'list' ? 'active' : ''} type="button" aria-label="Visualização em lista" onClick={() => setViewMode('list')}><Icon name="list" size={17} /></button><button className={viewMode === 'grid' ? 'active' : ''} type="button" aria-label="Visualização em cards" onClick={() => setViewMode('grid')}><Icon name="grid" size={16} /></button></div>
      </div>

      {viewMode === 'list' ? (
        <div className="table-panel">
          <table className="data-table list-table"><thead><tr><th><input className="row-check" type="checkbox" aria-label="Selecionar todas" /></th><th>Código</th><th>Imóvel</th><th>Tipo</th><th>Responsável</th><th>Data</th><th>Status</th><th>Progresso</th><th>Ações</th></tr></thead>
            <tbody>{filteredInspections.map((inspection) => <tr key={inspection.id}>
              <td><input className="row-check" type="checkbox" aria-label={`Selecionar ${inspection.code}`} /></td>
              <td className="code-cell"><Link href={`/vistorias/${inspection.id}`}>{inspection.code}</Link></td>
              <td className="property-cell"><Link href={`/vistorias/${inspection.id}`}><strong>{inspection.property}</strong><span>{inspection.city}</span></Link></td>
              <td className="type-cell">{inspection.type}</td>
              <td><div className="responsible-cell"><Avatar initials={inspection.responsibleInitials} tone={inspection.responsibleTone} />{inspection.responsible}</div></td>
              <td className="date-cell"><strong>{inspection.date}</strong><span>{inspection.time}</span></td>
              <td><StatusBadge status={inspection.status} /></td>
              <td><div className={`progress-cell ${inspection.progress === 100 ? 'progress-cell--complete' : ''}`}><ProgressBar value={inspection.progress} complete={inspection.progress === 100} /><span>{inspection.progress}%</span></div></td>
              <td className="actions-cell"><div className="table-actions"><Link className="icon-button" href={`/vistorias/${inspection.id}`} aria-label={`Abrir ${inspection.code}`}><Icon name="eye" size={17} /></Link>{inspection.status.toLowerCase().includes('conclu') ? <span className="table-complete"><Icon name="check" size={14} /></span> : <button className="icon-button icon-button--success" type="button" aria-label={`Concluir ${inspection.code}`} onClick={() => completeInspection(inspection.id)}><Icon name="check" size={17} /></button>}</div></td>
            </tr>)}</tbody>
          </table>
          <div className="pagination"><span>Mostrando {filteredInspections.length ? 1 : 0} a {filteredInspections.length} de {filteredInspections.length} {entityLabel}</span><div className="pagination-controls"><button className="arrow" type="button" aria-label="Página anterior" disabled><Icon name="chevronLeft" size={15} /></button><button className="active" type="button">1</button><button className="arrow" type="button" aria-label="Próxima página" disabled><Icon name="chevronRight" size={15} /></button></div></div>
        </div>
      ) : (
        <div className="inspection-grid-view">
          {filteredInspections.map((inspection) => <Link className="inspection-grid-card" key={inspection.id} href={`/vistorias/${inspection.id}`}><div className="inspection-grid-card__top"><span className="code-cell">{inspection.code}</span><StatusBadge status={inspection.status} /></div><h3>{inspection.property}</h3><p>{inspection.city}</p><div className="inspection-grid-card__meta"><span><Icon name="calendar" size={14} /> {inspection.date}</span><span><Icon name="clock" size={14} /> {inspection.time}</span></div><div className="progress-cell"><ProgressBar value={inspection.progress} complete={inspection.progress === 100} /><span>{inspection.progress}%</span></div></Link>)}
        </div>
      )}
      {filteredInspections.length === 0 && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="search" size={27} /></div><h2>Nenhuma {entityLabel === 'triagens' ? 'triagem' : 'vistoria'} encontrada</h2><p>Ajuste a busca ou o filtro de status para encontrar outros registros.</p></div></div>}
    </>
  );
}
