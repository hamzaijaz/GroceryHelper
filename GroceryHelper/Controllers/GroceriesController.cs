using System.Net.Mime;
using GroceryHelper.Models;
using GroceryHelper.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GroceryHelper.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class GroceriesController(IGroceryRepository repository, ILogger<GroceriesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Grocery>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Grocery>>> GetAll()
    {
        var groceries = await repository.GetAllAsync();
        return Ok(groceries);
    }

    [HttpPost]
    [ProducesResponseType<Grocery>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Grocery>> Create(GroceryRequest request)
    {
        var grocery = request.ToGrocery(Guid.NewGuid());
        await repository.AddAsync(grocery);

        logger.LogInformation("Created grocery {GroceryId}", grocery.Id);
        return StatusCode(StatusCodes.Status201Created, grocery);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<Grocery>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Grocery>> Update(Guid id, GroceryRequest request)
    {
        var grocery = request.ToGrocery(id);
        if (!await repository.UpdateAsync(grocery))
        {
            logger.LogWarning("Cannot update grocery {GroceryId}: not found", id);
            return NotFound();
        }

        logger.LogInformation("Updated grocery {GroceryId}", id);
        return Ok(grocery);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await repository.DeleteAsync(id))
        {
            logger.LogWarning("Cannot delete grocery {GroceryId}: not found", id);
            return NotFound();
        }

        logger.LogInformation("Deleted grocery {GroceryId}", id);
        return NoContent();
    }
}
