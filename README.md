# Vistora

SaaS multiempresa para vistorias imobiliárias. O repositório contém frontend Next.js, API e Worker .NET 10, PostgreSQL com RLS por organização, Redis, RabbitMQ e armazenamento S3 privado. A arquitetura separa domínio, casos de uso, infraestrutura e interfaces.

## Execução local

Pré-requisitos: .NET SDK compatível com `global.json`, Node.js 24+ e Docker Desktop em execução.

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Abra `http://localhost:3000`; a API responde em `http://localhost:8080/health`. O Compose inicia PostgreSQL, Redis, RabbitMQ e MinIO, cria o bucket local e aplica migrations em `Development`. O MinIO local usa HTTP apenas dentro da rede do Compose. A interface do MinIO fica em `http://localhost:9001`.

Para validar separadamente:

```powershell
dotnet restore backend/Vistora.slnx
dotnet build backend/Vistora.slnx --no-restore
dotnet test backend/Vistora.Tests/Vistora.Tests.csproj --no-restore
Set-Location frontend
npm.cmd ci
npm.cmd run build
```

Os testes de integração PostgreSQL usam Testcontainers e exigem Docker disponível. O frontend usa o proxy `/api/*` definido em `frontend/next.config.ts`. Fora do Compose, `VISTORA_API_URL` deve apontar para a API acessível pelo servidor Next.

## Banco e isolamento

O cadastro cria a organização, o usuário administrador e as credenciais em transação. Credenciais são armazenadas como hash. Entidades de negócio usam filtro por organização no EF Core e políticas RLS no PostgreSQL. API e Worker usam a role `vistora_app`; o `DatabaseRoleGuard` impede execução com `SUPERUSER` ou `BYPASSRLS`. Em `Development`, apenas a etapa de migration usa `VISTORA_POSTGRES_MIGRATION_CONNECTION` administrativa. Em produção, aplique migrations antes de iniciar os serviços. A migration cria a role local com senha `change-me`; troque a senha antes de qualquer uso fora do desenvolvimento.

## Fluxo implementado

O usuário cadastra imóveis e unidades, cria e edita modelos completos de checklist e abre vistorias de entrada ou saída. A vistoria copia ambientes e itens do modelo, registra respostas, observações e fotos, e pode receber data e hora em UTC. Uma saída é vinculada à entrada aprovada mais recente da mesma unidade, quando existe, e a interface compara respostas e observações. O Worker gera um PDF com dados do imóvel, respostas, observações e evidências JPEG/PNG. Evidências WebP são referenciadas no PDF e continuam acessíveis pela aplicação. O download de fotos, laudos e assinaturas passa por rotas autenticadas que conferem o SHA-256.

Administradores podem persistir os dados da organização, gerenciar membros e criar convites de uso único válidos por sete dias. A API guarda apenas o hash do token; como não há provedor de e-mail configurado, a interface oferece o link para cópia e envio manual. A vistoria concluída com relatório pode receber aceite eletrônico com imagem desenhada, usuário, data, versão dos termos e trilha de auditoria. A aprovação exige perfil administrador, relatório e aceite. Esse registro não é uma assinatura digital certificada. Após a conclusão, os dados do checklist não podem ser alterados pelos endpoints de edição.

As rotas de escrita usam perfis: `Admin` gerencia imóveis, unidades, modelos, convites, organização, relatórios e aprovação; `Admin` e `Vistoriador` podem criar e editar vistorias em rascunho e registrar aceite; `Leitor` tem acesso de leitura. O primeiro usuário cadastrado recebe `Admin`.

## Saúde e filas

`/health` verifica PostgreSQL, Redis e RabbitMQ na API. O Worker expõe o mesmo caminho na porta interna `8081`; o Compose usa os dois endpoints para marcar os serviços como saudáveis antes de iniciar seus dependentes. Mensagens inválidas vão para `<fila>.failed`; falhas transitórias recebem até cinco novas tentativas, com intervalo de 30 segundos, pela fila `<fila>.retry`. A inspeção e o reprocessamento operacional das mensagens falhas ainda precisam de procedimento.

## Limites atuais

- Convites ainda precisam ser enviados manualmente; entrega de e-mail e recuperação de senha não estão configuradas.
- O aceite é um registro eletrônico auditável; não usa certificado digital ou provedor de assinatura.
- A comparação de vistorias usa o nome de ambiente e item; alterações nesses nomes podem aparecer como itens ausentes.
- O PDF não incorpora imagens WebP. A inspeção e o reprocessamento operacional das mensagens na fila de falha ainda precisam de procedimento.
- Recuperação de desastre, metas de disponibilidade e desempenho e implantação de produção não foram validadas. A implantação depende de um destino e credenciais de infraestrutura.

Esses itens precisam de regras de produto, implementação e verificação antes de considerar o SaaS completo.

## CI/CD

`.github/workflows/ci.yml` compila backend e frontend, executa testes unitários e de integração com Testcontainers e valida o Compose em pull requests e pushes na `main`. Em pushes aprovados na `main`, publica imagens `api`, `worker` e `frontend` no GHCR, com tags `latest` e SHA. Publicar imagens não implanta os serviços nem migra o banco de produção.

## Configuração S3

`.env.example` usa MinIO local. Para produção, configure um endpoint S3 HTTPS privado, região, bucket e credenciais exclusivas do backend. Não envie chaves S3 ao frontend. `VISTORA_S3_ALLOW_INSECURE_LOCAL` libera HTTP somente para os hosts locais `minio`, `localhost` e `127.0.0.1`.

## Estrutura

```text
frontend/                       Next.js, React, TypeScript, PWA
backend/Vistora.Domain/         Modelos e regras sem dependências externas
backend/Vistora.Application/    Casos de uso e contratos
backend/Vistora.Infrastructure/ Persistência, S3, mensageria e PDF
backend/Vistora.Api/            Interface HTTP
backend/Vistora.Worker/         Processamento assíncrono
backend/Vistora.Tests/          Testes unitários e integrados
infra/                          Scripts operacionais
docs/                           Documentação de produto
```
