using FinanceTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class SchemaBootstrapService(
    FinanceTrackerDbContext dbContext,
    ILogger<SchemaBootstrapService> logger)
{
    private const string CreateRulesTableSql = """
        CREATE TABLE IF NOT EXISTS rules (
            id UUID PRIMARY KEY,
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            name VARCHAR(255) NOT NULL,
            condition_json TEXT NOT NULL,
            action_json TEXT NOT NULL,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );
        """;

    private const string CreateAccountMembersTableSql = """
        CREATE TABLE IF NOT EXISTS account_members (
            id UUID PRIMARY KEY,
            account_id UUID NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            role VARCHAR(50) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL
        );
        """;

    public async Task EnsureVersionTwoSchemaAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ensuring Version 2 schema objects exist");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await ExecuteStaticSqlAsync(CreateRulesTableSql, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS name VARCHAR(255) NOT NULL DEFAULT 'Migrated rule';
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS condition_json TEXT NOT NULL DEFAULT json_build_object()::text;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS action_json TEXT NOT NULL DEFAULT json_build_object()::text;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            DROP COLUMN IF EXISTS action_type;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            DROP COLUMN IF EXISTS action_value;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            DROP COLUMN IF EXISTS condition_value;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE rules
            ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
            """, cancellationToken);

        await ExecuteStaticSqlAsync(CreateAccountMembersTableSql, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE account_members
            ADD COLUMN IF NOT EXISTS role VARCHAR(50) NOT NULL DEFAULT 'VIEWER';
            """, cancellationToken);
        await ExecuteStaticSqlAsync("""
            ALTER TABLE account_members
            ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
            """, cancellationToken);

        await ExecuteStaticSqlAsync("CREATE INDEX IF NOT EXISTS idx_rules_user_id ON rules(user_id);", cancellationToken);
        await ExecuteStaticSqlAsync("CREATE INDEX IF NOT EXISTS idx_rules_user_id_active ON rules(user_id, is_active);", cancellationToken);
        await ExecuteStaticSqlAsync("CREATE UNIQUE INDEX IF NOT EXISTS ux_account_members_account_user ON account_members(account_id, user_id);", cancellationToken);
        await ExecuteStaticSqlAsync("CREATE INDEX IF NOT EXISTS idx_account_members_account_id ON account_members(account_id);", cancellationToken);
        await ExecuteStaticSqlAsync("CREATE INDEX IF NOT EXISTS idx_account_members_user_id ON account_members(user_id);", cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ExecuteStaticSqlAsync(string sql, CancellationToken cancellationToken)
    {
        Console.WriteLine(sql);
        logger.LogInformation("Executing schema SQL: {Sql}", sql);
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
