namespace GroceryHelper.Domain;

/// <summary>
/// Supported grocery categories. Serialized by name (e.g. "FrozenFood"), so new members can be added freely.
/// </summary>
public enum GroceryCategory
{
    Dairy,
    Fruit,
    Vegetables,
    Meat,
    FrozenFood,
    Snacks,
    Drinks,
    Baby,
    PersonalCare,
    Household,
    CleaningSupplies,
    PetCare,
    Oils,
    DryFruit,
}
