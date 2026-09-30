'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { Icon } from './icons';
import { PageHeading, PanelHeader } from './dashboard-primitives';

type OrganizationSettings = { id: string; name: string; document: string | null; operationalEmail: string | null };

export function SettingsPage() {
  const { role } = useCurrentUser();
  const [settings, setSettings] = useState<OrganizationSettings | null>(null);
  const [name, setName] = useState('');
  const [document, setDocument] = useState('');
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    apiRequest<OrganizationSettings>('/api/v1/organization')
      .then((value) => {
        setSettings(value);
        setName(value.name);
        setDocument(value.document ?? '');
        setEmail(value.operationalEmail ?? '');
      })
      .catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar a organização.'));
  }, []);

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setSaved(false);
    try {
      const value = await apiRequest<OrganizationSettings>('/api/v1/organization', {
        method: 'PATCH',
        body: JSON.stringify({ name, document: document || null, operationalEmail: email || null }),
      });
      setSettings(value);
      setSaved(true);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível salvar as configurações.');
    } finally {
      setBusy(false);
    }
  }

  const canEdit = role === 'Admin';
  return <>
    <PageHeading title="Configurações" description="Dados persistidos da organização e modelos usados nas vistorias." />
    {(error || saved) && <p className={`form-feedback ${error ? 'form-feedback--error' : 'form-feedback--success'}`} role={error ? 'alert' : 'status'}>{error || 'Dados salvos na organização.'}</p>}
    <div className="settings-layout">
      <form className="panel settings-form" onSubmit={save}>
        <PanelHeader title="Dados da organização" />
        {!settings && !error && <p role="status">Carregando configurações...</p>}
        <div className="form-grid">
          <label className="form-field form-field--full"><span>Nome da organização</span><input value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required disabled={!canEdit || busy} /></label>
          <label className="form-field"><span>CNPJ ou documento</span><input value={document} onChange={(event) => setDocument(event.target.value)} maxLength={32} disabled={!canEdit || busy} /></label>
          <label className="form-field"><span>E-mail operacional</span><input type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={320} disabled={!canEdit || busy} /></label>
        </div>
        <div className="form-actions">
          {!canEdit && <span className="panel-caption">Somente administradores podem editar.</span>}
          {canEdit && <button className="button button--primary" type="submit" disabled={busy || !settings}><Icon name="check" size={16} /> {busy ? 'Salvando...' : 'Salvar alterações'}</button>}
        </div>
      </form>
      <aside className="settings-nav panel">
        <PanelHeader title="Gerenciar" />
        <Link className="settings-nav-item" href="/modelos-checklist"><Icon name="clipboard" size={17} /> Modelos de checklist</Link>
        <Link className="settings-nav-item" href="/equipe"><Icon name="users" size={17} /> Equipe e convites</Link>
        <div className="settings-note"><Icon name="shield" size={18} /><span><strong>Dados protegidos</strong>As evidências ficam em armazenamento privado e vinculado à organização.</span></div>
      </aside>
    </div>
  </>;
}
