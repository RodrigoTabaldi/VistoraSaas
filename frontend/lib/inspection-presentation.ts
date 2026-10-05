export function csvCell(value: string) {
  // Spreadsheet applications can execute formulas even in quoted CSV cells.
  const safe = /^[=+@\-\t\r\n]/.test(value) ? `'${value}` : value;
  return `"${safe.replaceAll('"', '""')}"`;
}

