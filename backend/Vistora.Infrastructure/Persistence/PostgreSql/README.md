# PostgreSQL persistence

This directory is reserved for the PostgreSQL implementation of the infrastructure layer.

Suggested ownership:

```text
PostgreSql/
  Context/             # DbContext and design-time factory
  Configurations/      # Entity Framework entity mappings
  Migrations/          # Generated Entity Framework migrations
  Repositories/        # Repository implementations
```

When implementation starts, add the PostgreSQL/Entity Framework packages to `Vistora.Infrastructure.csproj` and register the database implementation in `DependencyInjection.cs`. Do not place domain entities, application interfaces, credentials, or connection strings here.
