using Fiap.FCGames.Catalogo.CrossCutting.Extensions;
using Fiap.FCGames.Catalogo.CrossCutting.Middleware;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Contexto;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.RegisterDI();
builder.Services.AddMediatRConfiguration();

builder.Services.RegisterSwaggerGenerator();

builder.Services.AddAutenticacaoApi(builder.Configuration);

builder.Services.AddAutorizacaoApi();

builder.Services.AddContextDatabase(builder.Configuration);

builder.Services.AddHealthChecks();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Aplica automaticamente as migrations pendentes e cria o banco SQLite local na inicialização.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FcGamesContexto>();
    dbContext.Database.Migrate();
    await SeedData.SeedJogosAsync(dbContext);
}

app.UseCorrelationId();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.RegisterSwagger();
    app.MapOpenApi();
    app.RegisterScalar();
}
app.UseErrorHandlingMiddleware();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
