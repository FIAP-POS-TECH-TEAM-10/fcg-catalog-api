using FCGames.IntegrationEvents;
using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregateBiblioteca;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Interface;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.FCGames.Catalogo.Worker.Consumers;

public class UsuarioCriadoEventoConsumer : IConsumer<UsuarioCriadoEvento>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UsuarioCriadoEventoConsumer> _logger;

    public UsuarioCriadoEventoConsumer(IUnitOfWork uow, ILogger<UsuarioCriadoEventoConsumer> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UsuarioCriadoEvento> context)
    {
        var evt = context.Message;

        var existe = await _uow.BibliotecaRepository.ObterPorUsuarioIdAsync(evt.UsuarioId);
        if (existe is not null)
        {
            _logger.LogInformation("Biblioteca já existe para UsuarioId {UsuarioId} — idempotente", evt.UsuarioId);
            return;
        }

        _uow.BibliotecaRepository.Adicionar(new Biblioteca
        {
            Id = Guid.NewGuid(),
            UsuarioId = evt.UsuarioId,
            CriadaEm = DateTime.UtcNow
        });

        await _uow.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Biblioteca criada para UsuarioId {UsuarioId} CorrelationId {CorrelationId}",
            evt.UsuarioId, evt.CorrelationId);
    }
}
