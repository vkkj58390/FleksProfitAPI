using FleksProfitAPI.Services;
using FleksProfitAPI.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// QuestDB DataSource
builder.Services.AddSingleton(sp =>
{
    var cs = builder.Configuration.GetConnectionString("QuestDb")
             ?? throw new InvalidOperationException("Missing connection string 'QuestDb'.");
    var dsBuilder = new NpgsqlDataSourceBuilder(cs);
    return dsBuilder.Build();
});

// QuestDB repository
builder.Services.AddScoped<QuestDbRepository>();

// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<FcrDataService>();
builder.Services.AddScoped<FcrRevenueService>();
builder.Services.AddScoped<StromPriceDataService>();
builder.Services.AddScoped<FcrProfitService>();
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
