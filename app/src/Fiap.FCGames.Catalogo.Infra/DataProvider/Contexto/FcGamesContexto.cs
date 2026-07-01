using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregateBiblioteca;
using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregateJogo;
using Fiap.FCGames.Catalogo.Domain.Aggregates.AggregatePedido;
using Fiap.FCGames.Catalogo.Infra.DataProvider.EntityConfigurations;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Fiap.FCGames.Catalogo.Infra.DataProvider.Contexto;

public class FcGamesContexto : DbContext
{
    public FcGamesContexto(DbContextOptions<FcGamesContexto> options) : base(options) { }

    public DbSet<Jogo> Jogos { get; set; }
    public DbSet<Biblioteca> Bibliotecas { get; set; }
    public DbSet<ItemBiblioteca> ItensBiblioteca { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JogoConfiguration());
        modelBuilder.ApplyConfiguration(new BibliotecaConfiguration());
        modelBuilder.ApplyConfiguration(new ItemBibliotecaConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoConfiguration());

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
