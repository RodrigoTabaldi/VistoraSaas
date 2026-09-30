'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { Icon } from './icons';
import { PageHeading, PanelHeader } from './dashboard-primitives';

type Room = { name: string; items: string[] };
type Template = { id: string; name: string; isActive: boolean; rowVersion: number; rooms: Array<{ name: string; position: number; items: Array<{ description: string; position: number }> }> };

function emptyRoom(): Room { return { name: '', items: [''] }; }

export function ChecklistTemplateEditor() {
  const { role } = useCurrentUser();
  const [templates, setTemplates] = useState<Template[]>([]);
  const [selectedId, setSelectedId] = useState('new');
  const [name, setName] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [rooms, setRooms] = useState<Room[]>([emptyRoom()]);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async (chooseId?: string) => {
    const list = await apiRequest<Template[]>('/api/v1/checklist-templates/manage');
    setTemplates(list);
    const selected = chooseId ? list.find((template) => template.id === chooseId) : undefined;
    if (selected) {
      setSelectedId(selected.id);
      setName(selected.name);
      setIsActive(selected.isActive);
      setRooms(selected.rooms.map((room) => ({ name: room.name, items: room.items.map((item) => item.description) })));
    }
  }, []);

  useEffect(() => {
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar os modelos.'));
  }, [refresh]);

  function selectTemplate(id: string) {
    setSelectedId(id);
    const selected = templates.find((template) => template.id === id);
    setError('');
    setSaved(false);
    if (!selected) {
      setName('');
      setIsActive(true);
      setRooms([emptyRoom()]);
      return;
    }
    setName(selected.name);
    setIsActive(selected.isActive);
    setRooms(selected.rooms.map((room) => ({ name: room.name, items: room.items.map((item) => item.description) })));
  }

  function updateRoom(roomIndex: number, next: Room) {
    setRooms((current) => current.map((room, index) => index === roomIndex ? next : room));
  }

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!rooms.length || rooms.some((room) => !room.name.trim() || !room.items.some((item) => item.trim()))) {
      setError('Cada modelo precisa ter ambientes e ao menos um item preenchido por ambiente.');
      return;
    }
    const roomPayload = rooms.map((room, position) => ({
      name: room.name.trim(), position,
      items: room.items.filter((item) => item.trim()).map((description, itemPosition) => ({ description: description.trim(), position: itemPosition })),
    }));
    setBusy(true);
    setError('');
    setSaved(false);
    try {
      if (selectedId === 'new') {
        const created = await apiRequest<{ checklistTemplateId: string }>('/api/v1/checklist-templates', {
          method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() },
          body: JSON.stringify({ name, rooms: roomPayload }),
        });
        await refresh(created.checklistTemplateId);
      } else {
        const selected = templates.find((template) => template.id === selectedId);
        if (!selected) throw new Error('Selecione um modelo válido.');
        await apiRequest(`/api/v1/checklist-templates/${selectedId}`, {
          method: 'PUT',
          body: JSON.stringify({ name, isActive, rowVersion: selected.rowVersion, rooms: roomPayload }),
        });
        await refresh(selectedId);
      }
      setSaved(true);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível salvar o modelo.');
    } finally {
      setBusy(false);
    }
  }

  if (role !== 'Admin') return <><PageHeading title="Modelos de checklist" description="Somente administradores podem editar modelos." /></>;

  return <>
    <PageHeading title="Modelos de checklist" description="Edite ambientes e itens usados pelas próximas vistorias. Checklists já criados preservam sua cópia original." />
    {(error || saved) && <p className={`form-feedback ${error ? 'form-feedback--error' : 'form-feedback--success'}`} role={error ? 'alert' : 'status'}>{error || 'Modelo salvo.'}</p>}
    <div className="template-editor-layout">
      <aside className="panel template-list">
        <PanelHeader title="Modelos" action={<button className="button button--soft" type="button" onClick={() => selectTemplate('new')}><Icon name="plus" size={15} /> Novo</button>} />
        {templates.map((template) => <button className={`template-list-item ${selectedId === template.id ? 'active' : ''}`} type="button" key={template.id} onClick={() => selectTemplate(template.id)}><span>{template.name}</span><small>{template.isActive ? 'Ativo' : 'Inativo'}</small></button>)}
        {templates.length === 0 && <p className="panel-body">Ainda não há modelos salvos.</p>}
      </aside>
      <form className="panel compact-form template-editor" onSubmit={save}>
        <PanelHeader title={selectedId === 'new' ? 'Novo modelo' : 'Editar modelo'} />
        <div className="form-grid">
          <label className="form-field form-field--full"><span>Nome do modelo</span><input value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required /></label>
          {selectedId !== 'new' && <label className="template-active-toggle"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} /> Disponível para novas vistorias</label>}
        </div>
        <div className="template-rooms">
          {rooms.map((room, roomIndex) => <section className="template-room-editor" key={roomIndex}>
            <div className="template-room-heading"><h3>Ambiente {roomIndex + 1}</h3><button className="button button--outline" type="button" onClick={() => setRooms((current) => current.filter((_, index) => index !== roomIndex))}>Remover ambiente</button></div>
            <label className="form-field"><span>Nome do ambiente</span><input value={room.name} onChange={(event) => updateRoom(roomIndex, { ...room, name: event.target.value })} maxLength={200} required /></label>
            <div className="template-items">
              {room.items.map((item, itemIndex) => <label className="form-field template-item-field" key={itemIndex}><span>Item {itemIndex + 1}</span><input value={item} onChange={(event) => updateRoom(roomIndex, { ...room, items: room.items.map((value, index) => index === itemIndex ? event.target.value : value) })} maxLength={1000} required /><button className="button button--outline" type="button" disabled={room.items.length === 1} onClick={() => updateRoom(roomIndex, { ...room, items: room.items.filter((_, index) => index !== itemIndex) })}>Remover</button></label>)}
              <button className="button button--soft" type="button" onClick={() => updateRoom(roomIndex, { ...room, items: [...room.items, ''] })}><Icon name="plus" size={15} /> Adicionar item</button>
            </div>
          </section>)}
        </div>
        <div className="form-actions template-actions"><button className="button button--outline" type="button" onClick={() => setRooms((current) => [...current, emptyRoom()])}><Icon name="plus" size={15} /> Adicionar ambiente</button><button className="button button--primary" type="submit" disabled={busy}>{busy ? 'Salvando...' : 'Salvar modelo'}</button></div>
      </form>
    </div>
  </>;
}
