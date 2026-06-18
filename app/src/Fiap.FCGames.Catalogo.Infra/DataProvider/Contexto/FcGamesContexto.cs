using Microsoft.EntityFrameworkCore;

namespace Fiap.FCGames.Catalogo.Infra.DataProvider.Contexto;

public class FcGamesContexto : DbContext
{
    public FcGamesContexto(DbContextOptions<FcGamesContexto> options) : base(options) { }

    // TODO: adicionar DbSets do domínio Catálogo:
    // public DbSet<Jogo> Jogos { get; set; }
    // public DbSet<Pedido> Pedidos { get; set; }
    // public DbSet<BibliotecaJogos> Bibliotecas { get; set; }
    // public DbSet<ItemBiblioteca> ItensBiblioteca { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: aplicar configurações das entidades do domínio Catálogo
    }
}
