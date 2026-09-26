# Vistora

Base do SaaS multiempresa para vistorias imobiliárias. A fundação inclui API, persistência PostgreSQL com isolamento por organização, criação da organização e do primeiro usuário, e login por e-mail e senha. Imóveis, vistorias, uploads e laudos ainda não têm fluxos completos de produto. A base de acesso ao Supabase Storage via protocolo S3 está disponível, mas ainda não há fluxo de upload nem entidade de negócio conectada a ela.

## Estrutura

```text
frontend/                       Next.js + React + TypeScript + PWA
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

- `backend/Vistora.Infrastructure/Persistence/PostgreSql/`: `DbContext`, mapeamentos, migrations e interceptors de isolamento por organização.
- `infra/postgresql/`: scripts operacionais e dados de seed apenas para desenvolvimento.

O cadastro cria uma organização, uma associação de usuário com papel de administrador e as credenciais da conta em uma transação. A senha é armazenada como hash; o usuário da organização continua sujeito às políticas RLS. A migration `AddAccountCredentials` cria a tabela de credenciais e o índice global de e-mail normalizado.

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

O servidor de desenvolvimento escuta em todas as interfaces de rede (`0.0.0.0`) para permitir acesso por outros dispositivos. O IP/hostname usado no navegador precisa estar listado em `allowedDevOrigins` no `frontend/next.config.ts`. O proxy `/api/*` roda no servidor Next e usa `VISTORA_API_URL`; quando frontend e API estão na mesma máquina, o padrão `http://localhost:8080` continua válido. Se a API estiver em outro host, defina `VISTORA_API_URL` no ambiente do processo do frontend com o endereço alcançável a partir da máquina que executa o Next e reinicie o servidor.

O frontend usa o App Router do Next.js, possui manifest e service worker mínimos para evolução como PWA e gera uma imagem standalone via `frontend/Dockerfile`. Ele não faz parte do Compose local nesta etapa.

O login fica em `/` e o cadastro em `/cadastro`. O Next encaminha chamadas `/api/*` para `VISTORA_API_URL`, que por padrão aponta para `http://localhost:8080`; configure essa variável no ambiente do frontend se a API estiver em outro endereço. O cadastro entra na conta e abre o dashboard.

Para abrir o frontend em desenvolvimento por outro computador na rede, inclua o hostname ou IP usado no navegador (sem protocolo ou porta) em `allowedDevOrigins` no `frontend/next.config.ts` e reinicie `npm.cmd run dev`. O endereço `10.1.18.116` está autorizado para este ambiente. O Next bloqueia os scripts de desenvolvimento para origens não autorizadas.

## Variáveis de ambiente

`.env.example` contém as variáveis para PostgreSQL, Redis, RabbitMQ, Supabase Storage S3 e OIDC/JWT externo. Em `Development`, a API aplica as migrations pendentes ao iniciar; fora desse ambiente, aplique migrations no processo de implantação. Para o Supabase, use o endpoint S3 direto, o region, bucket e as S3 Access Keys criadas nas configurações de Storage. Essas chaves têm acesso amplo e devem ficar apenas no backend e no arquivo `.env`, nunca no frontend.

## Supabase Storage S3

O adaptador está em `backend/Vistora.Infrastructure/Storage/S3/`. Ele expõe `IPrivateObjectStorage` para upload, remoção e URLs temporárias de download em bucket privado. A implementação usa URLs assinadas; os arquivos não ficam públicos e não são armazenados no PostgreSQL.

## Health checks

- API: `GET /health`, provido pelo health check do ASP.NET Core.
- Worker: o `WorkerReadinessHealthCheck` é executado no startup. Falha de health impede o processo de iniciar; sucesso é registrado no log.

## API, cache e mensageria

- O backend expõe propriedades, unidades, vistorias, cômodos, itens, evidências e relatórios em `/api/v1`. Uploads usam `multipart/form-data` com o campo `file`.
- `POST /api/v1/auth/register` cria uma organização e sua conta de administrador; `POST /api/v1/auth/login` valida a senha e inicia uma sessão em cookie `HttpOnly`. O e-mail é único sem diferenciar maiúsculas de minúsculas. `POST /api/v1/auth/logout` encerra a sessão.
- Os endpoints de negócio e `/api/v1/me` exigem autenticação por cookie da conta local ou, quando OIDC está configurado, por JWT com a claim `organization_id` (ou `tenant_id`/`org_id`). O tenant é extraído de uma claim confiável.
- Atualizações de itens e mudanças de status exigem `rowVersion`, evitando sobrescrever alterações concorrentes. O ciclo de status é `Draft -> Completed -> Approved`.
- `POST /api/v1/messages` publica um envelope JSON durável na fila RabbitMQ configurada em `Messaging:RabbitMq:QueueName`.
- O Worker consome a fila com confirmação manual (`ACK`) e reprocessa mensagens que falharem (`NACK` com requeue).
- Mensagens processadas ficam no Redis por uma hora na chave `vistora:message:{id}`. Esse fluxo é operacional e não grava no PostgreSQL.
- O endpoint `GET /health` verifica Redis e RabbitMQ, além dos checks registrados pela aplicação.

O endpoint de mensagens é uma fundação técnica para casos de uso futuros; ele ainda não substitui um outbox transacional, que deverá ser avaliado quando houver eventos de domínio persistidos.

## Nota sobre o template legado

Os arquivos padrão existentes na raiz (`Vistora.csproj`, `Program.cs` etc.) foram preservados para evitar remoção não solicitada. O `Vistora.slnx` foi atualizado e é a solution ativa, usando exclusivamente os projetos em `backend/`.
