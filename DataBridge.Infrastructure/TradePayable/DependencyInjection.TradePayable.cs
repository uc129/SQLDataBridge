using DataBridge.Application.Interfaces;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;
using DataBridge.Infrastructure.TradePayable.Repositories;
using DataBridge.Infrastructure.TradePayable.SPO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DataBridge.Infrastructure.TradePayable;

internal static class DependencyInjectionTradePayable
{
    internal static IServiceCollection AddTradePayableInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<TradePayableDbContext>();

        // Register the resolved settings value so Application-layer classes (no IOptions reference)
        // can inject TradePayableSettings directly.
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradePayableSettings>>().Value);

        services.AddScoped<IPipelineRunRepository,       PipelineRunRepository>();
        services.AddScoped<IFAGLL03StagingRepository,    FAGLL03StagingRepository>();
        services.AddScoped<IStepResultRepository,        StepResultRepository>();
        services.AddScoped<IBackupTablesRepository,      BackupTablesRepository>();
        services.AddScoped<ICrossServerPORepository,     CrossServerPORepository>();
        services.AddScoped<ICrossServerVendorRepository, CrossServerVendorRepository>();
        services.AddScoped<IMergedDataService,           MergedDataService>();
        services.AddScoped<IGLHyperionMapRepository,     GLHyperionMapRepository>();

        services.AddScoped<ISPStorageService, SPStorageService>();

        // Singleton so it survives across the upload request and the subsequent run-pipeline request.
        services.AddSingleton<IPipelineMemoryStore, PipelineMemoryStore>();

        RegisterMasterTable<AdvanceGLs>       (services, "AdvanceGLs");
        RegisterMasterTable<LiabilityGLs>     (services, "LiabilityGLs");
        RegisterMasterTable<NotDueGLs>        (services, "NotDueGLs");
        RegisterMasterTable<MSMECompanyCodes> (services, "MSMECompanyCodes");
        RegisterMasterTable<CapitalCreditorGLs>(services, "CapitalCreditorGLs");
        RegisterMasterTable<InsuranceGLs>     (services, "InsuranceGLs");
        RegisterMasterTable<NonMSMEGLs>       (services, "NonMSMEGLs");
        RegisterMasterTable<UnclaimedGLs>     (services, "UnclaimedGLs");
        RegisterMasterTable<GLHyperionMap>    (services, "GLHyperionMap");
        RegisterMasterTable<ICPHyperionMap>   (services, "ICPHyperionMap");
        RegisterMasterTable<ICPVendorMap>     (services, "ICPVendorMap");
        RegisterMasterTable<ForexMonthEndMap> (services, "ForexMonthEndMap");
        RegisterMasterTable<AgeingGroup>      (services, "AgeingGroup");

        return services;
    }

    private static void RegisterMasterTable<T>(IServiceCollection services, string tableKey)
        where T : class
    {
        services.AddScoped<IMasterTableRepository<T>>(sp =>
            new MasterTableRepository<T>(
                sp.GetRequiredService<TradePayableDbContext>(),
                sp.GetRequiredService<IOptions<TradePayableSettings>>(),
                tableKey));
    }
}
