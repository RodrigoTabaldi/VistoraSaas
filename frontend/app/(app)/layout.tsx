import { AppShell } from '../../components/app-shell';
import { InspectionProvider } from '../../lib/inspection-store';
import { WorkspaceProvider } from '../../lib/workspace-store';

export default function ProtectedAppLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <WorkspaceProvider><InspectionProvider><AppShell>{children}</AppShell></InspectionProvider></WorkspaceProvider>;
}
