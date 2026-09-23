import type { SVGProps } from 'react';

export type IconName =
  | 'home' | 'clipboard' | 'building' | 'calendar' | 'chart' | 'dashboard' | 'users' | 'settings'
  | 'search' | 'bell' | 'plus' | 'arrowRight' | 'arrowUp' | 'arrowDown' | 'clock' | 'file'
  | 'check' | 'warning' | 'x' | 'mapPin' | 'chevronDown' | 'chevronRight' | 'chevronLeft'
  | 'download' | 'grid' | 'list' | 'eye' | 'mail' | 'lock' | 'google' | 'menu' | 'shield'
  | 'database' | 'logout' | 'sofa' | 'bed' | 'kitchen' | 'bath' | 'tree' | 'filter' | 'refresh'
  | 'external' | 'more' | 'image' | 'link' | 'sparkles';

type IconProps = SVGProps<SVGSVGElement> & { name: IconName; size?: number };

export function Icon({ name, size = 20, className, ...props }: IconProps) {
  const common = {
    width: size,
    height: size,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.8,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
    className,
    'aria-hidden': true,
    ...props,
  };

  switch (name) {
    case 'home': return <svg {...common}><path d="m3 10 9-7 9 7" /><path d="M5 9v11h14V9" /><path d="M9 20v-6h6v6" /></svg>;
    case 'clipboard': return <svg {...common}><rect x="5" y="4" width="14" height="17" rx="2" /><path d="M9 4.5V3h6v1.5M8 9h8M8 13h8M8 17h5" /></svg>;
    case 'building': return <svg {...common}><path d="M4 21V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v16" /><path d="M2 21h20M8 7h2M14 7h2M8 11h2M14 11h2M8 15h2M14 15h2M10 21v-3h4v3" /></svg>;
    case 'calendar': return <svg {...common}><rect x="3" y="4.5" width="18" height="16" rx="2" /><path d="M16 2.5v4M8 2.5v4M3 9h18M8 13h.01M12 13h.01M16 13h.01M8 17h.01M12 17h.01" /></svg>;
    case 'chart': return <svg {...common}><path d="M4 20V10M10 20V4M16 20v-7M22 20H2" /></svg>;
    case 'dashboard': return <svg {...common}><path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z" /></svg>;
    case 'users': return <svg {...common}><path d="M16 20v-1.5a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4V20M9.5 10.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM16 3.8a3.5 3.5 0 0 1 0 6.8M17 14.6a4 4 0 0 1 4 4V20" /></svg>;
    case 'settings': return <svg {...common}><path d="M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Z" /><path d="m19.4 15 .1.1a1.8 1.8 0 0 1-2.5 2.5l-.1-.1a1.8 1.8 0 0 0-3.1 1.3v.2a1.8 1.8 0 0 1-3.6 0v-.2a1.8 1.8 0 0 0-3.1-1.3l-.1.1a1.8 1.8 0 1 1-2.5-2.5l.1-.1a1.8 1.8 0 0 0-1.3-3.1h-.2a1.8 1.8 0 0 1 0-3.6h.2a1.8 1.8 0 0 0 1.3-3.1l-.1-.1a1.8 1.8 0 1 1 2.5-2.5l.1.1a1.8 1.8 0 0 0 3.1-1.3V1.3a1.8 1.8 0 0 1 3.6 0v.2a1.8 1.8 0 0 0 3.1 1.3l.1-.1a1.8 1.8 0 0 1 2.5 2.5l-.1.1a1.8 1.8 0 0 0 1.3 3.1h.2a1.8 1.8 0 0 1 0 3.6h-.2a1.8 1.8 0 0 0-1.3 3.1Z" /></svg>;
    case 'search': return <svg {...common}><circle cx="10.8" cy="10.8" r="6.8" /><path d="m16 16 5 5" /></svg>;
    case 'bell': return <svg {...common}><path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" /></svg>;
    case 'plus': return <svg {...common}><path d="M12 5v14M5 12h14" /></svg>;
    case 'arrowRight': return <svg {...common}><path d="M5 12h14M13 6l6 6-6 6" /></svg>;
    case 'arrowUp': return <svg {...common}><path d="M12 19V5M6 11l6-6 6 6" /></svg>;
    case 'arrowDown': return <svg {...common}><path d="M12 5v14M6 13l6 6 6-6" /></svg>;
    case 'clock': return <svg {...common}><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>;
    case 'file': return <svg {...common}><path d="M6 3h8l4 4v14H6z" /><path d="M14 3v5h5M9 12h6M9 16h6" /></svg>;
    case 'check': return <svg {...common}><path d="m5 12 4 4L19 6" /></svg>;
    case 'warning': return <svg {...common}><path d="m12 3 9 17H3L12 3Z" /><path d="M12 9v4M12 16h.01" /></svg>;
    case 'x': return <svg {...common}><path d="m6 6 12 12M18 6 6 18" /></svg>;
    case 'mapPin': return <svg {...common}><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" /><circle cx="12" cy="10" r="2.5" /></svg>;
    case 'chevronDown': return <svg {...common}><path d="m6 9 6 6 6-6" /></svg>;
    case 'chevronRight': return <svg {...common}><path d="m9 6 6 6-6 6" /></svg>;
    case 'chevronLeft': return <svg {...common}><path d="m15 6-6 6 6 6" /></svg>;
    case 'download': return <svg {...common}><path d="M12 3v12M7 10l5 5 5-5M4 20h16" /></svg>;
    case 'grid': return <svg {...common}><rect x="4" y="4" width="6" height="6" rx="1" /><rect x="14" y="4" width="6" height="6" rx="1" /><rect x="4" y="14" width="6" height="6" rx="1" /><rect x="14" y="14" width="6" height="6" rx="1" /></svg>;
    case 'list': return <svg {...common}><path d="M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01" /></svg>;
    case 'eye': return <svg {...common}><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.5" /></svg>;
    case 'mail': return <svg {...common}><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m4 7 8 6 8-6" /></svg>;
    case 'lock': return <svg {...common}><rect x="5" y="10" width="14" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3M12 14v3" /></svg>;
    case 'google': return <svg {...common} stroke="none"><path fill="#4285F4" d="M21.6 12.2c0-.7-.1-1.4-.2-2H12v3.8h5.4a4.6 4.6 0 0 1-2 3v2.5h3.2c1.9-1.8 3-4.3 3-7.3Z" /><path fill="#34A853" d="M12 22c2.7 0 5-.9 6.7-2.5l-3.2-2.5c-.9.6-2 .9-3.5.9-2.7 0-5-1.8-5.8-4.3H3v2.6A10.1 10.1 0 0 0 12 22Z" /><path fill="#FBBC05" d="M6.2 13.6a6 6 0 0 1 0-3.2V7.8H3a10.1 10.1 0 0 0 0 8.4l3.2-2.6Z" /><path fill="#EA4335" d="M12 6.1c1.6 0 3 .6 4.1 1.7l3-3C17 3.1 14.7 2 12 2a10.1 10.1 0 0 0-9 5.8l3.2 2.6C7 7.9 9.3 6.1 12 6.1Z" /></svg>;
    case 'menu': return <svg {...common}><path d="M4 6h16M4 12h16M4 18h16" /></svg>;
    case 'shield': return <svg {...common}><path d="M12 3 20 6v5c0 5-3.4 8.7-8 10-4.6-1.3-8-5-8-10V6l8-3Z" /><path d="m8.5 12 2.2 2.2 4.8-4.8" /></svg>;
    case 'database': return <svg {...common}><ellipse cx="12" cy="5" rx="7" ry="3" /><path d="M5 5v7c0 1.7 3.1 3 7 3s7-1.3 7-3V5M5 12v7c0 1.7 3.1 3 7 3s7-1.3 7-3v-7" /></svg>;
    case 'logout': return <svg {...common}><path d="M10 5H5v14h5M14 16l4-4-4-4M18 12H9" /></svg>;
    case 'sofa': return <svg {...common}><path d="M5 11V8a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v3M4 18v-5a3 3 0 0 1 3-3h10a3 3 0 0 1 3 3v5M4 15h16M7 18v2M17 18v2" /></svg>;
    case 'bed': return <svg {...common}><path d="M4 19v-9M4 15h16M20 19v-7a2 2 0 0 0-2-2h-5v5M4 15h16M7 13h3V9H7a3 3 0 0 0-3 3" /></svg>;
    case 'kitchen': return <svg {...common}><path d="M4 20V6h16v14M4 10h16M8 6V3M12 6V3M16 6V3M8 14h3M13 14h3" /></svg>;
    case 'bath': return <svg {...common}><path d="M4 13h16v2a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4v-2ZM6 13V6a3 3 0 0 1 5-2M4 21h1M19 21h1M8 19v2M16 19v2" /></svg>;
    case 'tree': return <svg {...common}><path d="m12 3-4 6h2l-4 5h4l-2 5h8l-2-5h4l-4-5h2l-4-6ZM12 19v3" /></svg>;
    case 'filter': return <svg {...common}><path d="M4 5h16M7 12h10M10 19h4" /></svg>;
    case 'refresh': return <svg {...common}><path d="M20 11a8 8 0 0 0-14.9-3L3 11M3 5v6h6M4 13a8 8 0 0 0 14.9 3L21 13m0 6v-6h-6" /></svg>;
    case 'external': return <svg {...common}><path d="M14 4h6v6M20 4l-9 9M18 13v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h5" /></svg>;
    case 'more': return <svg {...common}><circle cx="5" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="19" cy="12" r="1" fill="currentColor" stroke="none" /></svg>;
    case 'image': return <svg {...common}><rect x="3" y="4" width="18" height="16" rx="2" /><circle cx="8.5" cy="9" r="1.5" /><path d="m4 17 5-5 3 3 2-2 6 5" /></svg>;
    case 'link': return <svg {...common}><path d="M10 13a5 5 0 0 0 7.1.1l2-2a5 5 0 0 0-7.1-7.1l-1.1 1.1M14 11a5 5 0 0 0-7.1-.1l-2 2A5 5 0 0 0 12 20l1.1-1.1" /></svg>;
    case 'sparkles': return <svg {...common}><path d="m12 3 1.4 4.6L18 9l-4.6 1.4L12 15l-1.4-4.6L6 9l4.6-1.4L12 3ZM19 15l.6 2.4L22 18l-2.4.6L19 21l-.6-2.4L16 18l2.4-.6L19 15Z" /></svg>;
  }
}
