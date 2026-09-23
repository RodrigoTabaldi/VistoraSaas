'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import type { ChecklistStatus } from './mock-data';

const STORAGE_KEY = 'vistora.workspace.v1';

export type PropertyRecord = {
  id: string;
  name: string;
  address: string;
  units: number;
  inspections: number;
  occupancy: string;
};

export type TeamMember = {
  id: string;
  name: string;
  email: string;
  role: string;
  status: 'Ativo' | 'Convite pendente';
  initials: string;
  tone: string;
};

export type InspectionPhoto = {
  id: string;
  inspectionId: string;
  itemName: string;
  fileName: string;
  dataUrl: string;
  createdAt: string;
};

type ItemDraft = { status?: ChecklistStatus; notes?: string };
type WorkspaceState = {
  properties: PropertyRecord[];
  team: TeamMember[];
  photos: InspectionPhoto[];
  itemDrafts: Record<string, ItemDraft>;
  settings: { companyName: string; document: string; email: string; notifications: boolean; autoReports: boolean };
};

const seed: WorkspaceState = {
  properties: [
    { id: 'vista-verde', name: 'Residencial Vista Verde', address: 'Av. das Nações, 1200 · São Paulo, SP', units: 84, inspections: 28, occupancy: '92%' },
    { id: 'parque-flores', name: 'Condomínio Parque das Flores', address: 'Rua das Flores, 320 · Osasco, SP', units: 126, inspections: 41, occupancy: '88%' },
    { id: 'solar-paulista', name: 'Edifício Solar Paulista', address: 'Al. Santos, 850 · São Paulo, SP', units: 48, inspections: 19, occupancy: '96%' },
    { id: 'bela-vista', name: 'Condomínio Bela Vista', address: 'Rua das Acácias, 74 · Santo André, SP', units: 64, inspections: 14, occupancy: '81%' },
    { id: 'harmonia', name: 'Residencial Harmonia', address: 'Rua Harmonia, 51 · São Bernardo do Campo, SP', units: 32, inspections: 9, occupancy: '94%' },
  ],
  team: [
    { id: 'carla', name: 'Carla Mendes', email: 'carla@vistora.com.br', role: 'Administradora', status: 'Ativo', initials: 'CM', tone: 'rose' },
    { id: 'rafael', name: 'Rafael Lima', email: 'rafael@vistora.com.br', role: 'Vistoriador', status: 'Ativo', initials: 'RL', tone: 'blue' },
    { id: 'juliana', name: 'Juliana Costa', email: 'juliana@vistora.com.br', role: 'Vistoriadora', status: 'Ativo', initials: 'JC', tone: 'purple' },
    { id: 'lucas', name: 'Lucas Ferreira', email: 'lucas@vistora.com.br', role: 'Vistoriador', status: 'Convite pendente', initials: 'LF', tone: 'slate' },
  ],
  photos: [],
  itemDrafts: {},
  settings: { companyName: 'Vistora Gestão de Imóveis', document: '12.345.678/0001-90', email: 'admin@vistora.com.br', notifications: true, autoReports: true },
};

type WorkspaceValue = WorkspaceState & {
  hydrated: boolean;
  addProperty: (input: Pick<PropertyRecord, 'name' | 'address' | 'units'>) => void;
  addMember: (input: Pick<TeamMember, 'name' | 'email' | 'role'>) => void;
  updateSettings: (settings: Partial<WorkspaceState['settings']>) => void;
  addPhoto: (input: { inspectionId: string; itemName: string; file: File }) => Promise<void>;
  removePhoto: (photoId: string) => void;
  getPhotos: (inspectionId: string, itemName?: string) => InspectionPhoto[];
  getItemDraft: (inspectionId: string, itemName: string) => ItemDraft;
  updateItemDraft: (inspectionId: string, itemName: string, draft: ItemDraft) => void;
};

const WorkspaceContext = createContext<WorkspaceValue | null>(null);

function isWorkspaceState(value: unknown): value is WorkspaceState {
  return Boolean(value && typeof value === 'object' && Array.isArray((value as WorkspaceState).properties) && Array.isArray((value as WorkspaceState).team));
}

function readAsDataUrl(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error ?? new Error('Não foi possível ler a imagem.'));
    reader.readAsDataURL(file);
  });
}

export function WorkspaceProvider({ children }: Readonly<{ children: ReactNode }>) {
  const [state, setState] = useState<WorkspaceState>(seed);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed: unknown = JSON.parse(raw);
        if (isWorkspaceState(parsed)) setState({ ...seed, ...parsed, settings: { ...seed.settings, ...parsed.settings } });
      }
    } catch {
      // A demonstração continua disponível mesmo sem localStorage.
    } finally {
      setHydrated(true);
    }
  }, []);

  useEffect(() => {
    if (hydrated) window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, [hydrated, state]);

  const addProperty = useCallback((input: Pick<PropertyRecord, 'name' | 'address' | 'units'>) => {
    setState((current) => ({ ...current, properties: [{ ...input, id: `property-${Date.now()}`, inspections: 0, occupancy: '—' }, ...current.properties] }));
  }, []);

  const addMember = useCallback((input: Pick<TeamMember, 'name' | 'email' | 'role'>) => {
    const initials = input.name.split(' ').map((part) => part[0]).join('').slice(0, 2).toUpperCase();
    setState((current) => ({ ...current, team: [...current.team, { ...input, id: `member-${Date.now()}`, status: 'Convite pendente', initials, tone: 'slate' }] }));
  }, []);

  const updateSettings = useCallback((settings: Partial<WorkspaceState['settings']>) => {
    setState((current) => ({ ...current, settings: { ...current.settings, ...settings } }));
  }, []);

  const addPhoto = useCallback(async ({ inspectionId, itemName, file }: { inspectionId: string; itemName: string; file: File }) => {
    const dataUrl = await readAsDataUrl(file);
    setState((current) => ({ ...current, photos: [{ id: `photo-${Date.now()}-${Math.random().toString(16).slice(2)}`, inspectionId, itemName, fileName: file.name, dataUrl, createdAt: new Date().toISOString() }, ...current.photos] }));
  }, []);

  const removePhoto = useCallback((photoId: string) => {
    setState((current) => ({ ...current, photos: current.photos.filter((photo) => photo.id !== photoId) }));
  }, []);

  const getPhotos = useCallback((inspectionId: string, itemName?: string) => state.photos.filter((photo) => photo.inspectionId === inspectionId && (!itemName || photo.itemName === itemName)), [state.photos]);
  const getItemDraft = useCallback((inspectionId: string, itemName: string) => state.itemDrafts[`${inspectionId}:${itemName}`] ?? {}, [state.itemDrafts]);
  const updateItemDraft = useCallback((inspectionId: string, itemName: string, draft: ItemDraft) => {
    setState((current) => ({ ...current, itemDrafts: { ...current.itemDrafts, [`${inspectionId}:${itemName}`]: { ...current.itemDrafts[`${inspectionId}:${itemName}`], ...draft } } }));
  }, []);

  const value = useMemo(() => ({ ...state, hydrated, addProperty, addMember, updateSettings, addPhoto, removePhoto, getPhotos, getItemDraft, updateItemDraft }), [addMember, addPhoto, addProperty, getItemDraft, getPhotos, hydrated, removePhoto, state, updateItemDraft, updateSettings]);
  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace() {
  const value = useContext(WorkspaceContext);
  if (!value) throw new Error('useWorkspace must be used inside WorkspaceProvider');
  return value;
}
