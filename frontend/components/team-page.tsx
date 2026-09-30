'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiRequest } from '../lib/api-client';
import { useCurrentUser } from '../lib/auth-context';
import { Icon } from './icons';
import { Avatar, PageHeading, PanelHeader, StatusBadge } from './dashboard-primitives';

type Role = 'Admin' | 'Vistoriador' | 'Leitor';
type Member = { id: string; name: string; email: string; role: Role };
type Invitation = { id: string; email: string; role: Role; expiresAtUtc: string; status: 'Pending' | 'Expired' };
type Team = { members: Member[]; invitations: Invitation[] };

const roleNames: Record<Role, string> = { Admin: 'Administrador', Vistoriador: 'Vistoriador', Leitor: 'Leitor' };

export function TeamPage() {
  const { role: currentRole } = useCurrentUser();
  const [team, setTeam] = useState<Team>({ members: [], invitations: [] });
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<Role>('Vistoriador');
  const [inviteLink, setInviteLink] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async () => {
    setTeam(await apiRequest<Team>('/api/v1/team'));
  }, []);

  useEffect(() => {
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar a equipe.'));
  }, [refresh]);

  async function createInvitation(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setMessage('');
    setInviteLink('');
    try {
      const result = await apiRequest<{ token: string }>('/api/v1/team/invitations', {
        method: 'POST',
        body: JSON.stringify({ email, role }),
      });
      setInviteLink(`${window.location.origin}/convite?token=${encodeURIComponent(result.token)}`);
      setEmail('');
      setMessage('Convite criado. Copie o link e envie à pessoa convidada.');
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível criar o convite.');
    } finally {
      setBusy(false);
    }
  }

  async function revokeInvitation(id: string) {
    setBusy(true);
    setError('');
    try {
      await apiRequest(`/api/v1/team/invitations/${id}`, { method: 'DELETE' });
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível cancelar o convite.');
    } finally {
      setBusy(false);
    }
  }

  async function copyInvitationLink() {
    try {
      if (!navigator.clipboard) throw new Error('Clipboard unavailable');
      await navigator.clipboard.writeText(inviteLink);
      setMessage('Link copiado.');
    } catch {
      setError('Não foi possível copiar o link; selecione e copie o campo.');
    }
  }

  if (currentRole !== 'Admin') return <><PageHeading title="Equipe" description="Somente administradores podem gerenciar membros e convites." /></>;

  return <>
    <PageHeading title="Equipe" description="Gerencie os acessos da organização e convide novas pessoas." />
    {(error || message) && <p className={`form-feedback ${error ? 'form-feedback--error' : 'form-feedback--success'}`} role={error ? 'alert' : 'status'}>{error || message}</p>}
    <form className="panel compact-form" onSubmit={createInvitation}>
      <PanelHeader title="Convidar pessoa" />
      <div className="form-grid">
        <label className="form-field"><span>E-mail</span><input type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={320} required /></label>
        <label className="form-field"><span>Perfil</span><select value={role} onChange={(event) => setRole(event.target.value as Role)}><option value="Vistoriador">Vistoriador</option><option value="Leitor">Leitor</option><option value="Admin">Administrador</option></select></label>
      </div>
      {inviteLink && <label className="form-field invite-link-field"><span>Link de uso único · válido por 7 dias</span><input readOnly value={inviteLink} onFocus={(event) => event.currentTarget.select()} /></label>}
      <div className="form-actions">
        {inviteLink && <button className="button button--outline" type="button" onClick={copyInvitationLink}><Icon name="link" size={16} /> Copiar link</button>}
        <button className="button button--primary" type="submit" disabled={busy}><Icon name="plus" size={16} /> {busy ? 'Criando...' : 'Criar convite'}</button>
      </div>
    </form>
    <section className="panel"><PanelHeader title="Membros" /><div className="team-list">
      {team.members.map((member, index) => <article className="team-row" key={member.id}>
        <Avatar initials={member.name.trim().split(/\s+/).slice(0, 2).map((part) => part[0]?.toUpperCase()).join('')} tone={['blue', 'rose', 'purple', 'slate'][index % 4]} large />
        <div className="team-copy"><strong>{member.name}</strong><span>{member.email}</span></div>
        <span className="team-role">{roleNames[member.role] ?? member.role}</span><StatusBadge status="Ativo" />
      </article>)}
      {team.members.length === 0 && <p className="panel-body">Nenhum membro encontrado.</p>}
    </div></section>
    <section className="panel"><PanelHeader title="Convites" /><div className="team-list">
      {team.invitations.map((invitation) => <article className="team-row" key={invitation.id}>
        <Avatar initials="?" tone="slate" large />
        <div className="team-copy"><strong>{invitation.email}</strong><span>Expira em {new Date(invitation.expiresAtUtc).toLocaleDateString('pt-BR')}</span></div>
        <span className="team-role">{roleNames[invitation.role] ?? invitation.role}</span>
        <StatusBadge status={invitation.status === 'Expired' ? 'Expirado' : 'Pendente'} />
        <button className="button button--outline" type="button" disabled={busy} onClick={() => revokeInvitation(invitation.id)}>Cancelar</button>
      </article>)}
      {team.invitations.length === 0 && <p className="panel-body">Nenhum convite pendente.</p>}
    </div></section>
  </>;
}
