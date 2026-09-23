import { Icon } from '../../../../components/icons';
import { InspectionDetail } from '../../../../components/inspection-detail';
import { PageHeading, StatusBadge } from '../../../../components/dashboard-primitives';

export default async function InspectionDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const displayCode = id === 'vista-verde' ? 'VT-2048' : 'VT-2048';

  return (
    <>
      <div className="breadcrumb"><span>Vistorias</span><Icon name="chevronRight" size={13} /><span>{displayCode}</span><Icon name="chevronRight" size={13} /><strong>Execução</strong></div>
      <div className="page-heading detail-header"><div><div className="detail-title-line"><h1>Vistoria Residencial Vista Verde</h1><StatusBadge status="Em andamento" /></div><p>Realize a inspeção do imóvel, verifique os itens da lista e registre evidências quando necessário.</p></div><div className="detail-progress"><div className="detail-progress-label"><span>68% concluído</span></div><div className="progress-line"><span style={{ width: '68%' }} /></div><small>34 de 50 itens verificados</small></div></div>

      <section className="property-summary" aria-label="Resumo do imóvel">
        <div className="property-summary-item"><div className="property-thumb" /><div className="summary-copy"><strong>Residencial Vista Verde</strong><span>Rua das Palmeiras, 320 - Bloco B, Apto 102</span><small>São Paulo, SP</small></div></div>
        <div className="property-summary-item"><span className="summary-icon"><Icon name="users" size={18} /></span><div className="summary-copy"><span>Inquilino</span><strong>Mariana Silva</strong><small>CPF 123.456.789-00</small></div></div>
        <div className="property-summary-item"><span className="summary-icon"><Icon name="file" size={18} /></span><div className="summary-copy"><span>Tipo de vistoria</span><strong>Entrada</strong><small>Residencial</small></div></div>
        <div className="property-summary-item"><span className="summary-icon"><Icon name="calendar" size={18} /></span><div className="summary-copy"><span>Data agendada</span><strong>12 de abr. de 2024</strong><small>09:00 - 12:00</small></div></div>
        <div className="property-summary-item"><span className="summary-icon"><Icon name="users" size={18} /></span><div className="summary-copy"><span>Vistoriador responsável</span><strong>Carla Mendes</strong><small>CRECI 123456-F</small></div></div>
      </section>

      <InspectionDetail inspectionId={id} />
    </>
  );
}
