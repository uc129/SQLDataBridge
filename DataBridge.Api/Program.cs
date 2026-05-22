using DataBridge.Application.UseCases;
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

var app = builder.Build();

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
