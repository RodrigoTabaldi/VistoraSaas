'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { apiRequest } from './api-client';
import type { InspectionRow } from './mock-data';

import { inspectionRow, type ApiInspection } from './inspection-row';
export type InspectionStatistic = { type: 'MoveIn' | 'MoveOut'; total: number; completed: number; pendingReports: number };

type InspectionStoreValue = {
  inspections: InspectionRow[];
  statistics: InspectionStatistic[];
  hydrated: boolean;
  error: string;
  refresh: () => Promise<void>;
  completeInspection: (id: string) => Promise<boolean>;
  getInspection: (id: string) => InspectionRow | undefined;
};

const InspectionStoreContext = createContext<InspectionStoreValue | null>(null);

export function InspectionProvider({ children, organizationId }: Readonly<{ children: React.ReactNode; organizationId: string }>) {
  const completionKeys = useRef(new Map<string, string>());
  const refreshVersion = useRef(0);
  const [statistics, setStatistics] = useState<InspectionStatistic[]>([]);
  const [items, setItems] = useState<InspectionRow[]>([]);
  const [hydrated, setHydrated] = useState(false);
  const [error, setError] = useState('');

  const refresh = useCallback(async () => {
    const version = ++refreshVersion.current;
    const [result, counts] = await Promise.all([
      apiRequest<{ items: ApiInspection[]; total: number }>('/api/v1/inspections/summary?pageSize=5'),
      apiRequest<InspectionStatistic[]>('/api/v1/inspections/statistics'),
    ]);
    if (version !== refreshVersion.current) return;
    setItems(result.items.map(inspectionRow));
    setStatistics(counts);
    setError('');
  }, []);

  useEffect(() => {
    setItems([]);
    setHydrated(false);
    refresh().catch((cause: unknown) => setError(cause instanceof Error ? cause.message : 'Não foi possível carregar as vistorias.'))
      .finally(() => setHydrated(true));
  }, [refresh, organizationId]);

  const completeInspection = useCallback(async (id: string) => {
    try {
      const key = completionKeys.current.get(id) ?? crypto.randomUUID();
      completionKeys.current.set(id, key);
      await apiRequest(`/api/v1/inspections/${id}/complete`, {
        method: 'POST',
        headers: { 'Idempotency-Key': key },
      });
      await refresh();
      return true;
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível concluir a vistoria.');
      return false;
    }
  }, [refresh]);

  const getInspection = useCallback((id: string) => items.find((item) => item.id === id), [items]);
  const value = useMemo(() => ({ inspections: items, statistics, hydrated, error, refresh, completeInspection, getInspection }),
    [items, statistics, hydrated, error, refresh, completeInspection, getInspection]);
  return <InspectionStoreContext.Provider value={value}>{children}</InspectionStoreContext.Provider>;
}

export function useInspections() {
  const value = useContext(InspectionStoreContext);
  if (!value) throw new Error('useInspections must be used inside InspectionProvider');
  return value;
}
