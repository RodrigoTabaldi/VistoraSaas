import { test } from 'node:test';
import assert from 'node:assert/strict';
import { csvCell } from '../lib/inspection-presentation.ts';
import { inspectionRow, getPresentationStatus } from '../lib/inspection-row.ts';

const inspection = { id: '12345678-0000-0000-0000-000000000000', unitId: 'unit', type: 'MoveIn', status: 'Draft',
  createdAtUtc: '2026-10-05T12:00:00Z', scheduledAtUtc: null, propertyName: 'Home', address: 'Street',
  unitIdentifier: '101', totalItems: 3, answeredItems: 2, hasReport: false };

test('checklist progress reflects answers rather than inspection status', () => {
  assert.equal(inspectionRow(inspection).progress, 67);
  assert.equal(inspectionRow({ ...inspection, totalItems: 0, answeredItems: 0 }).progress, 0);
  assert.equal(inspectionRow({ ...inspection, status: 'Completed' }).progress, 67);
});

test('completed inspections keep their status regardless of a past schedule', () => {
  assert.equal(getPresentationStatus('Completed', '2020-01-01T12:00:00Z'), 'Concluída');
  assert.equal(getPresentationStatus('Draft', '2020-01-01T12:00:00Z'), 'Atrasada');
  assert.equal(getPresentationStatus('Draft', '2099-01-01T12:00:00Z'), 'Agendada');
  assert.equal(getPresentationStatus('Draft', null), 'Em andamento');
});

test('property identity and report availability survive presentation mapping', () => {
  const row = inspectionRow({ ...inspection, hasReport: true });
  assert.equal(row.code, 'VIS-12345678');
  assert.equal(row.property, 'Home');
  assert.equal(row.city, 'Street · 101');
  assert.equal(row.hasReport, true);
});

test('CSV neutralizes spreadsheet formulas and preserves quotes', () => {
  for (const value of ['=SUM(A1)', '+CMD', '-1+2', '@SUM(A1)', '\t=1']) assert.equal(csvCell(value), `"'${value}"`);
  assert.equal(csvCell('Casa "A"'), '"Casa ""A"""');
});
