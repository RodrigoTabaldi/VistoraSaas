'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState, type ChangeEvent } from 'react';
import { Icon } from './icons';
import { PageHeading, StatusBadge } from './dashboard-primitives';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { useInspections } from '../lib/inspection-store';

type Evidence = { id: string; fileName: string; contentType: string; sizeBytes: number };
type InspectionItem = {
  id: string; description: string; response: string | null; notes: string | null;
  position: number; rowVersion: number; evidence: Evidence[];
};
type InspectionRoom = { id: string; name: string; position: number; items: InspectionItem[] };
type Inspection = {
  id: string; unitId: string; type: 'MoveIn' | 'MoveOut'; status: 'Draft' | 'Completed' | 'Approved';
  rowVersion: number; createdAtUtc: string; rooms: InspectionRoom[];
};
type Report = { id: string; version: number; createdAtUtc: string; approvedAtUtc: string | null };
type Download = { url: string };
type Comparison = {
  moveInInspectionId: string;
  differences: Array<{
    room: string; item: string; moveInResponse: string | null; moveOutResponse: string | null;
    moveInNotes: string | null; moveOutNotes: string | null; changed: boolean;
  }>;
};

const responseOptions = ['Não verificado', 'Conforme', 'Atenção', 'Não conforme'];

export function InspectionDetail({ inspectionId }: Readonly<{ inspectionId: string }>) {
  const role = useCurrentUser().role;
  const { getInspection, completeInspection, refresh: refreshInspections, error: storeError } = useInspections();
  const summary = getInspection(inspectionId);
  const [inspection, setInspection] = useState<Inspection | null>(null);
  const [reports, setReports] = useState<Report[]>([]);
  const [comparison, setComparison] = useState<Comparison | null>(null);
  const [comparisonError, setComparisonError] = useState('');
  const [roomName, setRoomName] = useState('');
  const [itemNames, setItemNames] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    const [detail, reportList] = await Promise.all([
      apiRequest<Inspection>(`/api/v1/inspections/${inspectionId}`),
      apiRequest<Report[]>(`/api/v1/inspections/${inspectionId}/reports`),
    ]);
    setInspection(detail);
    setReports(reportList);
    if (detail.type === 'MoveOut') {
      try {
        setComparison(await apiRequest<Comparison>(`/api/v1/inspections/${inspectionId}/comparison`));
        setComparisonError('');
      } catch (cause) {
        setComparison(null);
        setComparisonError(cause instanceof Error ? cause.message : 'Não foi possível comparar as vistorias.');
      }
    } else {
      setComparison(null);
      setComparisonError('');
    }
    setError('');
  }, [inspectionId]);

  useEffect(() => {
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar a vistoria.'))
      .finally(() => setLoading(false));
  }, [refresh]);

  function editItem(itemId: string, field: 'response' | 'notes', value: string) {
    setInspection((current) => current && ({ ...current, rooms: current.rooms.map((room) => ({
      ...room, items: room.items.map((item) => item.id === itemId ? { ...item, [field]: value } : item),
    })) }));
  }

  async function saveItem(item: InspectionItem) {
    setBusy(item.id);
    setError('');
    try {
      await apiRequest(`/api/v1/items/${item.id}`, {
        method: 'PUT',
        body: JSON.stringify({ response: item.response, notes: item.notes, rowVersion: item.rowVersion }),
      });
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível salvar o item.');
    } finally {
      setBusy('');
    }
  }

  async function addRoom(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!inspection) return;
    setBusy('room');
    setError('');
    try {
      const position = Math.max(-1, ...inspection.rooms.map((room) => room.position)) + 1;
      await apiRequest(`/api/v1/inspections/${inspectionId}/rooms`, {
        method: 'POST', body: JSON.stringify({ name: roomName, position }),
      });
      setRoomName('');
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível adicionar o ambiente.');
    } finally {
      setBusy('');
    }
  }

  async function addItem(event: React.FormEvent<HTMLFormElement>, room: InspectionRoom) {
    event.preventDefault();
    setBusy(room.id);
    setError('');
    try {
      const position = Math.max(-1, ...room.items.map((item) => item.position)) + 1;
      await apiRequest(`/api/v1/rooms/${room.id}/items`, {
        method: 'POST', body: JSON.stringify({ description: itemNames[room.id], position }),
      });
      setItemNames((current) => ({ ...current, [room.id]: '' }));
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível adicionar o item.');
    } finally {
      setBusy('');
    }
  }

  async function uploadEvidence(event: ChangeEvent<HTMLInputElement>, itemId: string) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    setBusy(itemId);
    setError('');
    try {
      if (!file.type.startsWith('image/') || file.size > 25 * 1024 * 1024) {
        throw new Error('Selecione uma imagem de até 25 MB.');
      }
      const body = new FormData();
      body.append('file', file);
      await apiRequest(`/api/v1/items/${itemId}/evidence`, { method: 'POST', body });
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível enviar a imagem.');
    } finally {
      setBusy('');
    }
  }

  async function download(path: string) {
    try {
      const result = await apiRequest<Download>(path);
      const link = document.createElement('a');
      link.href = result.url;
      link.target = '_blank';
      link.rel = 'noopener noreferrer';
      link.click();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível abrir o arquivo.');
    }
  }

  async function finishInspection() {
    setBusy('complete');
    await completeInspection(inspectionId);
    try { await refresh(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Não foi possível atualizar a vistoria.'); }
    finally { setBusy(''); }
  }

  async function approveInspection() {
    if (!inspection) return;
    setBusy('approve');
    setError('');
    try {
      await apiRequest(`/api/v1/inspections/${inspectionId}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status: 'Approved', rowVersion: inspection.rowVersion }),
      });
      await Promise.all([refresh(), refreshInspections()]);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível aprovar a vistoria.');
    } finally {
      setBusy('');
    }
  }

  if (loading) return <main role="status">Carregando vistoria...</main>;
  if (!inspection) return <div className="panel empty-state" role="alert">{error || 'Vistoria não encontrada.'}</div>;

  const readOnly = inspection.status !== 'Draft' || (role !== 'Admin' && role !== 'Vistoriador');
  const canApprove = role === 'Admin' && inspection.status === 'Completed' && reports.length > 0;
  const totalItems = inspection.rooms.reduce((sum, room) => sum + room.items.length, 0);
  const answeredItems = inspection.rooms.reduce((sum, room) => sum + room.items.filter((item) => item.response && item.response !== 'Não verificado').length, 0);
  const progress = totalItems ? Math.round(answeredItems / totalItems * 100) : 0;

  return <>
    <div className="breadcrumb"><Link href="/vistorias">Vistorias</Link><Icon name="chevronRight" size={13} /><strong>{summary?.code ?? inspection.id.slice(0, 8)}</strong></div>
    <PageHeading title={summary?.property ?? 'Vistoria'} description={`${inspection.type === 'MoveIn' ? 'Entrada' : 'Saída'} · ${summary?.city ?? ''}`} />
    <div className="page-actions"><StatusBadge status={inspection.status === 'Approved' ? 'Aprovada' : inspection.status === 'Completed' ? 'Concluída' : 'Em andamento'} /><span>{answeredItems} de {totalItems} itens verificados ({progress}%)</span></div>
    {(error || storeError) && <p className="form-feedback form-feedback--error" role="alert">{error || storeError}</p>}
    <section className="room-list" aria-label="Checklist por ambiente">{inspection.rooms.map((room) => <article className="room-card" key={room.id}>
      <div className="room-header"><div className="room-copy"><strong>{room.name}</strong><span>{room.items.length} itens</span></div></div>
      <div className="panel-body">{room.items.map((item) => <div className="panel compact-form" key={item.id}>
        <strong>{item.description}</strong>
        <div className="form-grid"><label className="form-field"><span>Estado</span><select value={item.response ?? 'Não verificado'} onChange={(event) => editItem(item.id, 'response', event.target.value)} disabled={readOnly || Boolean(busy)}>{responseOptions.map((option) => <option key={option}>{option}</option>)}</select></label><label className="form-field"><span>Observações</span><textarea value={item.notes ?? ''} onChange={(event) => editItem(item.id, 'notes', event.target.value)} disabled={readOnly || Boolean(busy)} maxLength={4000} /></label></div>
        {!readOnly && <button className="button button--soft" type="button" disabled={Boolean(busy)} onClick={() => saveItem(item)}>Salvar item</button>}
        <div><strong>Evidências</strong><ul>{item.evidence.map((evidence) => <li key={evidence.id}><button className="panel-link" type="button" onClick={() => download(`/api/v1/evidence/${evidence.id}/download`)}>{evidence.fileName}</button></li>)}</ul>{!readOnly && <label className="form-field"><span>Adicionar foto</span><input type="file" accept="image/*" onChange={(event) => uploadEvidence(event, item.id)} disabled={Boolean(busy)} /></label>}</div>
      </div>)}</div>
      {!readOnly && <form className="compact-form" onSubmit={(event) => addItem(event, room)}><label className="form-field"><span>Novo item</span><input value={itemNames[room.id] ?? ''} onChange={(event) => setItemNames((current) => ({ ...current, [room.id]: event.target.value }))} maxLength={1000} required /></label><button className="button button--soft" type="submit" disabled={Boolean(busy)}>Adicionar item</button></form>}
    </article>)}</section>
    {!readOnly && <form className="panel compact-form" onSubmit={addRoom}><label className="form-field"><span>Novo ambiente</span><input value={roomName} onChange={(event) => setRoomName(event.target.value)} maxLength={200} required /></label><button className="button button--soft" type="submit" disabled={Boolean(busy)}>Adicionar ambiente</button></form>}
    <section className="panel compact-form"><h2>Laudos</h2>{reports.length ? <ul>{reports.map((report) => <li key={report.id}>Versão {report.version} · {new Date(report.createdAtUtc).toLocaleDateString('pt-BR')} <button className="panel-link" type="button" onClick={() => download(`/api/v1/reports/${report.id}/download`)}>Abrir PDF</button></li>)}</ul> : <p>Nenhum laudo disponível. Após concluir, aguarde o processamento e atualize a lista.</p>}<button className="button button--outline" type="button" onClick={() => refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível atualizar os laudos.'))}>Atualizar laudos</button></section>
    {inspection.type === 'MoveOut' && <section className="panel compact-form"><h2>Comparação com a entrada</h2>{comparison ? <><p><Link href={`/vistorias/${comparison.moveInInspectionId}`}>Abrir vistoria de entrada aprovada</Link></p><div className="room-list">{comparison.differences.map((difference, index) => <article className="room-card" key={`${difference.room}-${difference.item}-${index}`}><div className="panel-body"><strong>{difference.room} · {difference.item}</strong><p>Entrada: {difference.moveInResponse ?? 'Não registrado'}{difference.moveInNotes ? ` — ${difference.moveInNotes}` : ''}</p><p>Saída: {difference.moveOutResponse ?? 'Não registrado'}{difference.moveOutNotes ? ` — ${difference.moveOutNotes}` : ''}</p><StatusBadge status={difference.changed ? 'Alterado' : 'Sem alteração'} /></div></article>)}</div></> : <p>{comparisonError || 'Nenhuma vistoria de entrada aprovada foi encontrada para esta unidade.'}</p>}</section>}
    <div className="detail-actions"><Link className="button button--outline" href="/vistorias">Voltar</Link>{!readOnly && <button className="button button--primary" type="button" disabled={Boolean(busy)} onClick={finishInspection}><Icon name="check" size={17} /> Concluir vistoria</button>}{canApprove && <button className="button button--primary" type="button" disabled={Boolean(busy)} onClick={approveInspection}><Icon name="check" size={17} /> Aprovar vistoria</button>}</div>
  </>;
}
