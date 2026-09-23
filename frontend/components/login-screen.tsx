'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Icon } from './icons';
import { VistoraLogo } from './logo';
import { DEMO_ACCOUNT } from '../lib/demo-account';

export function LoginScreen() {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get('email') ?? '').trim().toLowerCase();
    const password = String(formData.get('password') ?? '');

    if (email !== DEMO_ACCOUNT.email || password !== DEMO_ACCOUNT.password) {
      setError('Use as credenciais da conta demonstração para continuar.');
      return;
    }

    setError('');
    router.push('/dashboard');
  }

  return (
    <main className="login-page">
      <section className="login-hero" aria-label="Sobre a Vistora">
        <div className="login-hero-media" aria-hidden="true" />
        <div className="login-hero-content">
          <VistoraLogo light />

          <div className="login-hero-copy">
            <div className="accent-line" />
            <h1 className="login-hero-title">Gestão inteligente<br />de vistorias imobiliárias</h1>
            <p className="login-hero-subtitle">Mais agilidade, segurança e organização em todas as etapas da vistoria, do agendamento ao relatório final.</p>

            <div className="benefit-list">
              <div className="benefit">
                <div className="benefit-icon"><Icon name="calendar" size={28} /></div>
                <div><strong>Processos mais ágeis</strong><span>Da agenda ao relatório, tudo em um só lugar.</span></div>
              </div>
              <div className="benefit">
                <div className="benefit-icon"><Icon name="shield" size={28} /></div>
                <div><strong>Mais segurança</strong><span>Registros completos com fotos e evidências.</span></div>
              </div>
              <div className="benefit">
                <div className="benefit-icon"><Icon name="chart" size={28} /></div>
                <div><strong>Decisões mais inteligentes</strong><span>Dados organizados para sua imobiliária crescer.</span></div>
              </div>
            </div>

            <div className="login-proof">
              <div><strong>+10 mil</strong><span>imóveis vistoriados</span></div>
              <div className="login-proof-divider" />
              <span>Imobiliárias de todo o Brasil confiam na Vistora.</span>
            </div>
          </div>

          <div className="login-tagline">IMÓVEIS MAIS SIMPLES. HISTÓRIAS MAIS SEGURAS.</div>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-topbar">
          <span>Novo por aqui?</span>
          <a href="#demo">Solicitar demonstração</a>
        </div>

        <div className="login-card">
          <div className="login-card-header">
            <VistoraLogo />
            <h1>Acesse sua conta</h1>
            <p className="demo-hint">Acesso demonstração: <strong>{DEMO_ACCOUNT.email}</strong></p>
            <p>Continue de onde parou e gerencie suas vistorias com total segurança.</p>
          </div>

          <form onSubmit={handleSubmit}>
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
              <label className="checkbox-label"><input type="checkbox" defaultChecked /> <span>Lembrar-me</span></label>
              <a href="#forgot-password">Esqueci minha senha?</a>
            </div>

            {error ? <p className="login-error" role="alert">{error}</p> : null}

            <button className="button button--primary login-submit" type="submit">Entrar <Icon name="arrowRight" size={19} /></button>
          </form>

          <div className="divider"><span>Ou</span></div>
          <button className="button google-button" type="button"><Icon name="google" size={21} /> Entrar com Google</button>
          <p className="login-signup">Não tem uma conta? <a href="#demo">Solicitar demonstração</a></p>
        </div>

        <div className="login-trust" aria-label="Garantias da plataforma">
          <span className="trust-item"><Icon name="shield" size={20} /> Seus dados protegidos</span>
          <span className="trust-item"><Icon name="lock" size={20} /> Conexão segura</span>
          <span className="trust-item"><Icon name="database" size={20} /> Plataforma confiável</span>
        </div>
      </section>
    </main>
  );
}
