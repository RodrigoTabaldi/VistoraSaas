'use client';

import { useMemo, useState, type ChangeEvent } from 'react';
import { Icon, type IconName } from './icons';
import { DonutChart, LegendRow, ProgressBar, StatusBadge } from './dashboard-primitives';
import { detailRooms, type ChecklistStatus } from '../lib/mock-data';
import { useInspections } from '../lib/inspection-store';
import { useWorkspace } from '../lib/workspace-store';

const tabs = ['Checklist', 'Fotos', 'Observações', 'Histórico'] as const;

function roomProgress(verified: number, total: number) {
  return Math.round((verified / total) * 100);
}

function PhotosPanel({ inspectionId, items }: Readonly<{ inspectionId: string; items: Array<{ name: string }> }>) {
  const { addPhoto, getPhotos, removePhoto } = useWorkspace();
  const [target, setTarget] = useState(items[0]?.name ?? 'Geral');
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const inputId = `photo-upload-${inspectionId}`;
  const photos = getPhotos(inspectionId);

  async function handleFiles(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    event.target.value = '';
    if (!files.length) return;
    if (files.some((file) => !file.type.startsWith('image/') || file.size > 5 * 1024 * 1024)) {
      setError('Selecione apenas imagens de até 5 MB.');
      return;
    }
    setError('');
    setUploading(true);
    try {
      for (const file of files) await addPhoto({ inspectionId, itemName: target, file });
    } catch {
      setError('Não foi possível adicionar uma das imagens.');
    } finally {
      setUploading(false);
    }
  }

  return <section className="photo-workspace">
    <div className="photo-toolbar panel"><div><h2 className="panel-title">Evidências fotográficas</h2><p>Associe cada foto ao item correspondente do checklist.</p></div><div className="photo-toolbar-actions"><label className="form-field photo-target"><span>Item da evidência</span><select value={target} onChange={(event) => setTarget(event.target.value)}>{items.map((item) => <option key={item.name}>{item.name}</option>)}</select></label><label className={`button button--primary ${uploading ? 'is-loading' : ''}`} htmlFor={inputId}><Icon name="image" size={17} /> {uploading ? 'Adicionando...' : 'Adicionar fotos'}</label><input id={inputId} className="sr-only" type="file" accept="image/*" multiple onChange={handleFiles} /></div></div>
    {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
    {photos.length ? <div className="photo-gallery">{photos.map((photo) => <article className="photo-card" key={photo.id}><img src={photo.dataUrl} alt={`${photo.itemName}: ${photo.fileName}`} /><div className="photo-card-copy"><strong>{photo.itemName}</strong><span>{photo.fileName}</span></div><button className="icon-button photo-remove" type="button" aria-label={`Remover ${photo.fileName}`} onClick={() => removePhoto(photo.id)}><Icon name="x" size={16} /></button></article>)}</div> : <div className="panel empty-state photo-empty"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name="image" size={27} /></div><h2>Nenhuma foto adicionada</h2><p>Escolha o item do checklist e adicione as evidências da vistoria.</p><label className="button button--outline" htmlFor={inputId}>Selecionar imagens</label></div></div>}
  </section>;
}

export function InspectionDetail({ inspectionId }: Readonly<{ inspectionId: string }>) {
  const { getInspection, completeInspection } = useInspections();
  const { getItemDraft, updateItemDraft, getPhotos } = useWorkspace();
  const inspection = getInspection(inspectionId);
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Checklist');
  const [openRooms, setOpenRooms] = useState<string[]>(['Sala']);
  const [itemStatuses, setItemStatuses] = useState<Record<string, ChecklistStatus>>({});
  const completed = inspection?.status.toLowerCase().includes('conclu') ?? false;
  const allItems = useMemo(() => detailRooms.flatMap((room) => room.items), []);
  const statusFor = (itemName: string, fallback: ChecklistStatus) => getItemDraft(inspectionId, itemName).status ?? itemStatuses[itemName] ?? fallback;
  const counts = allItems.reduce((result, item) => {
    result[statusFor(item.name, item.status)] += 1;
    return result;
  }, { Conforme: 0, Atenção: 0, 'Não conforme': 0, 'Não verificado': 0 } as Record<ChecklistStatus, number>);

  function toggleRoom(roomName: string) {
    setOpenRooms((current) => current.includes(roomName) ? current.filter((name) => name !== roomName) : [...current, roomName]);
  }

  function cycleStatus(itemName: string, currentStatus: ChecklistStatus) {
    const sequence: ChecklistStatus[] = ['Conforme', 'Atenção', 'Não conforme', 'Não verificado'];
    const next = sequence[(sequence.indexOf(currentStatus) + 1) % sequence.length];
    setItemStatuses((current) => ({ ...current, [itemName]: next }));
    updateItemDraft(inspectionId, itemName, { status: next });
  }

  return <>
    <div className="detail-tabs">{tabs.map((tab) => <button key={tab} className={`detail-tab ${activeTab === tab ? 'active' : ''}`} type="button" onClick={() => setActiveTab(tab)}>{tab}</button>)}<div className="detail-tab-actions"><button className="button button--outline" type="button" onClick={() => setOpenRooms(detailRooms.map((room) => room.name))}><Icon name="chevronDown" size={15} /> Expandir todos</button></div></div>
    {activeTab === 'Fotos' ? <PhotosPanel inspectionId={inspectionId} items={allItems} /> : activeTab === 'Histórico' || activeTab === 'Observações' ? <div className="panel empty-state" style={{ marginTop: 15 }}><div className="empty-state-inner"><div className="empty-state-icon"><Icon name={activeTab === 'Histórico' ? 'clock' : 'file'} size={27} /></div><h2>{activeTab}</h2><p>{activeTab === 'Observações' ? 'As observações ficam salvas por item no checklist. Abra a aba Checklist para editar.' : 'O histórico será preenchido pelos eventos da inspeção quando a API estiver conectada.'}</p></div></div> : (
      <div className="detail-layout">
        <section className="room-list" aria-label="Checklist por ambiente">{detailRooms.map((room) => { const progress = roomProgress(room.verified, room.total); const isOpen = openRooms.includes(room.name); return <article className="room-card" key={room.name}>
          <div className="room-header"><span className="room-icon"><Icon name={room.icon as IconName} size={19} /></span><div className="room-copy"><strong>{room.name}</strong><span>{room.total} itens&nbsp; · &nbsp;{room.verified} verificados</span></div><div className="room-progress"><ProgressBar value={progress} /><small>{progress}%</small></div><button className="room-toggle" type="button" aria-label={`${isOpen ? 'Recolher' : 'Expandir'} ${room.name}`} onClick={() => toggleRoom(room.name)}><Icon name={isOpen ? 'chevronDown' : 'chevronRight'} size={18} /></button></div>
          {isOpen && room.items.length > 0 && <table className="checklist-table"><thead><tr><th>Item</th><th>Estado</th><th>Fotos</th><th>Observações</th><th /></tr></thead><tbody>{room.items.map((item) => { const draft = getItemDraft(inspectionId, item.name); const status = statusFor(item.name, item.status); const statusClass = status.replace(' ', '-'); const photoCount = getPhotos(inspectionId, item.name).length; return <tr key={item.name}><td className="check-item-name">{item.name}</td><td><button className={`item-status item-status--${statusClass}`} type="button" onClick={() => cycleStatus(item.name, status)} title="Clique para alterar o estado"><span className="item-status-dot"><Icon name={status === 'Conforme' ? 'check' : status === 'Não conforme' ? 'x' : status === 'Atenção' ? 'warning' : 'clock'} size={12} /></span><span>{status}</span></button></td><td><button className="evidence-count" type="button" onClick={() => setActiveTab('Fotos')}><span className={`evidence-thumb evidence-thumb--${item.photoTone}`} />{photoCount > 0 && <small>{photoCount}</small>}</button></td><td className="item-note"><textarea value={draft.notes ?? item.note} onChange={(event) => updateItemDraft(inspectionId, item.name, { notes: event.target.value })} aria-label={`Observações de ${item.name}`} /></td><td className="row-more"><button className="icon-button" type="button" aria-label={`Mais ações para ${item.name}`} onClick={() => setActiveTab('Fotos')}><Icon name="more" size={17} /></button></td></tr>; })}</tbody></table>}
        </article>; })}</section>
        <aside className="summary-sidebar">
          <article className="panel summary-panel"><div className="panel-header"><h2 className="panel-title">Resumo da vistoria</h2><StatusBadge status={completed ? 'Concluída' : 'Em execução'} /></div><DonutChart value={completed ? 100 : 68} label={completed ? '100%' : '68%'} unit="concluído"><div className="legend"><LegendRow label="Conformes" value={counts.Conforme} percent={Math.round((counts.Conforme / Math.max(allItems.length, 1)) * 100)} color="green" /><LegendRow label="Atenção" value={counts.Atenção} percent={Math.round((counts.Atenção / Math.max(allItems.length, 1)) * 100)} color="yellow" /><LegendRow label="Não conformes" value={counts['Não conforme']} percent={Math.round((counts['Não conforme'] / Math.max(allItems.length, 1)) * 100)} color="red" /><LegendRow label="Não verificados" value={counts['Não verificado']} percent={Math.round((counts['Não verificado'] / Math.max(allItems.length, 1)) * 100)} color="gray" /></div></DonutChart><div className="chart-callout"><span className="dashboard-callout-icon"><Icon name="chart" size={19} /></span><span><strong>{counts.Atenção + counts['Não conforme']} itens precisam de atenção</strong><br />Revise os itens não conformes e com atenção.</span><Icon className="callout-arrow" name="chevronRight" size={17} /></div></article>
          <article className="panel summary-panel"><div className="panel-header"><h2 className="panel-title">Itens por ambiente</h2></div><div className="mini-bars">{detailRooms.map((room) => <div className="hbar-row" key={room.name}><span><Icon name={room.icon as IconName} size={14} /> {room.name}</span><div className="progress-line"><span style={{ width: `${roomProgress(room.verified, room.total)}%`, background: roomProgress(room.verified, room.total) < 60 ? '#ffc735' : '#0c9e69' }} /></div><span className="hbar-value">{room.verified} / {room.total}</span></div>)}</div></article>
          <article className="panel summary-panel"><div className="panel-header"><h2 className="panel-title">Severidade dos problemas</h2></div><div className="severity-grid"><div className="severity severity--high"><Icon name="x" size={21} /><strong>2</strong><span>Comprometem<br />o uso do imóvel</span></div><div className="severity severity--medium"><Icon name="warning" size={21} /><strong>7</strong><span>Necessitam de<br />reparo</span></div><div className="severity severity--low"><Icon name="sparkles" size={21} /><strong>3</strong><span>Ajustes<br />estéticos</span></div></div></article>
          <div className="detail-actions"><button className="button button--outline" type="button" onClick={() => window.history.back()}><Icon name="chevronLeft" size={16} /> Voltar</button><button className="button button--outline" type="button" onClick={() => setActiveTab('Observações')}><Icon name="file" size={16} /> Salvar rascunho</button><button className="button button--primary" type="button" disabled={completed} onClick={() => completeInspection(inspectionId)}><Icon name="check" size={17} /> {completed ? 'Triagem concluída' : 'Concluir triagem'}</button></div>
        </aside>
      </div>
    )}
  </>;
}
