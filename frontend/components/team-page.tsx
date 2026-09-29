'use client';

import { useState } from 'react';
import { Icon } from './icons';
import { Avatar, PageHeading, PanelHeader, StatusBadge } from './dashboard-primitives';
import { useWorkspace } from '../lib/workspace-store';

export function TeamPage() {
  const { team, addMember } = useWorkspace();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState(''); const [email, setEmail] = useState(''); const [role, setRole] = useState('Vistoriador');
  function submit(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); addMember({ name, email, role }); setName(''); setEmail(''); setShowForm(false); }
  return <><PageHeading title="Equipe" description="Demonstração local: membros e convites não são enviados nem persistidos na API." /><div className="page-actions"><span className="panel-caption">{team.length} membros na organização</span><button className="button button--primary" type="button" onClick={() => setShowForm((value) => !value)}><Icon name="plus" size={17} /> Adicionar rascunho</button></div>{showForm && <form className="panel compact-form" onSubmit={submit}><PanelHeader title="Rascunho de membro" /><div className="form-grid"><label className="form-field"><span>Nome</span><input value={name} onChange={(event) => setName(event.target.value)} required /></label><label className="form-field"><span>E-mail</span><input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required /></label><label className="form-field"><span>Perfil</span><select value={role} onChange={(event) => setRole(event.target.value)}><option>Vistoriador</option><option>Administrador</option><option>Leitor</option></select></label></div><div className="form-actions"><button className="button button--outline" type="button" onClick={() => setShowForm(false)}>Cancelar</button><button className="button button--primary" type="submit">Salvar rascunho local</button></div></form>}<section className="panel"><PanelHeader title="Membros da equipe" /><div className="team-list">{team.map((member) => <article className="team-row" key={member.id}><Avatar initials={member.initials} tone={member.tone} large /><div className="team-copy"><strong>{member.name}</strong><span>{member.email}</span></div><span className="team-role">{member.role}</span><StatusBadge status={member.status} /><button className="icon-button" type="button" aria-label={`Mais ações para ${member.name}`}><Icon name="more" size={17} /></button></article>)}</div></section></>;
}
