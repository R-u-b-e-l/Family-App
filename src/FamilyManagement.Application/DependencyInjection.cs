using FamilyManagement.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
