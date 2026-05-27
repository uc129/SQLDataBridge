using DataBridge.Application.Interfaces;
using DataBridge.Infrastructure.Auth;
using DataBridge.Infrastructure.Excel;
using DataBridge.Infrastructure.Jobs;
using DataBridge.Infrastructure.Persistence;
using DataBridge.Infrastructure.Repositories;
using DataBridge.Infrastructure.SignalR;
using DataBridge.Infrastructure.TradePayable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataBridge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDataBridgeInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IJobRegistry, InMemoryJobRegistry>();

        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IUserRepository,    DapperUserRepository>();
        services.AddScoped<IMetricsRepository, DapperMetricsRepository>();
        services.AddScoped<IImportRepository,  DapperImportRepository>();
        services.AddScoped<IExportRepository,  DapperExportRepository>();
        services.AddScoped<ICleanRepository,   DapperCleanRepository>();
        services.AddScoped<IExcelParser,       ExcelDataReaderParser>();
        services.AddScoped<IExcelWriter,       ClosedXmlExcelWriter>();
        services.AddScoped<IProxyAuthService,  ProxyAuthService>();
        services.AddScoped<IProgressNotifier,  SignalRProgressNotifier>();
        services.AddScoped<AzureAdUserEnricher>();

        services.AddTradePayableInfrastructure(config);

        return services;
    }
}
