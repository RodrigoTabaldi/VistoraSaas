# Validação do Vistora

Use um ambiente local ou de homologação com dados fictícios. A aprovação automatizada não substitui a validação funcional abaixo.

## Preparação

Pré-requisitos: Docker em execução, SDK definido em `global.json`, Node.js 24 e dependências instaladas.

Na raiz:

```powershell
dotnet restore backend/Vistora.slnx
Push-Location frontend
npm.cmd ci
Pop-Location
powershell -File scripts/validate.ps1
docker compose up --build --wait --wait-timeout 300
```

O script interrompe na primeira falha. Os testes de integração criam bancos descartáveis e aplicam as migrations reais. Sem Docker, `-WithoutDatabase` executa somente checks parciais e não aprova a validação do banco.

Acesse http://localhost:3000. A porta padrão da API é 8082 (`VISTORA_API_PORT` pode alterá-la). No Compose local em Development, a API aplica migrations antes de ficar saudável. Não use esse procedimento como deploy de produção.

## Banco e conexões

Execute uma consulta administrativa local, somente para diagnóstico:

```powershell
docker compose exec postgres sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

```sql
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT tablename, indexname, indexdef FROM pg_indexes
WHERE schemaname = 'vistora' ORDER BY tablename, indexname;
SHOW max_connections;
SELECT application_name, state, count(*) FROM pg_stat_activity
WHERE datname = current_database() GROUP BY application_name, state;
SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'vistora_app';
```

Confirme a migration `20261005032246_AddDurableOperations`, os índices de unicidade de laudos e idempotência e `rolsuper=false`, `rolbypassrls=false`. Faça requisições antes de conferir conexões: pools abrem conexões sob demanda.

API e Worker têm limites locais de 30 e 10 conexões, configuráveis por `VISTORA_API_DB_POOL_SIZE` e `VISTORA_WORKER_DB_POOL_SIZE`. São valores iniciais para homologação, não capacidade comprovada. Some os limites de todas as réplicas e reserve conexões para administração, migrations e health checks. Compare com `max_connections` e meça latência e timeouts sob carga. As configurações `Database:*` prevalecem sobre o máximo e nome na connection string; demais opções são preservadas.

## Aceitação funcional

Registre resultado, data e evidência de cada cenário:

- [ ] Cadastrar duas empresas independentes e entrar/sair das contas.
- [ ] Criar uma vistoria pelo endereço, adicionar ambientes e itens, responder e anexar JPEG/PNG.
- [ ] Tentar concluir com itens pendentes: deve ser recusado. Responder todos e concluir.
- [ ] Aguardar o laudo, baixar e conferir endereço, checklist e fotos.
- [ ] Tentar aprovar sem aceite: deve ser recusado. Registrar assinatura, conferir o aceite e aprovar.
- [ ] Tentar editar checklist concluído: deve ser recusado.
- [ ] Criar vistoria de saída da mesma unidade e conferir comparação com a entrada aprovada.
- [ ] Conferir pesquisa, filtros, paginação e agenda.
- [ ] Convidar Vistoriador e Leitor. O Leitor não deve escrever; links usados não devem aceitar novamente.
- [ ] Na segunda empresa, tentar acessar IDs de vistoria, foto e laudo da primeira: os dados não devem ser retornados.
- [ ] Repetir uma escrita com a mesma `Idempotency-Key` e corpo: mesmo resultado sem duplicação. Com corpo diferente: conflito.
- [ ] Em ambiente isolado, interromper RabbitMQ e reiniciar Worker; após recuperação, conferir entrega dos laudos pendentes sem duplicar registros. Consulte `OPERATIONS.md`.

Para interromper o ambiente mantendo os dados: `docker compose down`. Não remova volumes para corrigir falhas.

## Critério de aprovação

Todos os checks automatizados, consultas de banco e cenários funcionais devem passar. Registre falhas antes de liberar o piloto. WebP, carga, backup/restauração e infraestrutura de produção exigem validação específica conforme `OPERATIONS.md`.
