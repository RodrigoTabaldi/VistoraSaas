'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { Icon, type IconName } from './icons';
import { apiRequest } from '../lib/api-client';

interface CurrentUser {
  userId: string;
  name: string;
  email: string;
  role: string;
  organizationId: string;
}

const navigation: Array<{ href: string; label: string; icon: IconName }> = [
  { href: '/dashboard', label: 'Home', icon: 'home' },
  { href: '/triagens', label: 'Triagens', icon: 'clipboard' },
  { href: '/imoveis', label: 'Imóveis', icon: 'building' },
  { href: '/agenda', label: 'Agenda', icon: 'calendar' },
  { href: '/relatorios', label: 'Relatórios', icon: 'chart' },
  { href: '/analises', label: 'Dashboard', icon: 'dashboard' },
  { href: '/equipe', label: 'Equipe', icon: 'users' },
  { href: '/configuracoes', label: 'Configurações', icon: 'settings' },
];

export function AppShell({ children }: Readonly<{ children: React.ReactNode }>) {
  const pathname = usePathname();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    let active = true;
    apiRequest<CurrentUser>('/api/v1/me')
      .then((user) => {
        if (active) {
          setCurrentUser(user);
          setIsAuthorized(true);
        }
      })
      .catch(() => {
        if (active) router.replace('/');
      });

    return () => { active = false; };
  }, [router]);

  useEffect(() => {
    setSidebarOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!sidebarOpen) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') setSidebarOpen(false);
    }
    window.addEventListener('keydown', closeOnEscape);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', closeOnEscape);
    };
  }, [sidebarOpen]);

  async function handleLogout() {
    try {
      await apiRequest('/api/v1/auth/logout', { method: 'POST' });
    } catch {
      // Leave the protected area even if the API is temporarily unavailable.
    }
    router.replace('/');
    router.refresh();
  }

  if (!isAuthorized || !currentUser) {
    return <main className="auth-loading" role="status">Verificando acesso…</main>;
  }

  const initials = currentUser.name.trim().split(/\s+/).slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase('pt-BR') ?? '')
    .join('') || 'US';
  const roleLabel = currentUser.role === 'Admin' ? 'Administrador' : currentUser.role;

  return (
    <div className="app-shell">
      <aside id="primary-navigation" className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''}`}>
        <div className="sidebar-brand">
          <div className="sidebar-brand-image" aria-label="Vistora">
            <img src="/logo.jpeg" alt="Vistora" />
          </div>
        </div>
        <nav className="sidebar-nav" aria-label="Navegação principal">
          {navigation.map((item) => {
            const isActive = item.href === '/dashboard' ? pathname === item.href : pathname.startsWith(item.href);
            return (
              <Link key={item.href} className="sidebar-nav-link" href={item.href} aria-current={isActive ? 'page' : undefined} onClick={() => setSidebarOpen(false)}>
                <Icon name={item.icon} size={20} />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
        <div className="sidebar-promo">
          <div className="sidebar-promo-content"><strong>Vistorias mais simples.<br />Imóveis mais seguros.</strong></div>
        </div>
      </aside>
      {sidebarOpen && <button className="sidebar-backdrop" aria-label="Fechar menu" onClick={() => setSidebarOpen(false)} />}

      <div className="app-main">
        <header className="topbar">
          <button className="mobile-menu" type="button" aria-label={sidebarOpen ? 'Fechar menu' : 'Abrir menu'} aria-expanded={sidebarOpen} aria-controls="primary-navigation" onClick={() => setSidebarOpen((open) => !open)}><Icon name={sidebarOpen ? 'x' : 'menu'} size={23} /></button>
          <label className="topbar-search">
            <Icon name="search" size={19} />
            <input aria-label="Buscar imóveis, vistorias e clientes" placeholder="Buscar imóveis, vistorias, clientes..." />
            <span className="keycap">⌘ K</span>
          </label>
          <div className="topbar-spacer" />
          <div className="topbar-actions">
            <button className="icon-button" type="button" aria-label="Notificações"><Icon name="bell" size={21} /><span className="notification-dot" /></button>
            <button className="icon-button" type="button" aria-label="Sair da conta" title="Sair da conta" onClick={handleLogout}><Icon name="logout" size={19} /></button>
            <div className="user-menu">
              <span className="avatar avatar--large">{initials}</span>
              <span className="user-info"><strong>{currentUser.name}</strong><span>{roleLabel}</span></span>
              <Icon name="chevronDown" size={16} />
            </div>
            <Link className="button button--primary topbar-cta" href="/triagens/nova"><Icon name="plus" size={18} /> Nova triagem</Link>
          </div>
        </header>
        <main className="app-content">{children}</main>
      </div>
    </div>
  );
}
