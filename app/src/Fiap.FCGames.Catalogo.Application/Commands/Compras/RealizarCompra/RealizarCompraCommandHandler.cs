using FCGames.IntegrationEvents;
using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregatePedido;
using Fiap.FCGames.Catalogo.Domain.Enums;
using Fiap.FCGames.Catalogo.Domain.Exception;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Interface;
using MassTransit;
using MediatR;

namespace Fiap.FCGames.Catalogo.Application.Commands.Compras.RealizarCompra;

public class RealizarCompraCommandHandler : IRequestHandler<RealizarCompraCommand, RealizarCompraResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPublishEndpoint _publisher;

    public RealizarCompraCommandHandler(IUnitOfWork uow, IPublishEndpoint publisher)
    {
        _uow = uow;
        _publisher = publisher;
    }

    public async Task<RealizarCompraResponse> Handle(RealizarCompraCommand request, CancellationToken cancellationToken)
    {
        var jogo = await _uow.JogoRepository.ObterPorIdAsync(request.JogoId)
            ?? throw new NotFoundException($"Jogo '{request.JogoId}' não encontrado.");

        var japossui = await _uow.BibliotecaRepository.UsuarioPossuiJogoAsync(request.UsuarioId, request.JogoId);
        if (japossui)
            throw new ConflictException($"Usuário já possui o jogo '{jogo.Nome}'.");

        var correlationId = Guid.NewGuid();
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            JogoId = jogo.Id,
            Preco = jogo.Preco,
            Status = StatusPedido.Pendente,
            CriadoEm = DateTime.UtcNow
        };

        _uow.PedidoRepository.Adicionar(pedido);

        await _publisher.Publish(new PedidoRealizadoEvento(
            PedidoId: pedido.Id,
            UsuarioId: pedido.UsuarioId,
            JogoId: jogo.Id,
            NomeJogo: jogo.Nome,
            Preco: jogo.Preco,
            RealizadoEmUtc: pedido.CriadoEm,
            CorrelationId: correlationId
        ), cancellationToken);

        await _uow.CommitAsync(cancellationToken);

        return new RealizarCompraResponse(pedido.Id, jogo.Id, jogo.Nome, jogo.Preco, pedido.Status.ToString());
    }
}
