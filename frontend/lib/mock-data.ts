export type InspectionStatus = 'Agendada' | 'Em andamento' | 'Concluída' | 'Pendente' | 'Atrasada';
export type ChecklistStatus = 'Conforme' | 'Atenção' | 'Não conforme' | 'Não verificado';

export type InspectionRow = {
  id: string;
  code: string;
  property: string;
  city: string;
  type: 'Entrada' | 'Saída' | 'Periódica';
  responsible: string;
  responsibleInitials: string;
  responsibleTone: string;
  date: string;
  time: string;
  status: InspectionStatus;
  progress: number;
};

export const inspections: InspectionRow[] = [
  { id: 'vista-verde', code: 'VIS-2024-001', property: 'Residencial Vista Verde', city: 'São Paulo, SP', type: 'Entrada', responsible: 'Carla Mendes', responsibleInitials: 'CM', responsibleTone: 'rose', date: '12 abr 2024', time: '09:00', status: 'Agendada', progress: 0 },
  { id: 'parque-flores', code: 'VIS-2024-002', property: 'Condomínio Parque das Flores', city: 'Osasco, SP', type: 'Periódica', responsible: 'Rafael Lima', responsibleInitials: 'RL', responsibleTone: 'blue', date: '12 abr 2024', time: '14:30', status: 'Em andamento', progress: 60 },
  { id: 'solar-paulista', code: 'VIS-2024-003', property: 'Edifício Solar Paulista', city: 'São Paulo, SP', type: 'Saída', responsible: 'Juliana Costa', responsibleInitials: 'JC', responsibleTone: 'purple', date: '13 abr 2024', time: '10:00', status: 'Concluída', progress: 100 },
  { id: 'bela-vista', code: 'VIS-2024-004', property: 'Condomínio Bela Vista', city: 'Santo André, SP', type: 'Entrada', responsible: 'Lucas Ferreira', responsibleInitials: 'LF', responsibleTone: 'slate', date: '13 abr 2024', time: '16:00', status: 'Pendente', progress: 20 },
  { id: 'harmonia', code: 'VIS-2024-005', property: 'Residencial Harmonia', city: 'São Bernardo do Campo, SP', type: 'Periódica', responsible: 'Carla Mendes', responsibleInitials: 'CM', responsibleTone: 'rose', date: '14 abr 2024', time: '09:30', status: 'Atrasada', progress: 0 },
  { id: 'acacias', code: 'VIS-2024-006', property: 'Condomínio das Acácias', city: 'Diadema, SP', type: 'Saída', responsible: 'Rafael Lima', responsibleInitials: 'RL', responsibleTone: 'blue', date: '14 abr 2024', time: '11:00', status: 'Agendada', progress: 0 },
  { id: 'monte-verde', code: 'VIS-2024-007', property: 'Residencial Monte Verde', city: 'São Paulo, SP', type: 'Periódica', responsible: 'Juliana Costa', responsibleInitials: 'JC', responsibleTone: 'purple', date: '14 abr 2024', time: '14:00', status: 'Em andamento', progress: 45 },
  { id: 'parque-central', code: 'VIS-2024-008', property: 'Edifício Parque Central', city: 'São Caetano do Sul, SP', type: 'Entrada', responsible: 'Lucas Ferreira', responsibleInitials: 'LF', responsibleTone: 'slate', date: '16 abr 2024', time: '10:00', status: 'Concluída', progress: 100 },
  { id: 'ipe-amarelo', code: 'VIS-2024-009', property: 'Condomínio Ipê Amarelo', city: 'Mauá, SP', type: 'Periódica', responsible: 'Carla Mendes', responsibleInitials: 'CM', responsibleTone: 'rose', date: '16 abr 2024', time: '15:30', status: 'Pendente', progress: 10 },
  { id: 'palmeiras', code: 'VIS-2024-010', property: 'Residencial das Palmeiras', city: 'Ribeirão Pires, SP', type: 'Saída', responsible: 'Rafael Lima', responsibleInitials: 'RL', responsibleTone: 'blue', date: '17 abr 2024', time: '09:00', status: 'Agendada', progress: 0 },
];

export const dashboardSchedule = [
  { day: 'Hoje', date: '12 abr', time: '09:00', property: 'Residencial Vista Verde', city: 'São Paulo, SP', type: 'Entrada', responsible: 'Carla Mendes', initials: 'CM', tone: 'rose', status: 'Confirmada' },
  { day: 'Hoje', date: '12 abr', time: '14:30', property: 'Condomínio Parque das Flores', city: 'Osasco, SP', type: 'Periódica', responsible: 'Rafael Lima', initials: 'RL', tone: 'blue', status: 'Confirmada' },
  { day: 'Amanhã', date: '13 abr', time: '10:00', property: 'Edifício Solar Paulista', city: 'São Paulo, SP', type: 'Saída', responsible: 'Juliana Costa', initials: 'JC', tone: 'purple', status: 'Pendente' },
  { day: 'Amanhã', date: '13 abr', time: '16:00', property: 'Condomínio Bela Vista', city: 'Santo André, SP', type: 'Entrada', responsible: 'Lucas Ferreira', initials: 'LF', tone: 'slate', status: 'Pendente' },
  { day: '14 abr', date: 'Seg', time: '09:30', property: 'Residencial Harmonia', city: 'São Bernardo do Campo, SP', type: 'Periódica', responsible: 'Carla Mendes', initials: 'CM', tone: 'rose', status: 'Agendada' },
];

export const detailRooms: Array<{
  name: string;
  icon: 'sofa' | 'bed' | 'kitchen' | 'bath' | 'tree';
  verified: number;
  total: number;
  items: Array<{ name: string; status: ChecklistStatus; note: string; photoTone: string }>;
}> = [
  {
    name: 'Sala', icon: 'sofa', verified: 4, total: 6,
    items: [
      { name: 'Paredes e pintura', status: 'Conforme', note: 'Pintura em bom estado, sem manchas.', photoTone: 'wall' },
      { name: 'Piso', status: 'Atenção', note: 'Pequenos riscos no piso, conforme foto.', photoTone: 'floor' },
      { name: 'Janelas', status: 'Conforme', note: 'Funcionamento normal.', photoTone: 'window' },
      { name: 'Portas', status: 'Conforme', note: 'Sem avarias.', photoTone: 'door' },
      { name: 'Tomadas e interruptores', status: 'Não conforme', note: 'Tomada da parede lateral não funciona.', photoTone: 'socket' },
      { name: 'Iluminação', status: 'Conforme', note: 'Todas as luminárias funcionando.', photoTone: 'light' },
    ],
  },
  { name: 'Quarto', icon: 'bed', verified: 6, total: 8, items: [] },
  { name: 'Cozinha', icon: 'kitchen', verified: 6, total: 10, items: [] },
  { name: 'Banheiro', icon: 'bath', verified: 4, total: 8, items: [] },
  { name: 'Área externa', icon: 'tree', verified: 2, total: 8, items: [] },
];

export const recentActivities = [
  { icon: 'clock', title: 'Vistoria concluída', subtitle: 'Apartamento 302 - Edifício Solar Paulista', time: 'Há 2 horas' },
  { icon: 'home', title: 'Novo imóvel cadastrado', subtitle: 'Residencial Harmonia - São Bernardo do Campo, SP', time: 'Há 4 horas' },
  { icon: 'file', title: 'Relatório gerado', subtitle: 'Condomínio Parque das Flores', time: 'Há 6 horas' },
  { icon: 'calendar', title: 'Vistoria agendada', subtitle: 'Casa - Jardim das Acácias', time: 'Há 1 dia' },
];

export const cityMetrics = [
  { city: 'São Paulo', value: 72, percent: 29 },
  { city: 'Osasco', value: 48, percent: 19 },
  { city: 'Santo André', value: 36, percent: 15 },
  { city: 'São Bernardo do Campo', value: 32, percent: 13 },
  { city: 'Guarulhos', value: 24, percent: 10 },
  { city: 'Diadema', value: 18, percent: 7 },
  { city: 'Barueri', value: 12, percent: 5 },
  { city: 'Outras', value: 10, percent: 4 },
];

export const occurrenceTypes = [
  { label: 'Manutenção', value: 38, percent: 31, color: 'green' },
  { label: 'Danos estruturais', value: 26, percent: 21, color: 'teal' },
  { label: 'Instalações elétricas', value: 18, percent: 15, color: 'yellow' },
  { label: 'Hidráulica', value: 16, percent: 13, color: 'orange' },
  { label: 'Acabamento', value: 12, percent: 10, color: 'red' },
  { label: 'Outros', value: 12, percent: 10, color: 'slate' },
];

export const typeMetrics = [
  { label: 'Entrada', value: 78, percent: 37, color: 'dark' },
  { label: 'Saída', value: 64, percent: 31, color: 'green' },
  { label: 'Periódica', value: 50, percent: 24, color: 'mint' },
  { label: 'Entrega', value: 12, percent: 6, color: 'pale' },
  { label: 'Outros', value: 4, percent: 2, color: 'gray' },
];
