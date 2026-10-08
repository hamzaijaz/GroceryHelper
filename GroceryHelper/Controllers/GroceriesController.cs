using System.Net.Mime;
using GroceryHelper.Models;
using GroceryHelper.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GroceryHelper.Controllers;

[ApiController]
[Route("api/groceries")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class GroceriesController(IGroceryRepository repository) : ControllerBase
{
    /// <param name="category">Optional category to filter by.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Grocery>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<Grocery>>> GetAll([FromQuery] GroceryCategory? category)
    {
        var groceries = await repository.GetAllAsync(category);
        return Ok(groceries);
    }

    [HttpPost]
    [ProducesResponseType<Grocery>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Grocery>> Create(GroceryRequest request)
    {
        var grocery = request.ToGrocery(Guid.NewGuid());
        await repository.AddAsync(grocery);
        return StatusCode(StatusCodes.Status201Created, grocery);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<Grocery>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Grocery>> Update(Guid id, GroceryRequest request)
    {
        var grocery = request.ToGrocery(id);
        return await repository.UpdateAsync(grocery) ? Ok(grocery) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await repository.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
