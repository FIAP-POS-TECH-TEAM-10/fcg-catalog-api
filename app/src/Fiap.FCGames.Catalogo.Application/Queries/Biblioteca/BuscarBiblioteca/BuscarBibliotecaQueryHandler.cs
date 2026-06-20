using Fiap.FCGames.Catalogo.Infra.DataProvider.Interface;
using MediatR;

namespace Fiap.FCGames.Catalogo.Application.Queries.Biblioteca.BuscarBiblioteca;

public class BuscarBibliotecaQueryHandler : IRequestHandler<BuscarBibliotecaQuery, BuscarBibliotecaResponse>
{
    private readonly IUnitOfWork _uow;

    public BuscarBibliotecaQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<BuscarBibliotecaResponse> Handle(BuscarBibliotecaQuery request, CancellationToken cancellationToken)
    {
        var biblioteca = await _uow.BibliotecaRepository.ObterComItensPorUsuarioIdAsync(request.UsuarioId);

        if (biblioteca is null)
            return new BuscarBibliotecaResponse(request.UsuarioId, new List<ItemBibliotecaResponse>());

        var jogoIds = biblioteca.Itens.Select(i => i.JogoId).ToList();
        var todosJogos = await _uow.JogoRepository.ListarTodosAsync();
        var jogosMap = todosJogos
            .Where(j => jogoIds.Contains(j.Id))
            .ToDictionary(j => j.Id);

        var itens = biblioteca.Itens.Select(i =>
        {
            jogosMap.TryGetValue(i.JogoId, out var jogo);
            return new ItemBibliotecaResponse(
                i.JogoId,
                jogo?.Nome ?? "Desconhecido",
                jogo?.Preco ?? 0,
                i.DataAdicao
            );
        }).ToList();

        return new BuscarBibliotecaResponse(request.UsuarioId, itens);
    }
}
