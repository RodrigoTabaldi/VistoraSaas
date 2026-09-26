'use client';

import { FormEvent, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Icon } from './icons';
import { apiRequest } from '../lib/api-client';

export function SignupScreen() {
  const router = useRouter();
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    const formData = new FormData(event.currentTarget);
    const name = String(formData.get('name') ?? '').trim();
    const organizationName = String(formData.get('organizationName') ?? '').trim();
    const email = String(formData.get('email') ?? '').trim().toLowerCase();
    const password = String(formData.get('password') ?? '');
    const confirmPassword = String(formData.get('confirmPassword') ?? '');

    if (password !== confirmPassword) {
      setError('As senhas não coincidem.');
      return;
    }

    setError('');
    setIsSubmitting(true);
    try {
      await apiRequest('/api/v1/auth/register', {
        method: 'POST',
        body: JSON.stringify({ name, organizationName, email, password }),
      });
      router.replace('/dashboard');
      router.refresh();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Não foi possível criar a conta.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page login-page--signup">
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
        <div className="login-card login-card--signup">
          <div className="login-card-header">
            <div className="login-brand" aria-label="Vistora">
              <img src="/logo.jpeg" alt="Vistora" />
            </div>
            <h1>Criar sua conta</h1>
            <p>Cadastre sua empresa e comece suas vistorias.</p>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="form-field">
              <label htmlFor="name">Seu nome</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="users" size={20} />
                <input id="name" name="name" type="text" placeholder="Nome completo" autoComplete="name" maxLength={150} required />
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="organizationName">Nome da empresa</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="building" size={20} />
                <input id="organizationName" name="organizationName" type="text" placeholder="Sua imobiliária ou empresa" autoComplete="organization" maxLength={200} required />
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="signupEmail">E-mail</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="mail" size={20} />
                <input id="signupEmail" name="email" type="email" placeholder="seu@email.com" autoComplete="email" maxLength={320} required />
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="signupPassword">Senha</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="lock" size={20} />
                <input id="signupPassword" name="password" type="password" placeholder="Mínimo de 12 caracteres" autoComplete="new-password" minLength={12} maxLength={128} required />
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="confirmPassword">Confirmar senha</label>
              <div className="input-wrap">
                <Icon className="input-icon" name="lock" size={20} />
                <input id="confirmPassword" name="confirmPassword" type="password" placeholder="Digite a senha novamente" autoComplete="new-password" minLength={12} maxLength={128} required />
              </div>
            </div>

            {error ? <p className="login-error" role="alert">{error}</p> : null}

            <button className="button button--primary login-submit" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Criando conta…' : 'Criar conta'} <Icon name="arrowRight" size={19} />
            </button>
          </form>

          <p className="login-signup">Já tem uma conta? <Link href="/">Entrar</Link></p>
        </div>

        <p className="login-privacy"><Icon name="shield" size={15} /> Seus dados protegidos e conexão segura.</p>
      </section>
    </main>
  );
}
