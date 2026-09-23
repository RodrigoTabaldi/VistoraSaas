'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';
import { Icon, type IconName } from './icons';

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
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="app-shell">
      <aside className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''}`}>
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
          <button className="mobile-menu" type="button" aria-label="Abrir menu" onClick={() => setSidebarOpen(true)}><Icon name="menu" size={23} /></button>
          <label className="topbar-search">
            <Icon name="search" size={19} />
            <input aria-label="Buscar imóveis, vistorias e clientes" placeholder="Buscar imóveis, vistorias, clientes..." />
            <span className="keycap">⌘ K</span>
          </label>
          <div className="topbar-spacer" />
          <div className="topbar-actions">
            <button className="icon-button" type="button" aria-label="Notificações"><Icon name="bell" size={21} /><span className="notification-dot" /></button>
            <div className="user-menu">
              <span className="avatar avatar--large">BA</span>
              <span className="user-info"><strong>Bruno Almeida</strong><span>Administrador</span></span>
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
