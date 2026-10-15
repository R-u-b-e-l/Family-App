using System.Net;
using System.Net.Http.Json;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.IntegrationTests;

[Collection("api")]
public class ApiIntegrationTests(ApiFactory factory) : IClassFixture<DatabaseReset>
{
    private readonly ApiFactory _factory = factory;

    private static int _suffix;
    private static string Unique() => $"{Interlocked.Increment(ref _suffix)}{Random.Shared.Next(1000, 9999)}";

    [Fact]
    public async Task FullAuthFlow_Register_Login_Refresh_Works()
    {
        var client = _factory.CreateClient();
        var id = Unique();
        var auth = await client.RegisterFamilyAsync($"Family {id}", $"admin{id}", $"admin{id}@test.com", $"0171{id}000");

        Assert.NotEmpty(auth.AccessToken);
        Assert.NotEmpty(auth.RefreshToken);
        Assert.Contains("FamilyAdmin", auth.User.Roles);
        Assert.NotEmpty(auth.Permissions);

        // Login with username, then email.
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Identifier = $"admin{id}", Password = "Passw0rd123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Identifier = $"admin{id}", Password = "WrongPass1" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        // Refresh rotates the token: old one stops working, new one works.
        var refresh1 = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh1.StatusCode);
        var newTokens = (await refresh1.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var replayOld = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replayOld.StatusCode);

        // Reuse detection revoked the whole session family, so the new token is dead too.
        var afterReuse = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = newTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_FamilyB_CannotSeeFamilyA()
    {
        var clientA = _factory.CreateClient();
        var idA = Unique();
        var authA = await clientA.RegisterFamilyAsync($"Family A{idA}", $"userA{idA}", $"a{idA}@test.com", $"0172{idA}000");
        clientA.WithToken(authA.AccessToken);

        var cash = await clientA.GetAccountIdAsync("1111");
        var salary = await clientA.GetAccountIdAsync("4100");
        var voucher = await clientA.PostVoucherAsync(new SaveVoucherRequest
        {
            VoucherType = VoucherType.RV,
            VoucherDate = DateOnly.FromDateTime(DateTime.Today),
            Narration = "Family A money",
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = cash, Debit = 9999, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = salary, Debit = 0, Credit = 9999 }
            ]
        });

        var clientB = _factory.CreateClient();
        var idB = Unique();
        var authB = await clientB.RegisterFamilyAsync($"Family B{idB}", $"userB{idB}", $"b{idB}@test.com", $"0173{idB}000");
        clientB.WithToken(authB.AccessToken);

        // Family B must get 404 (not 403) for Family A's voucher and account.
        var voucherProbe = await clientB.GetAsync($"/api/v1/vouchers/{voucher.Id}");
        Assert.Equal(HttpStatusCode.NotFound, voucherProbe.StatusCode);

        var accountProbe = await clientB.GetAsync($"/api/v1/accounts/{cash}");
        Assert.Equal(HttpStatusCode.NotFound, accountProbe.StatusCode);

        // Family B cannot post into Family A's accounts.
        var crossPost = await clientB.PostAsJsonAsync("/api/v1/vouchers", new SaveVoucherRequest
        {
            VoucherType = VoucherType.RV,
            VoucherDate = DateOnly.FromDateTime(DateTime.Today),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = cash, Debit = 1, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = salary, Debit = 0, Credit = 1 }
            ]
        });
        Assert.Equal(HttpStatusCode.BadRequest, crossPost.StatusCode);

        // Family B's books are untouched by Family A's transactions.
        var trialBalance = await clientB.GetFromJsonAsync<ApiResponse<TrialBalanceDto>>(
            $"/api/v1/reports/trial-balance?asOf={DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        Assert.Empty(trialBalance!.Data!.Rows);

        var members = await clientB.GetFromJsonAsync<ApiResponse<PagedResult<UserDto>>>("/api/v1/users");
        Assert.Single(members!.Data!.Items);
    }

    [Fact]
    public async Task VoucherPosting_EnforcesBalance_AndPermissions()
    {
        var client = _factory.CreateClient();
        var id = Unique();
        var auth = await client.RegisterFamilyAsync($"Family V{id}", $"userV{id}", $"v{id}@test.com", $"0174{id}000");
        client.WithToken(auth.AccessToken);

        var cash = await client.GetAccountIdAsync("1111");
        var salary = await client.GetAccountIdAsync("4100");

        var unbalanced = await client.PostAsJsonAsync("/api/v1/vouchers", new SaveVoucherRequest
        {
            VoucherType = VoucherType.RV,
            VoucherDate = DateOnly.FromDateTime(DateTime.Today),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = cash, Debit = 100, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = salary, Debit = 0, Credit = 90 }
            ]
        });
        Assert.Equal(HttpStatusCode.BadRequest, unbalanced.StatusCode);

        var posted = await client.PostVoucherAsync(new SaveVoucherRequest
        {
            VoucherType = VoucherType.RV,
            VoucherDate = DateOnly.FromDateTime(DateTime.Today),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = cash, Debit = 500, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = salary, Debit = 0, Credit = 500 }
            ]
        });
        Assert.Equal(VoucherStatus.Posted, posted.Status);
        Assert.Equal(posted.TotalDebit, posted.TotalCredit);

        // Unauthenticated calls are rejected.
        var anonymous = _factory.CreateClient();
        var denied = await anonymous.GetAsync("/api/v1/vouchers");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task MemberWithoutAdminRole_CannotManageMembers()
    {
        var client = _factory.CreateClient();
        var id = Unique();
        var admin = await client.RegisterFamilyAsync($"Family M{id}", $"adminM{id}", $"m{id}@test.com", $"0175{id}000");
        client.WithToken(admin.AccessToken);

        var createMember = await client.PostAsJsonAsync("/api/v1/users", new CreateMemberRequest
        {
            FullName = "Junior Member",
            UserName = $"junior{id}",
            Email = $"junior{id}@test.com",
            Password = "Passw0rd123"
        });
        Assert.Equal(HttpStatusCode.OK, createMember.StatusCode);

        var memberClient = _factory.CreateClient();
        var memberLogin = await memberClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Identifier = $"junior{id}", Password = "Passw0rd123" });
        var memberAuth = (await memberLogin.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        memberClient.WithToken(memberAuth.AccessToken);

        // FamilyMember template has read-only permissions.
        var attemptCreate = await memberClient.PostAsJsonAsync("/api/v1/users", new CreateMemberRequest
        {
            FullName = "Evil", UserName = $"evil{id}", Email = $"evil{id}@test.com", Password = "Passw0rd123"
        });
        Assert.Equal(HttpStatusCode.Forbidden, attemptCreate.StatusCode);

        var attemptVoucher = await memberClient.PostAsJsonAsync("/api/v1/vouchers", new SaveVoucherRequest
        {
            VoucherType = VoucherType.JV,
            VoucherDate = DateOnly.FromDateTime(DateTime.Today),
            Lines = []
        });
        Assert.Equal(HttpStatusCode.Forbidden, attemptVoucher.StatusCode);

        // The family dashboard is admin-only; members get 403 while the admin gets 200.
        var memberDashboard = await memberClient.GetAsync("/api/v1/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, memberDashboard.StatusCode);

        var adminDashboard = await client.GetAsync("/api/v1/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, adminDashboard.StatusCode);

        // But every member can read their own statement and loan summary.
        var ownStatement = await memberClient.GetAsync($"/api/v1/member-statement/{memberAuth.User.Id}");
        Assert.Equal(HttpStatusCode.OK, ownStatement.StatusCode);

        var ownLoans = await memberClient.GetAsync("/api/v1/loans");
        Assert.Equal(HttpStatusCode.OK, ownLoans.StatusCode);

        // And a member cannot read the admin's statement (needs Member.View on others).
        var adminStatement = await memberClient.GetAsync($"/api/v1/member-statement/{admin.User.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, adminStatement.StatusCode);
    }
}

/// <summary>Resets the test database once per test class run.</summary>
public class DatabaseReset
{
    private static readonly Lock Gate = new();
    private static bool _done;

    public DatabaseReset(ApiFactory factory)
    {
        lock (Gate)
        {
            if (_done)
                return;
            factory.ResetDatabase();
            _done = true;
        }
    }
}
