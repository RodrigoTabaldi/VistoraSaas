# Operação do Vistora

## Critérios antes de produção

- Executar `dotnet test backend/Vistora.Tests/Vistora.Tests.csproj --no-restore` com Docker disponível. A suite inclui migrations, RLS e idempotência transacional em PostgreSQL real.
- Executar `npm.cmd test` e `npm.cmd run build` no frontend.
- Validar o fluxo completo em homologção: cadastro, vistoria pelo endereço, checklist, foto, conclusão, PDF, aceite e aprovação; repetir com Admin, Vistoriador e Leitor e com duas empresas.
- Simular indisponibilidade de RabbitMQ e reinício do Worker; comprovar que os eventos e laudos pendentes são recuperados, sem duplicar registros de negócio.
- Medir resposta, uso de memória e filas com a quantidade prevista de empresas, imóveis, itens e fotos. Definir metas de disponibilidade, perda máxima de dados e tempo de recuperação com o responsável pelo produto.

## Atualização

1. Fazer backup consistente do PostgreSQL e verificar disponibilidade dos objetos S3 e das chaves de autenticação.
2. Drenar escritas durante a primeira mudança de idempotência Redis para PostgreSQL: resultados anteriores não serão reaproveitados.
3. Aplicar migrations usando credencial administrativa exclusiva. API e Worker devem continuar usando `vistora_app`, sem SUPERUSER nem BYPASSRLS.
4. Atualizar API e Worker; verificar `/health` e os logs. As tabelas novas são aditivas; não excluir tabelas para voltar uma imagem de aplicação.
5. Criar uma vistoria de teste e concluir o fluxo em uma empresa de homologção.

## Filas e laudos

- Monitorar quantidade e idade dos registros não publicados em `vistora.outbox_messages` e dos jobs Pending, Processing e Failed em `vistora.report_jobs`.
- `/health` indica conectividade, mas não garante que filas estejam sendo drenadas. Alertar quando houver crescimento contínuo de pendências ou erros persistentes no Worker.
- O scanner recebe jobs novos em ciclos de cinco segundos; falhas respeitam intervalo de um minuto entre tentativas. Processamento abandonado é recuperado após 30 minutos.
- Consultar o estado na tela da vistoria. Para falha final, corrigir a causa de armazenamento, dados ou PDF e usar o reenvio de laudo como administrador. Não alterar manualmente o estado do checklist concluído.
- Outbox entrega pelo menos uma vez. Manter o ID original e idempotência no consumidor; nunca assumir que uma mensagem será entregue uma única vez.
- Não apagar resultados de idempotência sem definir e comunicar a janela de suporte a repetições. Após remoção, a chave volta a representar uma nova operação.

## Backup e restauração

Fazer backup do banco com uma ferramenta PostgreSQL, dos objetos privados S3 e das chaves persistidas da autenticação. Usar armazenamento separado, criptografia e acesso restrito. A frequência depende da perda máxima de dados aceita pelo negócio.

Ensaiar a restauração em ambiente isolado e medir o tempo. Conferir contagens por empresa, acessos entre empresas, hashes de fotos e PDFs, jobs pendentes e aceite. Um backup existente sem teste de restauração não comprova recuperabilidade.

## Limites conhecidos

Vistorias, agenda e laudos usam filtros e paginação no servidor. Indicadores são agregados no banco; o provider carrega somente as cinco vistorias mais recentes. As pesquisas textuais precisam ser medidas na base prevista e podem requerer índices de busca quando houver evidência de gargalo.

Testes unitários e build não substituem validação em navegador, testes de carga nem ensaios de recuperação. Este documento define os procedimentos; a infraestrutura de produção, alertas e backups devem ser configurados e comprovados no ambiente de destino.
