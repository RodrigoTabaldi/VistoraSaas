type LogoProps = {
  compact?: boolean;
  light?: boolean;
  className?: string;
};

export function VistoraLogo({ compact = false, light = false, className = '' }: LogoProps) {
  return (
    <div className={`vistora-logo ${compact ? 'vistora-logo--compact' : ''} ${light ? 'vistora-logo--light' : ''} ${className}`}>
      <svg className="vistora-logo__mark" viewBox="0 0 76 80" aria-hidden="true">
        <path d="M11 73V29L38 13l20 12v12L38 25 22 35v38H11Z" fill="none" stroke="currentColor" strokeWidth="4.2" strokeLinejoin="round" />
        <path d="M27 73V45l11-9 13 8v29" fill="none" stroke="currentColor" strokeWidth="4.2" strokeLinejoin="round" />
        <path d="M7 29h15l17 31 18-37 15-7-26 48-7 12-7-12L7 29Z" fill="none" stroke="currentColor" strokeWidth="4.8" strokeLinejoin="round" />
      </svg>
      {!compact && <span className="vistora-logo__word">Vistora</span>}
    </div>
  );
}
