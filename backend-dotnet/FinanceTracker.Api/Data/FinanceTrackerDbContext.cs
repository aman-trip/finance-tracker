using FinanceTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Data;

public sealed class FinanceTrackerDbContext(DbContextOptions<FinanceTrackerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<AccountMembership> AccountMemberships => Set<AccountMembership>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAutomaticValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAutomaticValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureAccounts(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureTransactions(modelBuilder);
        ConfigureBudgets(modelBuilder);
        ConfigureGoals(modelBuilder);
        ConfigureRecurringTransactions(modelBuilder);
        ConfigurePasswordResetTokens(modelBuilder);
        ConfigureRules(modelBuilder);
        ConfigureAccountMemberships(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            entity.Property(x => x.OpeningBalance).HasColumnName("opening_balance").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.CurrentBalance).HasColumnName("current_balance").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.InstitutionName).HasColumnName("institution_name").HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            entity.Property(x => x.Color).HasColumnName("color").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Archived).HasColumnName("is_archived").IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.TransactionDate).HasColumnName("transaction_date").IsRequired();
            entity.Property(x => x.Merchant).HasColumnName("merchant").HasMaxLength(255);
            entity.Property(x => x.Note).HasColumnName("note").HasColumnType("text");
            entity.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100);
            entity.Property(x => x.TransferGroupId).HasColumnName("transfer_group_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureBudgets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
            entity.Property(x => x.Month).HasColumnName("month").IsRequired();
            entity.Property(x => x.Year).HasColumnName("year").IsRequired();
            entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.AlertThresholdPercent).HasColumnName("alert_threshold_percent").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.Month, x.Year }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureGoals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("goals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.TargetAmount).HasColumnName("target_amount").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.CurrentAmount).HasColumnName("current_amount").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.TargetDate).HasColumnName("target_date");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRecurringTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.ToTable("recurring_transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(19, 2).IsRequired();
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(x => x.Frequency).HasColumnName("frequency").HasConversion<string>().IsRequired();
            entity.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(x => x.EndDate).HasColumnName("end_date");
            entity.Property(x => x.NextRunDate).HasColumnName("next_run_date").IsRequired();
            entity.Property(x => x.AutoCreateTransaction).HasColumnName("auto_create_transaction").IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePasswordResetTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.ExpiryTime).HasColumnName("expiry_time").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.ToTable("rules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.ConditionJson).HasColumnName("condition_json").HasColumnType("text").IsRequired();
            entity.Property(x => x.ActionJson).HasColumnName("action_json").HasColumnType("text").IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAccountMemberships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountMembership>(entity =>
        {
            entity.ToTable("account_members");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasIndex(x => new { x.AccountId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ApplyAutomaticValues()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<User>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }

            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Account>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }

            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Category>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<Budget>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<Goal>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<RecurringTransaction>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<Transaction>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<PasswordResetToken>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Rule>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AccountMembership>().Where(x => x.State == EntityState.Added))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }

            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }
    }
}
