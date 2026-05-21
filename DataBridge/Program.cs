using DataBridge.Hubs;
using DataBridge.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Security.Claims;

const long MaxUploadBytes = 524_288_000; // 500 MB — matches web.config IIS limit

var builder = WebApplication.CreateBuilder(args);

// Raise Kestrel's default 30 MB body limit to match the IIS web.config cap
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxUploadBytes);
// Raise the multipart form limit (separate from the Kestrel body limit)
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = MaxUploadBytes);

// ── Authentication ────────────────────────────────────────────────────────────

// Use "DataBridgeAuth" as the single cookie for both SSO and Proxy Login.
// AddMicrosoftIdentityWebAppAuthentication sets OIDC's SignInScheme to this
// cookie, so after Azure AD auth the identity lands in the same cookie that
// ProxyLogin writes to directly.
const string AuthCookie = "DataBridgeAuth";

builder.Services.AddMicrosoftIdentityWebAppAuthentication(
    builder.Configuration,
    configSectionName: "AzureAd",
    cookieScheme: AuthCookie);

// Make the DataBridgeAuth cookie the default for both authenticating requests
// and challenging unauthenticated ones. Challenges redirect to LoginPath ("/Login")
// without touching OIDC — OIDC is only triggered when the user explicitly
// clicks "Sign in with Microsoft" which hits /MicrosoftIdentity/Account/SignIn.
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
});

// Append to the events registered internally by Microsoft.Identity.Web
builder.Services.Configure<OpenIdConnectOptions>("OpenIdConnect", options =>
{
    options.Events.OnTokenValidated = async ctx =>
    {
        var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();

        var upn = ctx.Principal?.FindFirstValue("preferred_username")
               ?? ctx.Principal?.FindFirstValue(ClaimTypes.Email)
               ?? string.Empty;

        if (string.IsNullOrWhiteSpace(upn))
        {
            ctx.Fail("No UPN/email claim found in Azure AD token.");
            return;
        }

        var user = await userService.FindActiveUserAsync(upn.ToLowerInvariant());

        if (user is null)
        {
            ctx.Fail($"'{upn}' is not registered or inactive in DataBridge.");
            return;
        }

        var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
        identity.AddClaim(new Claim(ClaimTypes.Role, user.RoleCode));
        identity.AddClaim(new Claim("DataBridgeDisplayName", user.DisplayName));
        identity.AddClaim(new Claim("DataBridgeRole", user.RoleCode));
        identity.AddClaim(new Claim("DataBridgePSNO", user.PSNO));
        identity.AddClaim(new Claim("DataBridgeAuthMethod", "SSO"));
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

// ── App services ──────────────────────────────────────────────────────────────

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SqlExportService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<CleanService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
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
