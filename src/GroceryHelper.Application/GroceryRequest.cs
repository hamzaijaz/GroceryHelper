using System.ComponentModel.DataAnnotations;
using GroceryHelper.Domain;

namespace GroceryHelper.Application;

/// <summary>
/// Payload used to create or update a grocery.
/// </summary>
public sealed record GroceryRequest(
    [Required, StringLength(GroceryRequest.MaxNameLength)] string Name,
    [Range(1, GroceryRequest.MaxQuantity)] int Quantity,
    GroceryCategory? Category = null)
{
    public const int MaxNameLength = 100;
    public const int MaxQuantity = 1000;

    public Grocery ToGrocery(Guid id) => new(id, Name.Trim(), Quantity, Category);
}
