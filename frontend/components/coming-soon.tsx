import { Icon, type IconName } from './icons';
import { PageHeading } from './dashboard-primitives';

export function ComingSoonPage({ title, description, icon }: Readonly<{ title: string; description: string; icon: IconName }>) {
  return <><PageHeading title={title} description={description} /><section className="panel empty-state"><div className="empty-state-inner"><div className="empty-state-icon"><Icon name={icon} size={29} /></div><h2>Estamos preparando esta área</h2><p>O fluxo visual já está previsto no produto. Na próxima etapa, ele será conectado aos dados reais da API.</p><button className="button button--primary" type="button">Voltar ao dashboard <Icon name="arrowRight" size={16} /></button></div></section></>;
}
