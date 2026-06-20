using Fiap.FCGames.Catalogo.Api.Controllers.Shared;
using Fiap.FCGames.Catalogo.Application.Queries.Biblioteca.BuscarBiblioteca;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Catalogo.Api.Controllers;

[ApiController]
[Route("biblioteca")]
public class BibliotecaController : ApiControllerBase<BibliotecaController>
{
    public BibliotecaController(ISender sender, ILogger<BibliotecaController> logger) : base(sender, logger) { }

    [HttpGet("{usuarioId:guid}")]
    [Authorize]
    public async Task<IActionResult> BuscarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new BuscarBibliotecaQuery(usuarioId), cancellationToken);
        return Ok(result);
    }
}
