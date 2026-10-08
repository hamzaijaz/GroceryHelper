namespace GroceryHelper.Models;

public sealed record Grocery(Guid Id, string Name, int Quantity, GroceryCategory? Category = null);
