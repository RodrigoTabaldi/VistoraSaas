'use client';
import { useEffect, useState } from 'react';
import { apiRequest } from './api-client';
import { inspectionRow, type ApiInspection } from './inspection-row';
import type { InspectionRow } from './mock-data';

export function useInspectionPage(parameters: string, revision: unknown = null) {
  const [items, setItems] = useState<InspectionRow[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    apiRequest<{ items: ApiInspection[]; total: number }>(`/api/v1/inspections/summary?${parameters}`)
      .then((result) => { if (active) { setItems(result.items.map(inspectionRow)); setTotal(result.total); } })
      .catch((cause: unknown) => { if (active) { setItems([]); setTotal(0); setError(cause instanceof Error ? cause.message : 'Falha ao carregar vistorias.'); } })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [parameters, revision]);
  return { items, total, loading, error };
}
