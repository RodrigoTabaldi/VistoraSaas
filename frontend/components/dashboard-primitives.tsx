import type { CSSProperties, ReactNode } from 'react';
import { Icon, type IconName } from './icons';

type MetricCardProps = {
  icon: IconName;
  label: string;
  value: string;
  trend?: string;
  trendTone?: 'up' | 'down' | 'bad';
  note: string;
  tone?: 'green' | 'blue' | 'yellow';
};

export function MetricCard({ icon, label, value, trend, trendTone = 'up', note, tone = 'green' }: MetricCardProps) {
  return (
    <article className="metric-card">
      <div className={`metric-icon metric-icon--${tone}`}><Icon name={icon} size={25} /></div>
      <div className="metric-copy">
        <span className="metric-label">{label}</span>
        <div className="metric-main"><strong className="metric-value">{value}</strong>{trend && <span className={`trend trend--${trendTone}`}>{trend}</span>}</div>
        <span className="metric-note">{note}</span>
      </div>
    </article>
  );
}

export function PanelHeader({ title, action, children }: Readonly<{ title: string; action?: ReactNode; children?: ReactNode }>) {
  return <div className="panel-header"><h2 className="panel-title">{title}</h2>{action ?? children}</div>;
}

export function DonutChart({ value, label, color = '#11a870', children, className = '' }: Readonly<{ value: number; label: string; color?: string; children?: ReactNode; className?: string }>) {
  const style = { '--donut-value': `${value}%`, '--donut-color': color } as CSSProperties;
  return (
    <div className={`donut-layout ${className}`}>
      <div className="donut" style={style}><div className="donut-center"><strong>{label}</strong><span>vistorias</span></div></div>
      {children}
    </div>
  );
}

export function LegendRow({ label, value, percent, color = 'green' }: Readonly<{ label: string; value: string | number; percent?: string | number; color?: 'green' | 'yellow' | 'gray' | 'light' | 'red' | 'orange' }>) {
  return <div className="legend-row"><span className={`legend-dot legend-dot--${color}`} /><span>{label}</span><strong>{value}</strong>{percent !== undefined && <span>{percent}%</span>}</div>;
}

export function StatusBadge({ status }: Readonly<{ status: string }>) {
  const normalized = status.toLowerCase();
  const tone = normalized.includes('conclu') || normalized.includes('confirm') || normalized.includes('conforme')
    ? 'success'
    : normalized.includes('andamento') || normalized.includes('agendada')
      ? 'progress'
      : normalized.includes('atras') || normalized.includes('não conforme')
        ? 'danger'
        : normalized.includes('atenção') || normalized.includes('pendente')
          ? 'pending'
          : 'neutral';
  return <span className={`status-badge status-badge--${tone}`}>{status}</span>;
}

export function Avatar({ initials, tone = 'slate', large = false }: Readonly<{ initials: string; tone?: string; large?: boolean }>) {
  return <span className={`avatar avatar--${tone} ${large ? 'avatar--large' : ''}`}>{initials}</span>;
}

export function ProgressBar({ value, complete = false }: Readonly<{ value: number; complete?: boolean }>) {
  return <div className={`progress-line ${complete ? 'progress-line--complete' : ''}`}><span style={{ width: `${value}%` }} /></div>;
}

export function PageHeading({ title, description, side }: Readonly<{ title: string; description: string; side?: string }>) {
  return <div className="page-heading"><div><h1>{title}</h1><p>{description}</p></div>{side && <div className="heading-side">{side}</div>}</div>;
}
