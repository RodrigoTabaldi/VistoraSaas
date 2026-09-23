import Link from 'next/link';
import { Icon } from '../../../../components/icons';
import { TriageCreateForm } from '../../../../components/triage-create-form';

export default function NewTriageRoute() {
  return (
    <>
      <div className="breadcrumb"><Link href="/triagens">Triagens</Link><Icon name="chevronRight" size={13} /><strong>Nova triagem</strong></div>
      <div className="page-heading detail-header"><div><h1>Nova triagem</h1><p>Crie uma inspeção a partir de um checklist e organize a próxima execução.</p></div></div>
      <section className="triage-create-layout"><TriageCreateForm /><aside className="panel triage-create-aside"><span className="triage-aside-icon"><Icon name="sparkles" size={22} /></span><h2>Um fluxo mais simples</h2><p>Depois de criada, a triagem aparece na Home e na lista para você acompanhar o progresso e concluir quando os itens forem verificados.</p><div className="triage-aside-list"><span><Icon name="check" size={15} /> Dados organizados</span><span><Icon name="check" size={15} /> Checklist reutilizável</span><span><Icon name="check" size={15} /> Relatório após conclusão</span></div></aside></section>
    </>
  );
}
