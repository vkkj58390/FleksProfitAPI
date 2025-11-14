using FleksProfitAPI.Data;
using FleksProfitAPI.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Log environment + connection string
Console.WriteLine($"ASPNETCORE_ENVIRONMENT={builder.Environment.EnvironmentName}");

// Listen on 8080 for Docker
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Npgsql DataSource (PostgreSQL wire to QuestDB)
builder.Services.AddSingleton(sp =>
{
    var cs = builder.Configuration.GetConnectionString("QuestDb");
    Console.WriteLine($"Using QuestDB connection string: {cs}");
    var dsBuilder = new NpgsqlDataSourceBuilder(cs);
    return dsBuilder.Build();
});

// QuestDB repository
builder.Services.AddScoped<QuestDbRepository>();

// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<FcrDataService>();
builder.Services.AddScoped<FcrRevenueService>();
builder.Services.AddHostedService<EnergiNetSyncBackgroundService>();

// Controllers + Swagger + CORS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

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
