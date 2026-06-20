using Fiap.FCGames.Catalogo.CrossCutting.Extensions;
using Fiap.FCGames.Catalogo.Worker.Consumers;
using MassTransit;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Services.AddSerilog();

builder.Services.RegisterDI();
builder.Services.AddContextDatabase(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UsuarioCriadoEventoConsumer>();
    x.AddConsumer<PagamentoProcessadoEventoConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ReceiveEndpoint("catalog-usuario-criado", e =>
        {
            e.ConfigureConsumer<UsuarioCriadoEventoConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("catalog-pagamento-processado", e =>
        {
            e.ConfigureConsumer<PagamentoProcessadoEventoConsumer>(ctx);
        });
    });
});

var host = builder.Build();
await host.RunAsync();
