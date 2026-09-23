type LogoProps = {
  compact?: boolean;
  light?: boolean;
  className?: string;
};

export function VistoraLogo({ compact = false, light = false, className = '' }: LogoProps) {
  return (
    <div className={`vistora-logo ${compact ? 'vistora-logo--compact' : ''} ${light ? 'vistora-logo--light' : ''} ${className}`}>
      <svg className="vistora-logo__mark" viewBox="0 0 54 60" aria-hidden="true">
        <path d="M27 2 48 13v24L27 58 6 47V13L27 2Z" fill="none" stroke="currentColor" strokeWidth="3.2" strokeLinejoin="round" />
        <path d="m13 20 14 8 14-8M13 20v19l14 8 14-8V20M20 31l7 4 7-4M20 31v10l7 4 7-4V31" fill="none" stroke="currentColor" strokeWidth="3.2" strokeLinejoin="round" />
        <path d="m18 22 7 7 13-15" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {!compact && <span className="vistora-logo__word">Vistora</span>}
    </div>
  );
}
