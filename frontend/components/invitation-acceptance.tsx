'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { apiRequest } from '../lib/api-client';

export function InvitationAcceptance({ token }: Readonly<{ token: string }>) {
  const router = useRouter();
  const [name, setName] = useState('');
  const [password, setPassword] = useState('');
  const [passwordConfirmation, setPasswordConfirmation] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function accept(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (password !== passwordConfirmation) {
      setError('As senhas não coincidem.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await apiRequest('/api/v1/auth/invitations/accept', {
        method: 'POST',
        body: JSON.stringify({ token, name, password }),
      });
      router.replace('/dashboard');
      router.refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível aceitar o convite.');
    } finally {
      setBusy(false);
    }
  }

  return <main className="invitation-page">
    <form className="panel compact-form invitation-card" onSubmit={accept}>
      <h1>Aceitar convite</h1>
      <p>Crie seu acesso à organização indicada neste convite.</p>
      {!token && <p className="form-feedback form-feedback--error" role="alert">O link não contém um token válido. Solicite um novo convite ao administrador.</p>}
      <label className="form-field"><span>Nome</span><input value={name} onChange={(event) => setName(event.target.value)} maxLength={150} autoComplete="name" required /></label>
      <label className="form-field"><span>Senha (mínimo de 12 caracteres)</span><input type="password" value={password} onChange={(event) => setPassword(event.target.value)} minLength={12} maxLength={128} autoComplete="new-password" required /></label>
      <label className="form-field"><span>Confirmar senha</span><input type="password" value={passwordConfirmation} onChange={(event) => setPasswordConfirmation(event.target.value)} minLength={12} maxLength={128} autoComplete="new-password" required /></label>
      {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
      <button className="button button--primary" type="submit" disabled={busy || !token}>{busy ? 'Validando convite...' : 'Criar acesso'}</button>
      <Link href="/">Voltar ao início</Link>
    </form>
  </main>;
}
