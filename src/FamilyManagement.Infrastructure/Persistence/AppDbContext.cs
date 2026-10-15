using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    private readonly ICurrentUser _currentUser = currentUser;

    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilySettings> FamilySettings => Set<FamilySettings>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherDetail> VoucherDetails => Set<VoucherDetail>();
    public DbSet<VoucherSequence> VoucherSequences => Set<VoucherSequence>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- Identity tables -------------------------------------------------
        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Address).HasMaxLength(500);
            e.Property(u => u.ProfilePicturePath).HasMaxLength(500);
            e.HasIndex(u => u.FamilyId);
            e.HasIndex(u => u.PhoneNumber);
            e.HasIndex(u => u.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            e.HasOne(u => u.Family).WithMany(f => f.Users).HasForeignKey(u => u.FamilyId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AppRole>(e =>
        {
            e.Property(r => r.Description).HasMaxLength(250);
            // Roles are per family, so the Identity default global-unique name index must relax.
            e.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
            e.HasIndex(r => new { r.FamilyId, r.NormalizedName }).IsUnique().HasFilter("[FamilyId] IS NOT NULL");
        });

        // ---- Core entities ---------------------------------------------------
        builder.Entity<Family>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(100);
            e.Property(f => f.LogoPath).HasMaxLength(500);
        });

        builder.Entity<FamilySettings>(e =>
        {
            e.HasIndex(s => s.FamilyId).IsUnique();
            e.Property(s => s.Currency).HasMaxLength(3);
            e.Property(s => s.DateFormat).HasMaxLength(30);
            e.Property(s => s.Timezone).HasMaxLength(60);
            e.Property(s => s.Address).HasMaxLength(500);
            e.Property(s => s.ContactPhone).HasMaxLength(20);
            e.Property(s => s.ContactEmail).HasMaxLength(256);
            e.HasOne(s => s.Family).WithOne(f => f.Settings).HasForeignKey<FamilySettings>(s => s.FamilyId);
        });

        builder.Entity<Permission>(e =>
        {
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Code).HasMaxLength(100);
            e.Property(p => p.Group).HasMaxLength(50);
            e.Property(p => p.Description).HasMaxLength(250);
            e.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            e.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(88);
            e.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);
            e.Property(t => t.CreatedByIp).HasMaxLength(45);
            e.Property(t => t.RevokedByIp).HasMaxLength(45);
            e.HasIndex(t => t.TokenHash);
            e.HasIndex(t => new { t.UserId, t.ExpiresAt });
            e.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId);
        });

        builder.Entity<OtpCode>(e =>
        {
            e.Property(o => o.CodeHash).HasMaxLength(88);
            e.HasIndex(o => new { o.UserId, o.Purpose, o.ExpiresAt });
            e.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId);
        });

        builder.Entity<Account>(e =>
        {
            e.Property(a => a.Code).HasMaxLength(20);
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.Description).HasMaxLength(500);
            e.HasIndex(a => new { a.FamilyId, a.Code }).IsUnique();
            e.HasIndex(a => new { a.FamilyId, a.ParentAccountId });
            e.HasIndex(a => new { a.FamilyId, a.LinkedMemberId });
            e.HasOne(a => a.Parent).WithMany(a => a.Children).HasForeignKey(a => a.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AccountingPeriod>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(50);
            e.HasIndex(p => new { p.FamilyId, p.StartDate }).IsUnique();
        });

        builder.Entity<Voucher>(e =>
        {
            e.Property(v => v.VoucherNumber).HasMaxLength(30);
            e.Property(v => v.Narration).HasMaxLength(500);
            e.Property(v => v.RowVersion).IsRowVersion();
            e.HasIndex(v => new { v.FamilyId, v.VoucherNumber }).IsUnique();
            e.HasIndex(v => new { v.FamilyId, v.VoucherDate, v.Status });
            e.HasIndex(v => new { v.FamilyId, v.VoucherType, v.Status });
            e.HasIndex(v => new { v.FamilyId, v.MemberId });
            e.HasOne(v => v.Period).WithMany().HasForeignKey(v => v.PeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.ReversalOfVoucher).WithMany().HasForeignKey(v => v.ReversalOfVoucherId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(v => v.Details).WithOne(d => d.Voucher).HasForeignKey(d => d.VoucherId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VoucherDetail>(e =>
        {
            e.Property(d => d.Narration).HasMaxLength(500);
            e.HasIndex(d => new { d.AccountId, d.VoucherId });
            e.HasIndex(d => d.VoucherId);
            e.HasOne(d => d.Account).WithMany().HasForeignKey(d => d.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_VoucherDetail_NonNegative", "[Debit] >= 0 AND [Credit] >= 0");
                t.HasCheckConstraint("CK_VoucherDetail_OneSide", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)");
            });
        });

        builder.Entity<VoucherSequence>(e =>
        {
            e.HasIndex(s => new { s.FamilyId, s.VoucherType, s.FiscalYear }).IsUnique();
        });

        builder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.Action).HasMaxLength(100);
            e.Property(a => a.EntityName).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(50);
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.Property(a => a.DeviceInfo).HasMaxLength(500);
            e.HasIndex(a => new { a.FamilyId, a.CreatedDate });
            e.HasIndex(a => new { a.FamilyId, a.EntityName, a.EntityId });
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Title).HasMaxLength(200);
            e.Property(n => n.Body).HasMaxLength(1000);
            e.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedDate });
        });

        ApplyTenantFilters(builder);
        SeedData.Apply(builder);
    }

    /// <summary>Global query filter on every family-owned entity. The API never sees foreign rows.</summary>
    private void ApplyTenantFilters(ModelBuilder builder)
    {
        builder.Entity<FamilySettings>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<AppUser>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<Account>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<AccountingPeriod>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<Voucher>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<VoucherDetail>().HasQueryFilter(e => e.Voucher!.FamilyId == _currentUser.FamilyId);
        builder.Entity<VoucherSequence>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<AuditLog>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
        builder.Entity<Notification>().HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId);
    }

    // ---- IAppDbContext -------------------------------------------------------

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
            return await operation(cancellationToken);

        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }

    public async Task<int> GetNextVoucherSequenceAsync(Guid familyId, VoucherType voucherType, int fiscalYear, CancellationToken ct = default)
    {
        if (Database.IsSqlServer())
        {
            // Atomic claim under UPDLOCK so concurrent postings never share a number.
            var claimed = await Database.SqlQuery<int>($"""
                UPDATE [VoucherSequences] WITH (UPDLOCK, HOLDLOCK)
                SET [NextNumber] = [NextNumber] + 1
                OUTPUT [deleted].[NextNumber]
                WHERE [FamilyId] = {familyId} AND [VoucherType] = {(int)voucherType} AND [FiscalYear] = {fiscalYear}
                """).ToListAsync(ct);

            if (claimed.Count > 0)
                return claimed[0];

            VoucherSequences.Add(new VoucherSequence
            {
                FamilyId = familyId,
                VoucherType = voucherType,
                FiscalYear = fiscalYear,
                NextNumber = 2
            });
            await SaveChangesAsync(ct);
            return 1;
        }

        // Non-relational providers (unit tests): simple read-modify-write.
        var sequence = await VoucherSequences
            .FirstOrDefaultAsync(s => s.FamilyId == familyId && s.VoucherType == voucherType && s.FiscalYear == fiscalYear, ct);
        if (sequence is null)
        {
            sequence = new VoucherSequence { FamilyId = familyId, VoucherType = voucherType, FiscalYear = fiscalYear, NextNumber = 1 };
            VoucherSequences.Add(sequence);
        }
        var number = sequence.NextNumber;
        sequence.NextNumber++;
        await SaveChangesAsync(ct);
        return number;
    }

    // ---- Tenant write guard ----------------------------------------------------

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GuardTenantWrites();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        GuardTenantWrites();
        return base.SaveChanges();
    }

    private void GuardTenantWrites()
    {
        if (!_currentUser.IsAuthenticated)
            return; // Registration & login flows set FamilyId explicitly and carry no tenant context.

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity.FamilyId == Guid.Empty:
                    entry.Entity.FamilyId = _currentUser.FamilyId;
                    break;
                case EntityState.Added when entry.Entity.FamilyId != _currentUser.FamilyId:
                case EntityState.Modified when entry.Entity.FamilyId != _currentUser.FamilyId:
                case EntityState.Deleted when entry.Entity.FamilyId != _currentUser.FamilyId:
                    throw new InvalidOperationException(
                        $"Cross-tenant write blocked for {entry.Entity.GetType().Name}.");
            }
        }
    }
}
