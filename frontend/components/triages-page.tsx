'use client';

import Link from 'next/link';
import { Icon } from './icons';
import { InspectionsTable } from './inspections-table';
import { MetricCard, PageHeading, PanelHeader } from './dashboard-primitives';
import { useInspections } from '../lib/inspection-store';

function includesStatus(status: string, value: string) {
  return status.toLowerCase().includes(value);
}

export function TriagesPage() {
  const { inspections } = useInspections();
  const pending = inspections.filter((item) => !includesStatus(item.status, 'conclu')).length;
  const completed = inspections.filter((item) => includesStatus(item.status, 'conclu')).length;
  const completionRate = inspections.length ? Math.round((completed / inspections.length) * 100) : 0;

  return (
    <>
      <PageHeading title="Triagens" description="Crie, acompanhe e conclua as inspeções dos seus imóveis em um só fluxo." side="“Clareza em cada etapa, segurança em cada decisão.”" />
      <div className="triage-page-actions"><Link className="button button--primary" href="/triagens/nova"><Icon name="plus" size={17} /> Nova triagem</Link></div>

      <section className="stats-grid stats-grid--three" aria-label="Indicadores de triagens">
        <MetricCard icon="calendar" label="Vistorias registradas" value={String(inspections.length)} note="na organização" />
        <MetricCard icon="file" label="Em execução" value={String(pending)} note="ainda não concluídas" tone="blue" />
        <MetricCard icon="check" label="Taxa de conclusão" value={`${completionRate}%`} note="de todas as vistorias" />
      </section>

      <section className="triage-workspace">
        <article className="panel">
          <PanelHeader title="Todas as triagens" action={<Link className="panel-link" href="/triagens/nova"><Icon name="plus" size={15} /> Criar a partir de checklist</Link>} />
          <div className="panel-body"><InspectionsTable entityLabel="triagens" /></div>
        </article>
        <aside className="triage-side-panel panel">
          <div className="panel-header"><h2 className="panel-title">Fluxo da triagem</h2></div>
          <div className="triage-flow">
            <div className="triage-flow-step triage-flow-step--active"><span>1</span><div><strong>Agendar</strong><small>Escolha imóvel, tipo e responsável.</small></div></div>
            <div className="triage-flow-line" />
            <div className="triage-flow-step"><span>2</span><div><strong>Executar checklist</strong><small>Registre respostas, notas e evidências.</small></div></div>
            <div className="triage-flow-line" />
            <div className="triage-flow-step"><span>3</span><div><strong>Concluir e gerar relatório</strong><small>Finalize a triagem e acompanhe o processamento.</small></div></div>
          </div>
          <Link className="button button--soft triage-flow-cta" href="/triagens/nova"><Icon name="sparkles" size={16} /> Começar agora</Link>
        </aside>
      </section>
    </>
  );
}
