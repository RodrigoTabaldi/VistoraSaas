'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { Icon } from './icons';
import { useInspections } from '../lib/inspection-store';
import type { InspectionRow } from '../lib/mock-data';

const propertyOptions = [
  { property: 'Residencial Vista Verde', city: 'São Paulo, SP', unit: 'Bloco B · Apto 102' },
  { property: 'Condomínio Parque das Flores', city: 'Osasco, SP', unit: 'Torre 2 · Apto 804' },
  { property: 'Edifício Solar Paulista', city: 'São Paulo, SP', unit: 'Apto 302' },
  { property: 'Condomínio Bela Vista', city: 'Santo André, SP', unit: 'Bloco A · Apto 41' },
  { property: 'Residencial Harmonia', city: 'São Bernardo do Campo, SP', unit: 'Casa 12' },
];

const responsibleOptions = [
  { name: 'Carla Mendes', initials: 'CM', tone: 'rose' },
  { name: 'Rafael Lima', initials: 'RL', tone: 'blue' },
  { name: 'Juliana Costa', initials: 'JC', tone: 'purple' },
  { name: 'Lucas Ferreira', initials: 'LF', tone: 'slate' },
];

function formatDate(value: string) {
  const date = new Date(`${value}T12:00:00`);
  return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' }).replace('.', '');
}

export function TriageCreateForm() {
  const router = useRouter();
  const { createInspection } = useInspections();
  const [property, setProperty] = useState(propertyOptions[0].property);
  const [unit, setUnit] = useState(propertyOptions[0].unit);
  const [type, setType] = useState<InspectionRow['type']>('Entrada');
  const [responsible, setResponsible] = useState(responsibleOptions[0].name);
  const [date, setDate] = useState('2024-04-18');
  const [time, setTime] = useState('09:00');
  const [checklist, setChecklist] = useState('Checklist residencial completo');
  const [error, setError] = useState('');

  const selectedProperty = propertyOptions.find((item) => item.property === property) ?? propertyOptions[0];
  const selectedResponsible = responsibleOptions.find((item) => item.name === responsible) ?? responsibleOptions[0];

  function handlePropertyChange(value: string) {
    setProperty(value);
    const next = propertyOptions.find((item) => item.property === value);
    if (next) setUnit(next.unit);
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!property || !unit || !date || !time) {
      setError('Preencha imóvel, unidade, data e horário para criar a triagem.');
      return;
    }

    createInspection({
      property,
      city: selectedProperty.city,
      type,
      responsible: selectedResponsible.name,
      responsibleInitials: selectedResponsible.initials,
      responsibleTone: selectedResponsible.tone,
      date: formatDate(date),
      time,
    });
    router.push('/triagens?created=1');
  }

  return (
    <form className="triage-form" onSubmit={handleSubmit}>
      <div className="triage-form-intro"><span className="triage-form-icon"><Icon name="clipboard" size={22} /></span><div><h2>Dados da triagem</h2><p>Defina o contexto inicial. O checklist poderá ser preenchido durante a execução.</p></div></div>
      <div className="form-grid">
        <label className="form-field"><span>Imóvel</span><select value={property} onChange={(event) => handlePropertyChange(event.target.value)}>{propertyOptions.map((item) => <option key={item.property}>{item.property}</option>)}</select></label>
        <label className="form-field"><span>Unidade</span><input value={unit} onChange={(event) => setUnit(event.target.value)} placeholder="Ex.: Bloco A · Apto 101" required /></label>
        <label className="form-field"><span>Tipo de triagem</span><select value={type} onChange={(event) => setType(event.target.value as InspectionRow['type'])}><option>Entrada</option><option>Saída</option><option>Periódica</option></select></label>
        <label className="form-field"><span>Responsável</span><select value={responsible} onChange={(event) => setResponsible(event.target.value)}>{responsibleOptions.map((item) => <option key={item.name}>{item.name}</option>)}</select></label>
        <label className="form-field"><span>Data</span><input type="date" value={date} onChange={(event) => setDate(event.target.value)} required /></label>
        <label className="form-field"><span>Horário</span><input type="time" value={time} onChange={(event) => setTime(event.target.value)} required /></label>
        <label className="form-field form-field--full"><span>Checklist base</span><select value={checklist} onChange={(event) => setChecklist(event.target.value)}><option>Checklist residencial completo</option><option>Checklist de entrada</option><option>Checklist de saída</option><option>Checklist de áreas comuns</option></select><small>O checklist define os ambientes e itens que serão avaliados.</small></label>
      </div>
      {error && <p className="form-feedback form-feedback--error" role="alert">{error}</p>}
      <div className="triage-form-actions"><Link className="button button--outline" href="/triagens">Cancelar</Link><button className="button button--primary" type="submit"><Icon name="plus" size={17} /> Criar triagem</button></div>
    </form>
  );
}
