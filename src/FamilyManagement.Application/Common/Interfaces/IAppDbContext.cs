using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Family> Families { get; }
    DbSet<FamilySettings> FamilySettings { get; }
    DbSet<AppUser> Users { get; }
    DbSet<AppRole> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Account> Accounts { get; }
    DbSet<AccountingPeriod> AccountingPeriods { get; }
    DbSet<Voucher> Vouchers { get; }
    DbSet<VoucherDetail> VoucherDetails { get; }
    DbSet<VoucherSequence> VoucherSequences { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs the operation inside a database transaction (with the provider's execution strategy).</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims the next voucher number for (family, type, fiscal year).
    /// Must be called inside the posting transaction.
    /// </summary>
    Task<int> GetNextVoucherSequenceAsync(Guid familyId, VoucherType voucherType, int fiscalYear, CancellationToken ct = default);
}
