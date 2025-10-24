using FleksProfitAPI.Data;
using FleksProfitAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Resolve relative DB path from config to an absolute path under the content root
var relConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/fcr.db";
var contentRoot = builder.Environment.ContentRootPath;
var dataSourcePrefix = "Data Source=";
var relPath = relConn.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase)
    ? relConn.Substring(dataSourcePrefix.Length)
    : relConn;

var dbPath = Path.GetFullPath(Path.Combine(contentRoot, relPath));
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

// Log and use the resolved absolute path
builder.Logging.AddConsole();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"{dataSourcePrefix}{dbPath}"));

// Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<FcrDataService>();
builder.Services.AddScoped<FcrRevenueService>();
builder.Services.AddHostedService<EnergiNetSyncBackgroundService>();


// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database exists / apply migrations before background service runs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.GetPendingMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
