using MediatR;

namespace Fiap.FCGames.Catalogo.Application.Queries.Biblioteca.BuscarBiblioteca;

public record BuscarBibliotecaQuery(Guid UsuarioId) : IRequest<BuscarBibliotecaResponse>;
