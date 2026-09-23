'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import { Icon } from './icons';
import { Avatar, PageHeading, StatusBadge } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

export function AgendaPage() {
  const { inspections } = useInspections();
  const [responsible, setResponsible] = useState('Todos');
  const [status, setStatus] = useState('Todos');
  const filtered = useMemo(() => inspections.filter((item) => (responsible === 'Todos' || item.responsible === responsible) && (status === 'Todos' || item.status === status)), [inspections, responsible, status]);
  return <>
    <PageHeading title="Agenda" description="Organize as próximas execuções e distribua a operação entre os vistoriadores." />
    <div className="page-actions"><div className="agenda-period"><button type="button" aria-label="Período anterior"><Icon name="chevronLeft" size={16} /></button><strong>Abril de 2024</strong><button type="button" aria-label="Próximo período"><Icon name="chevronRight" size={16} /></button></div><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={17} /> Agendar triagem</Link></div>
    <section className="agenda-layout"><article className="panel"><div className="agenda-week"><span>SEG<span>15</span></span><span>TER<span>16</span></span><span className="active">QUA<span>17</span></span><span>QUI<span>18</span></span><span>SEX<span>19</span></span><span>SÁB<span>20</span></span><span>DOM<span>21</span></span></div><div className="agenda-toolbar"><label className="toolbar-select"><select value={responsible} onChange={(event) => setResponsible(event.target.value)} aria-label="Filtrar agenda por responsável"><option>Todos</option><option>Carla Mendes</option><option>Rafael Lima</option><option>Juliana Costa</option><option>Lucas Ferreira</option></select><Icon name="chevronDown" size={14} /></label><label className="toolbar-select"><select value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Filtrar agenda por status"><option>Todos</option><option>Agendada</option><option>Em andamento</option><option>Pendente</option><option>Atrasada</option></select><Icon name="chevronDown" size={14} /></label></div><div className="agenda-list">{filtered.map((item) => <Link className="agenda-row" href={`/vistorias/${item.id}`} key={item.id}><span className="agenda-time">{item.time}</span><span className="agenda-marker" /><span className="agenda-row-copy"><strong>{item.property}</strong><span>{item.city} · {item.type}</span></span><Avatar initials={item.responsibleInitials} tone={item.responsibleTone} /><span className="agenda-responsible">{item.responsible}</span><StatusBadge status={item.status} /><Icon name="chevronRight" size={16} /></Link>)}</div></article><aside className="panel agenda-side"><h2 className="panel-title">Resumo do dia</h2><div className="agenda-summary"><strong>{filtered.length}</strong><span>triagens na agenda</span></div><p>Use os filtros para acompanhar a carga de cada vistoriador e abrir o checklist da execução.</p><Link className="panel-link" href="/triagens">Ver todas as triagens <Icon name="arrowRight" size={15} /></Link></aside></section>
  </>;
}
