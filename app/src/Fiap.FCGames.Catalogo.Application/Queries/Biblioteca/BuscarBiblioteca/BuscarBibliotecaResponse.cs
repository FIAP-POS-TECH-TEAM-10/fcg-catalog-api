namespace Fiap.FCGames.Catalogo.Application.Queries.Biblioteca.BuscarBiblioteca;

public record ItemBibliotecaResponse(Guid JogoId, string NomeJogo, decimal Preco, DateTime DataAdicao);

public record BuscarBibliotecaResponse(Guid UsuarioId, List<ItemBibliotecaResponse> Jogos);
