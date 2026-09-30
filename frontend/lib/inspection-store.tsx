'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { apiRequest } from './api-client';
import type { InspectionRow } from './mock-data';

type ApiInspection = {
  id: string;
  unitId: string;
  type: 'MoveIn' | 'MoveOut';
  status: 'Draft' | 'Completed' | 'Approved';
  createdAtUtc: string;
  scheduledAtUtc: string | null;
};
type Property = { id: string; name: string; address: string };
type Unit = { id: string; propertyId: string; identifier: string };

function getPresentationStatus(status: ApiInspection['status'], scheduledAtUtc: string | null): InspectionRow['status'] {
  if (status !== 'Draft') return 'Concluída';
  if (!scheduledAtUtc) return 'Em andamento';
  return Date.parse(scheduledAtUtc) < Date.now() ? 'Atrasada' : 'Agendada';
}

type InspectionStoreValue = {
  inspections: InspectionRow[];
  hydrated: boolean;
  error: string;
  refresh: () => Promise<void>;
  completeInspection: (id: string) => Promise<void>;
  getInspection: (id: string) => InspectionRow | undefined;
};

const InspectionStoreContext = createContext<InspectionStoreValue | null>(null);

export function InspectionProvider({ children, organizationId }: Readonly<{ children: React.ReactNode; organizationId: string }>) {
  const [items, setItems] = useState<InspectionRow[]>([]);
  const [hydrated, setHydrated] = useState(false);
  const [error, setError] = useState('');

  const refresh = useCallback(async () => {
    const [inspections, properties] = await Promise.all([
      apiRequest<ApiInspection[]>('/api/v1/inspections'),
      apiRequest<Property[]>('/api/v1/properties'),
    ]);
    const unitLists = await Promise.all(properties.map((property) =>
      apiRequest<Unit[]>(`/api/v1/properties/${property.id}/units`)));
    const units = new Map(unitLists.flat().map((unit) => [unit.id, unit]));
    const propertyById = new Map(properties.map((property) => [property.id, property]));
    setItems(inspections.map((inspection) => {
      const unit = units.get(inspection.unitId);
      const property = unit ? propertyById.get(unit.propertyId) : undefined;
      const created = new Date(inspection.createdAtUtc);
      const completed = inspection.status !== 'Draft';
      return {
        id: inspection.id,
        code: `VIS-${inspection.id.slice(0, 8).toUpperCase()}`,
        property: property?.name ?? 'Imóvel não encontrado',
        city: [property?.address, unit?.identifier].filter(Boolean).join(' · '),
        type: inspection.type === 'MoveIn' ? 'Entrada' : 'Saída',
        responsible: 'Não atribuído',
        responsibleInitials: 'NA',
        responsibleTone: 'slate',
        date: created.toLocaleDateString('pt-BR'),
        time: created.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
        scheduledAtUtc: inspection.scheduledAtUtc,
        status: getPresentationStatus(inspection.status, inspection.scheduledAtUtc),
        progress: completed ? 100 : 0,
      };
    }));
    setError('');
  }, []);

  useEffect(() => {
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar as vistorias.'))
      .finally(() => setHydrated(true));
  }, [refresh, organizationId]);

  const completeInspection = useCallback(async (id: string) => {
    try {
      await apiRequest(`/api/v1/inspections/${id}/complete`, {
        method: 'POST',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      });
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível concluir a vistoria.');
    }
  }, [refresh]);

  const getInspection = useCallback((id: string) => items.find((item) => item.id === id), [items]);
  const value = useMemo(() => ({ inspections: items, hydrated, error, refresh, completeInspection, getInspection }),
    [items, hydrated, error, refresh, completeInspection, getInspection]);
  return <InspectionStoreContext.Provider value={value}>{children}</InspectionStoreContext.Provider>;
}

export function useInspections() {
  const value = useContext(InspectionStoreContext);
  if (!value) throw new Error('useInspections must be used inside InspectionProvider');
  return value;
}
