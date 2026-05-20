using DataBridge.Hubs;
using DataBridge.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Adds the services

builder.Services.AddSignalR();
builder.Services.AddScoped<SqlExportService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<CleanService>();
builder.Services.AddScoped<MetricsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ...
app.MapControllers(); // Maps the attribute routes

app.MapRazorPages();
app.MapHub<ProgressHub>("/progressHub");

app.Run();
