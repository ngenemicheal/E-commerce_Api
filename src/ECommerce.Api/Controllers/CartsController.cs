using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.UseCases.Carts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ISender _mediator;

    public CartsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartResponse>> GetMyCart(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyCartQuery(), ct));
    }

    [HttpPost("me/items")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartResponse>> AddItem([FromBody] AddItemToCartCommand command, CancellationToken ct)
    {
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpPut("me/items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartResponse>> UpdateItemQuantity([FromRoute] Guid productId, [FromBody] UpdateCartItemQuantityCommand command, CancellationToken ct)
    {
        if (command.ProductId != productId)
        {
            ModelState.AddModelError(nameof(command.ProductId), "Route productId does not match body productId.");
            return ValidationProblem(ModelState);
        }

        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("me/items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartResponse>> RemoveItem([FromRoute] Guid productId, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new RemoveCartItemCommand(productId), ct));
    }

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartResponse>> Clear(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ClearCartCommand(), ct));
    }
}
