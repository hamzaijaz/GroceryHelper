using System.ComponentModel.DataAnnotations;

namespace GroceryHelper.Models;

/// <summary>
/// Payload used to create or update a grocery.
/// </summary>
public sealed record GroceryRequest(
    [Required, StringLength(GroceryRequest.MaxNameLength)] string Name,
    [Range(1, GroceryRequest.MaxQuantity)] int Quantity,
    [StringLength(GroceryRequest.MaxCategoryLength)] string? Category = null)
{
    public const int MaxNameLength = 100;
    public const int MaxQuantity = 1000;
    public const int MaxCategoryLength = 50;

    public Grocery ToGrocery(Guid id) =>
        new(id, Name.Trim(), Quantity, string.IsNullOrWhiteSpace(Category) ? null : Category.Trim());
}
