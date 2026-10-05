import type { InspectionRow } from './mock-data';
export type ApiInspection = {
  id: string;
  unitId: string;
  type: 'MoveIn' | 'MoveOut';
  status: 'Draft' | 'Completed' | 'Approved';
  createdAtUtc: string;
  scheduledAtUtc: string | null;
  propertyName: string;
  address: string;
  unitIdentifier: string;
  totalItems: number;
  answeredItems: number;
  hasReport: boolean;
};

export function getPresentationStatus(status: ApiInspection['status'], scheduledAtUtc: string | null): InspectionRow['status'] {
  if (status !== 'Draft') return 'Concluída';
  if (!scheduledAtUtc) return 'Em andamento';
  return Date.parse(scheduledAtUtc) < Date.now() ? 'Atrasada' : 'Agendada';
}

export function inspectionRow(inspection: ApiInspection): InspectionRow {
      const created = new Date(inspection.createdAtUtc);
      return {
        id: inspection.id,
        code: `VIS-${inspection.id.slice(0, 8).toUpperCase()}`,
        property: inspection.propertyName,
        city: [inspection.address, inspection.unitIdentifier].filter(Boolean).join(' · '),
        type: inspection.type === 'MoveIn' ? 'Entrada' : 'Saída',
        responsible: 'Não atribuído',
        responsibleInitials: 'NA',
        responsibleTone: 'slate',
        date: created.toLocaleDateString('pt-BR'),
        time: created.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
        scheduledAtUtc: inspection.scheduledAtUtc,
        status: getPresentationStatus(inspection.status, inspection.scheduledAtUtc),
        progress: inspection.totalItems ? Math.round(inspection.answeredItems * 100 / inspection.totalItems) : 0,
        createdAtUtc: inspection.createdAtUtc,
        hasReport: inspection.hasReport,
      };
 }
