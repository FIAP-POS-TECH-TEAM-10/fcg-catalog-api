using Fiap.FCGames.Catalogo.Infra.DataProvider.Interface;
using Fiap.FCGames.Catalogo.Infra.DataProvider.Repositories;
using Fiap.FCGames.Catalogo.Infra.DataProvider.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Catalogo.CrossCutting.Extensions;

public static class RegisterDependencyInjectionExtensions
{
    public static void RegisterDI(this IServiceCollection services)
    {
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // TODO: registrar repositories do domínio Catálogo (Jogos, Pedidos, Biblioteca)
    }
}
