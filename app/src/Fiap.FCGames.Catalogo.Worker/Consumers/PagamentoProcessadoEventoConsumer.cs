using FCGames.IntegrationEvents;
using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregateBiblioteca;
using Fiap.FCGames.Catalogo.Domain.Enums;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Interface;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.FCGames.Catalogo.Worker.Consumers;

public class PagamentoProcessadoEventoConsumer : IConsumer<PagamentoProcessadoEvento>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PagamentoProcessadoEventoConsumer> _logger;

    public PagamentoProcessadoEventoConsumer(IUnitOfWork uow, ILogger<PagamentoProcessadoEventoConsumer> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PagamentoProcessadoEvento> context)
    {
        var evt = context.Message;

        var pedido = await _uow.PedidoRepository.ObterPorIdAsync(evt.PedidoId);
        if (pedido is null)
        {
            _logger.LogWarning("PedidoId {PedidoId} não encontrado — ignorando", evt.PedidoId);
            return;
        }

        if (pedido.Status != StatusPedido.Pendente)
        {
            _logger.LogInformation("PedidoId {PedidoId} já em status {Status} — idempotente", evt.PedidoId, pedido.Status);
            return;
        }

        if (evt.Status == "Aprovado")
        {
            pedido.Status = StatusPedido.Aprovado;
            _uow.PedidoRepository.Atualizar(pedido);

            var biblioteca = await _uow.BibliotecaRepository.ObterPorUsuarioIdAsync(evt.UsuarioId);
            if (biblioteca is not null)
            {
                _uow.BibliotecaRepository.AdicionarItem(new ItemBiblioteca
                {
                    Id = Guid.NewGuid(),
                    BibliotecaId = biblioteca.Id,
                    JogoId = evt.JogoId,
                    DataAdicao = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Biblioteca não encontrada para UsuarioId {UsuarioId} — jogo não adicionado", evt.UsuarioId);
            }

            await _uow.CommitAsync(context.CancellationToken);
            _logger.LogInformation("Pedido {PedidoId} aprovado. Jogo {NomeJogo} adicionado à biblioteca", evt.PedidoId, evt.NomeJogo);
        }
        else
        {
            pedido.Status = StatusPedido.Rejeitado;
            _uow.PedidoRepository.Atualizar(pedido);
            await _uow.CommitAsync(context.CancellationToken);
            _logger.LogInformation("Pedido {PedidoId} rejeitado. Motivo: {Motivo}", evt.PedidoId, evt.Motivo ?? "não informado");
        }
    }
}
