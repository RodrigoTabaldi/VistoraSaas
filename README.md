# Vistora

Base do SaaS multiempresa para vistorias imobiliárias. Esta etapa estabelece a arquitetura e a execução local; não inclui banco de dados, autenticação, imóveis, vistorias, uploads ou laudos. A base de acesso ao Supabase Storage via protocolo S3 está disponível, mas ainda não há fluxo de upload nem entidade de negócio conectada a ela.

## Estrutura

```text
frontend/                       React + TypeScript + PWA
backend/
  Vistora.Domain/               Regras e modelos de domínio (sem dependências)
  Vistora.Application/          Casos de uso e contratos
  Vistora.Infrastructure/       Adaptadores de infraestrutura
  Vistora.Api/                  ASP.NET Core API
  Vistora.Worker/               Processos assíncronos .NET
  Vistora.Tests/                Testes automatizados
infra/                          Espaço para configurações de infraestrutura
docs/                           Documentação de produto e arquitetura
```

## PostgreSQL: pontos de implementação

- `backend/Vistora.Infrastructure/Persistence/PostgreSql/`: código de persistência .NET. O `README.md` local define os diretórios para `DbContext`, mapeamentos, migrations e repositórios.
- `infra/postgresql/`: scripts operacionais e dados de seed apenas para desenvolvimento.

As pastas são intencionalmente vazias nesta etapa. Não há pacote de Entity Framework, schema, migration ou conexão de banco implementados.

As referências respeitam a direção da arquitetura limpa: `Application` depende de `Domain`; `Infrastructure` depende de `Application` e `Domain`; API e Worker compõem as camadas internas.

## Pré-requisitos

- .NET SDK 10.0.400 (ou compatível com `global.json`)
- Node.js 24+
- Docker Desktop para a execução via Compose

## Execução local

1. Copie `.env.example` para `.env` e ajuste os valores locais quando necessário.
2. Restaure e valide o backend:

   ```powershell
   dotnet restore Vistora.slnx
   dotnet build Vistora.slnx --no-restore
   dotnet test backend/Vistora.Tests/Vistora.Tests.csproj --no-build
   ```

3. Inicie API, Worker e dependências configuráveis:

   ```powershell
   docker compose up --build
   ```

4. Confirme a API em `http://localhost:8080/health`.

Para executar o frontend separadamente:

```powershell
Set-Location frontend
npm.cmd install
npm.cmd run dev
```

O frontend possui manifest e service worker mínimos, permitindo sua evolução como PWA. O container do frontend existe em `frontend/Dockerfile`, mas não faz parte do Compose local nesta etapa.

## Variáveis de ambiente

`.env.example` contém as variáveis para PostgreSQL, Redis, RabbitMQ, Supabase Storage S3 e OIDC/JWT. Para o Supabase, use o endpoint S3 direto, o region, bucket e as S3 Access Keys criadas nas configurações de Storage. Essas chaves têm acesso amplo e devem ficar apenas no backend e no arquivo `.env`, nunca no frontend.

## Supabase Storage S3

O adaptador está em `backend/Vistora.Infrastructure/Storage/S3/`. Ele expõe `IPrivateObjectStorage` para upload, remoção e URLs temporárias de download em bucket privado. A implementação usa URLs assinadas; os arquivos não ficam públicos e não são armazenados no PostgreSQL.

## Health checks

- API: `GET /health`, provido pelo health check do ASP.NET Core.
- Worker: o `WorkerReadinessHealthCheck` é executado no startup. Falha de health impede o processo de iniciar; sucesso é registrado no log.

## API, cache e mensageria

- O backend expõe propriedades, unidades, vistorias, cômodos, itens, evidências e relatórios em `/api/v1`. Uploads usam `multipart/form-data` com o campo `file`.
- Com OIDC configurado, envie um JWT com a claim `organization_id` (ou `tenant_id`/`org_id`); os endpoints de negócio exigem autenticação. Em Development sem Authority, o fallback local é o header `X-Organization-Id`.
- Atualizações de itens e mudanças de status exigem `rowVersion`, evitando sobrescrever alterações concorrentes. O ciclo de status é `Draft -> Completed -> Approved`.
- `POST /api/v1/messages` publica um envelope JSON durável na fila RabbitMQ configurada em `Messaging:RabbitMq:QueueName`.
- O Worker consome a fila com confirmação manual (`ACK`) e reprocessa mensagens que falharem (`NACK` com requeue).
- Mensagens processadas ficam no Redis por uma hora na chave `vistora:message:{id}`. Esse fluxo é operacional e não grava no PostgreSQL.
- O endpoint `GET /health` verifica Redis e RabbitMQ, além dos checks registrados pela aplicação.

O endpoint de mensagens é uma fundação técnica para casos de uso futuros; ele ainda não substitui um outbox transacional, que deverá ser avaliado quando houver eventos de domínio persistidos. Nenhuma migration ou alteração de schema é necessária para esta etapa.

## Nota sobre o template legado

Os arquivos padrão existentes na raiz (`Vistora.csproj`, `Program.cs` etc.) foram preservados para evitar remoção não solicitada. O `Vistora.slnx` foi atualizado e é a solution ativa, usando exclusivamente os projetos em `backend/`.
