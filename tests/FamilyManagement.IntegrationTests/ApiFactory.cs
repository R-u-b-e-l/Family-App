using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FamilyManagement.IntegrationTests;

/// <summary>Boots the real API against a dedicated SQL Server test database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=.;Database=FamilyManagementDb_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;

public static class ClientExtensions
{
    public static async Task<AuthResponse> RegisterFamilyAsync(this HttpClient client, string family, string userName,
        string email, string mobile, string password = "Passw0rd123")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register-family", new RegisterFamilyRequest
        {
            FamilyName = family,
            AdminFullName = $"{userName} admin",
            Email = email,
            Mobile = mobile,
            UserName = userName,
            Password = password,
            ConfirmPassword = password
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return body!.Data!;
    }

    public static HttpClient WithToken(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task<Guid> GetAccountIdAsync(this HttpClient client, string code)
    {
        var accounts = await client.GetFromJsonAsync<ApiResponse<PagedResult<AccountDto>>>("/api/v1/accounts?pageSize=100");
        return accounts!.Data!.Items.Single(a => a.Code == code).Id;
    }

    public static async Task<VoucherDto> PostVoucherAsync(this HttpClient client, SaveVoucherRequest request)
    {
        var draftResponse = await client.PostAsJsonAsync("/api/v1/vouchers", request);
        draftResponse.EnsureSuccessStatusCode();
        var draft = (await draftResponse.Content.ReadFromJsonAsync<ApiResponse<VoucherDto>>())!.Data!;

        var postResponse = await client.PostAsync($"/api/v1/vouchers/{draft.Id}/post", null);
        postResponse.EnsureSuccessStatusCode();
        return (await postResponse.Content.ReadFromJsonAsync<ApiResponse<VoucherDto>>())!.Data!;
    }
}
