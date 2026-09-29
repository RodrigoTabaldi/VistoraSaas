# Persistência PostgreSQL

`VistoraDbContext` define os mapeamentos e filtros por organização. As migrations em `Migrations/` criam o esquema, as políticas RLS e a role de aplicação `vistora_app`. Os interceptors aplicam o identificador da organização à sessão e limpam o contexto antes de devolver a conexão ao pool.

A API usa uma conexão administrativa somente para aplicar migrations em `Development` (`ConnectionStrings:PostgresMigration`). API e Worker usam `ConnectionStrings:Postgres` para as operações normais. `DatabaseRoleGuard` impede a inicialização se essa conexão tiver `SUPERUSER` ou `BYPASSRLS`.

Em produção, aplique migrations antes de iniciar os serviços com uma conexão administrativa separada. Configure a role de execução com senha própria e sem privilégios que contornem RLS. Os testes em `Vistora.Tests/Integration` usam PostgreSQL real por meio de Testcontainers para verificar isolamento, concorrência e privilégios.
