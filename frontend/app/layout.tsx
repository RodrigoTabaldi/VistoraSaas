import type { Metadata, Viewport } from 'next';
import { ServiceWorkerRegister } from '../components/service-worker-register';
import './globals.css';

export const metadata: Metadata = {
  title: {
    default: 'Vistora | Gestão de vistorias imobiliárias',
    template: '%s | Vistora',
  },
  description: 'Gestão inteligente de vistorias imobiliárias.',
  manifest: '/manifest.webmanifest',
};

export const viewport: Viewport = {
  themeColor: '#063f35',
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="pt-BR">
      <body><ServiceWorkerRegister />{children}</body>
    </html>
  );
}
