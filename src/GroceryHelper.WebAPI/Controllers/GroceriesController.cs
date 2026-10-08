using System.Net.Mime;
using GroceryHelper.Application;
using GroceryHelper.Domain;
using Microsoft.AspNetCore.Mvc;

namespace GroceryHelper.WebAPI.Controllers;

[ApiController]
[Route("api/groceries")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class GroceriesController(GroceryService groceryService) : ControllerBase
{
    /// <param name="category">Optional category to filter by.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Grocery>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<Grocery>>> GetAll([FromQuery] GroceryCategory? category)
    {
        var groceries = await groceryService.GetAllAsync(category);
        return Ok(groceries);
    }

    [HttpPost]
    [ProducesResponseType<Grocery>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Grocery>> Create(GroceryRequest request)
    {
        var grocery = await groceryService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, grocery);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<Grocery>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Grocery>> Update(Guid id, GroceryRequest request)
    {
        var grocery = await groceryService.UpdateAsync(id, request);
        return grocery is null ? NotFound() : Ok(grocery);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await groceryService.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
