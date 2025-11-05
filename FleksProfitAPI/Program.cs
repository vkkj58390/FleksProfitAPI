using FleksProfitAPI.Data;
using FleksProfitAPI.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Npgsql DataSource (PostgreSQL wire to QuestDB)
builder.Services.AddSingleton(sp =>
{
    var cs = builder.Configuration.GetConnectionString("QuestDb");
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
