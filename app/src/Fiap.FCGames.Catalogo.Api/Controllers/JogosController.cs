using Fiap.FCGames.Catalogo.Api.Controllers.Shared;
using Fiap.FCGames.Catalogo.Application.Commands.Jogos.AtualizarJogo;
using Fiap.FCGames.Catalogo.Application.Commands.Jogos.CriarJogo;
using Fiap.FCGames.Catalogo.Application.Commands.Jogos.RemoverJogo;
using Fiap.FCGames.Catalogo.Application.Queries.Jogos.ListarJogos;
using Fiap.FCGames.Catalogo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Catalogo.Api.Controllers;

[ApiController]
[Route("jogos")]
public class JogosController : ApiControllerBase<JogosController>
{
    public JogosController(ISender sender, ILogger<JogosController> logger) : base(sender, logger) { }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ListarAsync(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListarJogosQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> CriarAsync([FromBody] CriarJogoCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Created($"/jogos/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> AtualizarAsync(Guid id, [FromBody] AtualizarJogoRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AtualizarJogoCommand(id, request.Nome, request.Descricao, request.Preco), cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> RemoverAsync(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoverJogoCommand(id), cancellationToken);
        return Ok();
    }
}

public record AtualizarJogoRequest(string Nome, string Descricao, decimal Preco);
