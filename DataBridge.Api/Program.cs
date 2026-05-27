using DataBridge.Application.TradePayable.Processing;
using DataBridge.Application.TradePayable.Services;
using DataBridge.Application.TradePayable.Steps;
using DataBridge.Application.TradePayable.UseCases;
using DataBridge.Application.UseCases;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Infrastructure;
using DataBridge.Infrastructure.Auth;
using DataBridge.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Security.Claims;

const long MaxUploadBytes = 524_288_000; // 500 MB

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxUploadBytes);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = MaxUploadBytes);

// ── Authentication ────────────────────────────────────────────────────────────

const string AuthCookie = "DataBridgeAuth";

builder.Services.AddMicrosoftIdentityWebAppAuthentication(
    builder.Configuration,
    configSectionName: "AzureAd",
    cookieScheme: AuthCookie);

builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = AuthCookie;
    options.DefaultChallengeScheme = AuthCookie;
});

builder.Services.Configure<CookieAuthenticationOptions>(AuthCookie, options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    // Redirect browsers to login page; return status codes for API clients
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.Configure<OpenIdConnectOptions>("OpenIdConnect", options =>
{
    options.Events.OnTokenValidated = async ctx =>
    {
        var enricher = ctx.HttpContext.RequestServices.GetRequiredService<AzureAdUserEnricher>();
        await enricher.OnTokenValidatedAsync(ctx);
    };

    options.Events.OnRemoteFailure = ctx =>
    {
        ctx.Response.Redirect("/AccessDenied");
        ctx.HandleResponse();
        return Task.CompletedTask;
    };
});

// ── Authorization ─────────────────────────────────────────────────────────────

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("Admin", policy =>
        policy.RequireClaim(ClaimTypes.Role, "Admin"));
});

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

builder.Services.AddDataBridgeInfrastructure(builder.Configuration);

// ── Application use-cases ─────────────────────────────────────────────────────
builder.Services.AddScoped<ImportDataUseCase>();
builder.Services.AddScoped<ExportDataUseCase>();
builder.Services.AddScoped<GetAvailableExportTargetsUseCase>();
builder.Services.AddScoped<CleanDataUseCase>();
builder.Services.AddScoped<ImportAndCleanUseCase>();
builder.Services.AddScoped<GetDashboardUseCase>();
builder.Services.AddScoped<GetTableInfoUseCase>();
builder.Services.AddScoped<AddUserUseCase>();
builder.Services.AddScoped<ProxySignInUseCase>();
builder.Services.AddScoped<CancelJobUseCase>();

// ── Trade Payable ─────────────────────────────────────────────────────────────
builder.Services.Configure<TradePayableSettings>(
    builder.Configuration.GetSection("TradePayable"));

// Processing engine
builder.Services.AddScoped<HelperFunctions>();
builder.Services.AddScoped<StaticMasterTableService>();
//builder.Services.AddScoped<CrossServerMasterTablesService>();
builder.Services.AddScoped<DataProcessor>();
builder.Services.AddScoped<GITHelper>();
builder.Services.AddScoped<GITProcessor>();
builder.Services.AddScoped<GITDocCurrHelper>();
builder.Services.AddScoped<GITDocCurrProcessor>();

// Pipeline steps (ordered registration; DataProcessingService sorts by StepIndex)
builder.Services.AddScoped<IProcessStep, Step00_GetRawDataStep>();
builder.Services.AddScoped<IProcessStep, Step01_PopulateRawDataStep>();
builder.Services.AddScoped<IProcessStep, Step02_GetMergedDataStep>();
builder.Services.AddScoped<IProcessStep, Step03_ProcessGITLocalStep>();
builder.Services.AddScoped<IProcessStep, Step06_AppendTradeNetLiabilityStep>();
builder.Services.AddScoped<IProcessStep, Step07_ProcessGITDocCurrStep>();
builder.Services.AddScoped<IProcessStep, Step10_AppendTradeDocCurrNetLiabilityStep>();
builder.Services.AddScoped<IProcessStep, Step11_MergeTradeAndSNADataStep>();
builder.Services.AddScoped<IProcessStep, Step12_FixCPAgeingHyperionStep>();
builder.Services.AddScoped<IProcessStep, Step13_SaveProcessSummaryStep>();

builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();

// Use cases
builder.Services.AddScoped<UploadFAGLL03UseCase>();
builder.Services.AddScoped<RunPipelineStepUseCase>();
builder.Services.AddScoped<RunFullPipelineUseCase>();
builder.Services.AddScoped<DownloadStepReportUseCase>();
builder.Services.AddScoped<GetPipelineRunsUseCase>();
builder.Services.AddScoped<GetPipelineStateUseCase>();
builder.Services.AddScoped<MasterTableUseCase>();

var app = builder.Build();

// ── GLHyperionMapper startup initialisation ────────────────────────────────────
using (var initScope = app.Services.CreateScope())
{
    var glRepo = initScope.ServiceProvider.GetRequiredService<IGLHyperionMapRepository>();
    await GLHyperionMapper.InitializeAsync(glRepo);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<ProgressHub>("/progressHub").RequireAuthorization();

app.Run();
