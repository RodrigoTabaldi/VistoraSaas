'use client';

import Link from 'next/link';
import { useMemo, useState } from 'react';
import { Icon } from './icons';
import { Avatar, ProgressBar, StatusBadge } from './dashboard-primitives';
import { inspections, type InspectionStatus } from '../lib/mock-data';

const statusOptions: Array<'Todos os status' | InspectionStatus> = ['Todos os status', 'Agendada', 'Em andamento', 'Concluída', 'Pendente', 'Atrasada'];

export function InspectionsTable() {
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState<(typeof statusOptions)[number]>('Todos os status');
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list');

  const filteredInspections = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return inspections.filter((inspection) => {
      const matchesStatus = status === 'Todos os status' || inspection.status === status;
      const matchesQuery = !normalizedQuery || [inspection.code, inspection.property, inspection.city, inspection.responsible].some((field) => field.toLowerCase().includes(normalizedQuery));
      return matchesStatus && matchesQuery;
    });
  }, [query, status]);

  return (
    <>
      <div className="list-toolbar">
        <label className="list-search"><Icon name="search" size={18} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar vistorias..." aria-label="Buscar vistorias" /></label>
        <label className="toolbar-select"><select value={status} onChange={(event) => setStatus(event.target.value as (typeof statusOptions)[number])} aria-label="Filtrar por status">{statusOptions.map((option) => <option key={option}>{option}</option>)}</select><Icon name="chevronDown" size={14} /></label>
        <label className="toolbar-select"><select defaultValue="Todos os responsáveis" aria-label="Filtrar por responsável"><option>Todos os responsáveis</option><option>Carla Mendes</option><option>Rafael Lima</option><option>Juliana Costa</option></select><Icon name="chevronDown" size={14} /></label>
        <label className="toolbar-select"><span>01/04/2024 - 30/04/2024</span><Icon name="calendar" size={15} /></label>
        <button className="button button--soft" type="button"><Icon name="download" size={16} /> Exportar</button>
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
              <td className="actions-cell"><button className="icon-button" type="button" aria-label={`Mais ações para ${inspection.code}`}><Icon name="more" size={18} /></button></td>
            </tr>)}</tbody>
          </table>
          <div className="pagination"><span>Mostrando {filteredInspections.length ? 1 : 0} a {filteredInspections.length} de 48 vistorias</span><div className="pagination-controls"><button className="arrow" type="button" aria-label="Página anterior"><Icon name="chevronLeft" size={15} /></button><button className="active" type="button">1</button><button type="button">2</button><button type="button">3</button><button type="button">4</button><button type="button">5</button><span>...</span><button className="arrow" type="button" aria-label="Próxima página"><Icon name="chevronRight" size={15} /></button></div></div>
        </div>
      ) : (
        <div className="inspection-grid-view">
          {filteredInspections.map((inspection) => <Link className="inspection-grid-card" key={inspection.id} href={`/vistorias/${inspection.id}`}><div className="inspection-grid-card__top"><span className="code-cell">{inspection.code}</span><StatusBadge status={inspection.status} /></div><h3>{inspection.property}</h3><p>{inspection.city}</p><div className="inspection-grid-card__meta"><span><Icon name="calendar" size={14} /> {inspection.date}</span><span><Icon name="clock" size={14} /> {inspection.time}</span></div><div className="progress-cell"><ProgressBar value={inspection.progress} complete={inspection.progress === 100} /><span>{inspection.progress}%</span></div></Link>)}
        </div>
      )}
      {filteredInspections.length === 0 && <div className="empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="search" size={27} /></div><h2>Nenhuma vistoria encontrada</h2><p>Ajuste a busca ou o filtro de status para encontrar outros registros.</p></div></div>}
    </>
  );
}
