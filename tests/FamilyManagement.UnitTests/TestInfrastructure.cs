using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Infrastructure.Accounting;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.UnitTests;

public class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public string? UserName { get; set; } = "tester";
    public string? IpAddress => "127.0.0.1";
    public string? DeviceInfo => "unit-test";
}

public class FakeClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc);
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}

/// <summary>Pass-through cache: factories always run, Remove is a no-op.</summary>
public class FakeCache : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
    public Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default) => factory(ct);
    public void Remove(params string[] keys) { }
}

/// <summary>Builds an in-memory AppDbContext seeded with a family, its CoA and an open 2026 period.</summary>
public class TestContext : IDisposable
{
    public FakeCurrentUser CurrentUser { get; } = new();
    public FakeClock Clock { get; } = new();
    public AppDbContext Db { get; }
    public Guid PeriodId { get; }

    public TestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"fm-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;
        Db = new AppDbContext(options, CurrentUser);
        Db.Database.EnsureCreated();

        var familyId = CurrentUser.FamilyId;
        Db.Families.Add(new Family { Id = familyId, Name = "Test Family", CreatedDate = Clock.UtcNow });
        Db.FamilySettings.Add(new FamilySettings { FamilyId = familyId, CreatedDate = Clock.UtcNow });
        Db.Accounts.AddRange(DefaultChartOfAccounts.Build(familyId, Clock.UtcNow));

        var period = new AccountingPeriod
        {
            FamilyId = familyId,
            Name = "FY 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = PeriodStatus.Open,
            CreatedDate = Clock.UtcNow
        };
        Db.AccountingPeriods.Add(period);
        PeriodId = period.Id;
        Db.SaveChanges();
    }

    public Guid AccountId(string code) => Db.Accounts.IgnoreQueryFilters()
        .Single(a => a.FamilyId == CurrentUser.FamilyId && a.Code == code).Id;

    public void Dispose() => Db.Dispose();
}
