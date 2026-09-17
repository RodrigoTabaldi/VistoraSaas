# PostgreSQL operational artifacts

Place database operational files here when they are needed:

```text
postgresql/
  scripts/             # Manual bootstrap, maintenance, or diagnostic SQL
  seed/                # Development-only seed data
```

Application schema changes should normally be created as Entity Framework migrations in `backend/Vistora.Infrastructure/Persistence/PostgreSql/Migrations/`. Keep secrets in `.env`, never in SQL scripts.
