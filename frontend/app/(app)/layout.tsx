import { AppShell } from '../../components/app-shell';
import { InspectionProvider } from '../../lib/inspection-store';

export default function ProtectedAppLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <InspectionProvider><AppShell>{children}</AppShell></InspectionProvider>;
}
