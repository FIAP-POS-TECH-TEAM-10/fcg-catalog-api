using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregateJogo;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Contexto;

namespace Fiap.FCGames.Catalogo.Infra.DataProvider.Seed;

public static class SeedData
{
    public static async Task SeedJogosAsync(FcGamesContexto context)
    {
        if (context.Jogos.Any()) return;

        var jogos = new List<Jogo>
        {
            new() { Id = Guid.NewGuid(), Nome = "Hades", Descricao = "Roguelike aclamado pela crítica", Preco = 49.90m, DataCadastro = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Nome = "Hollow Knight", Descricao = "Metroidvania desafiador e atmosférico", Preco = 29.90m, DataCadastro = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Nome = "Cyberpunk 2077", Descricao = "RPG de ação em mundo aberto futurista", Preco = 149.90m, DataCadastro = DateTime.UtcNow },
        };

        context.Jogos.AddRange(jogos);
        await context.SaveChangesAsync();
    }
}
