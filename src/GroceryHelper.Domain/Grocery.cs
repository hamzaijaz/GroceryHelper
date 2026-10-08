namespace GroceryHelper.Domain;

public sealed record Grocery(Guid Id, string Name, int Quantity, GroceryCategory? Category = null);
