'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { inspections as seedInspections, type InspectionRow, type InspectionStatus } from './mock-data';

const STORAGE_KEY = 'vistora.inspections.v1';

export type CreateInspectionInput = {
  property: string;
  city: string;
  type: InspectionRow['type'];
  responsible: string;
  responsibleInitials: string;
  responsibleTone: string;
  date: string;
  time: string;
};

type InspectionStoreValue = {
  inspections: InspectionRow[];
  hydrated: boolean;
  createInspection: (input: CreateInspectionInput) => InspectionRow;
  completeInspection: (id: string) => void;
  updateInspectionStatus: (id: string, status: InspectionStatus) => void;
  getInspection: (id: string) => InspectionRow | undefined;
};

const InspectionStoreContext = createContext<InspectionStoreValue | null>(null);

function isInspectionList(value: unknown): value is InspectionRow[] {
  return Array.isArray(value) && value.every((item) => (
    item && typeof item === 'object' &&
    typeof (item as InspectionRow).id === 'string' &&
    typeof (item as InspectionRow).property === 'string' &&
    typeof (item as InspectionRow).status === 'string'
  ));
}

export function InspectionProvider({ children }: Readonly<{ children: React.ReactNode }>) {
  const [items, setItems] = useState<InspectionRow[]>(seedInspections);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    try {
      const stored = window.localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed: unknown = JSON.parse(stored);
        if (isInspectionList(parsed)) setItems(parsed);
      }
    } catch {
      // If local storage is unavailable or corrupted, the seeded demo remains usable.
    } finally {
      setHydrated(true);
    }
  }, []);

  useEffect(() => {
    if (hydrated) window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  }, [hydrated, items]);

  const createInspection = useCallback((input: CreateInspectionInput) => {
    const created: InspectionRow = {
      ...input,
      id: `local-${Date.now()}`,
      code: `VIS-2024-${String(items.length + 1).padStart(3, '0')}`,
      status: 'Agendada',
      progress: 0,
    };
    setItems((current) => [created, ...current]);
    return created;
  }, [items.length]);

  const completeInspection = useCallback((id: string) => {
    setItems((current) => current.map((item) => item.id === id
      ? { ...item, status: 'Concluída', progress: 100 }
      : item));
  }, []);

  const updateInspectionStatus = useCallback((id: string, status: InspectionStatus) => {
    setItems((current) => current.map((item) => item.id === id
      ? { ...item, status, progress: status === 'Concluída' ? 100 : item.progress }
      : item));
  }, []);

  const getInspection = useCallback((id: string) => items.find((item) => item.id === id), [items]);

  const value = useMemo(() => ({
    inspections: items,
    hydrated,
    createInspection,
    completeInspection,
    updateInspectionStatus,
    getInspection,
  }), [completeInspection, createInspection, getInspection, hydrated, items, updateInspectionStatus]);

  return <InspectionStoreContext.Provider value={value}>{children}</InspectionStoreContext.Provider>;
}

export function useInspections() {
  const value = useContext(InspectionStoreContext);
  if (!value) throw new Error('useInspections must be used inside InspectionProvider');
  return value;
}
