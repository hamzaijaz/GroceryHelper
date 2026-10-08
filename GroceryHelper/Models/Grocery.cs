namespace GroceryHelper.Models;

public sealed record Grocery(Guid Id, string Name, int Quantity, string? Category = null);
