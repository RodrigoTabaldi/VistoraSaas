'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Icon } from './icons';
import { apiRequest } from '../lib/api-client';

export function LoginScreen() {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get('email') ?? '').trim().toLowerCase();
    const password = String(formData.get('password') ?? '');
    const rememberMe = formData.get('rememberMe') === 'on';

    setError('');
    setIsSubmitting(true);
    try {
      await apiRequest('/api/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password, rememberMe }),
      });
      router.replace('/dashboard');
      router.refresh();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Não foi possível entrar.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-visual" aria-label="Sobre a Vistora">
        <div className="login-visual-media" aria-hidden="true" />
        <div className="login-visual-overlay" aria-hidden="true" />
        <div className="login-visual-caption">
          <span className="login-visual-kicker">Vistora SaaS</span>
          <h1>Vistorias mais claras.<br />Decisões mais seguras.</h1>
          <p>Organize cada etapa da vistoria em um só lugar.</p>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="login-card-header">
            <div className="login-brand" aria-label="Vistora">
              <img src="/logo.jpeg" alt="Vistora" />
            </div>
            <h1>Bem-vindo de volta</h1>
            <p>Entre para continuar suas vistorias.</p>
          </div>

          <form method="post" onSubmit={handleSubmit}>
            <noscript><p className="login-error" role="alert">Ative o JavaScript para entrar na sua conta.</p></noscript>
            <div className="form-field">
              <label htmlFor="email">E-mail</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="mail" size={21} />
                <input id="email" name="email" type="email" placeholder="seu@email.com" autoComplete="email" required />
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="password">Senha</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="lock" size={21} />
                <input id="password" name="password" type={showPassword ? 'text' : 'password'} placeholder="Sua senha" autoComplete="current-password" required />
                <button className="input-action" type="button" aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setShowPassword((current) => !current)}>
                  <Icon name="eye" size={20} />
                </button>
              </div>
            </div>

            <div className="login-options">
              <label className="checkbox-label"><input type="checkbox" name="rememberMe" defaultChecked /> <span>Lembrar-me</span></label>
            </div>

            {error ? <p className="login-error" role="alert">{error}</p> : null}

            <button className="button button--primary login-submit" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Entrando…' : 'Entrar'} <Icon name="arrowRight" size={19} />
            </button>
          </form>

          <p className="login-signup">Ainda não tem conta? <Link href="/cadastro">Criar conta</Link></p>
        </div>

        <p className="login-privacy"><Icon name="shield" size={15} /> Seus dados protegidos e conexão segura.</p>
      </section>
    </main>
  );
}
