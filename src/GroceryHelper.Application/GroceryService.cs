using GroceryHelper.Domain;

namespace GroceryHelper.Application;

/// <summary>
/// Grocery use cases. Assigns ids and maps requests to domain objects; persistence is delegated to <see cref="IGroceryRepository"/>.
/// </summary>
public sealed class GroceryService(IGroceryRepository repository)
{
    /// <param name="category">Optional category filter; <c>null</c> returns all groceries.</param>
    public Task<IReadOnlyList<Grocery>> GetAllAsync(GroceryCategory? category) => repository.GetAllAsync(category);

    public async Task<Grocery> CreateAsync(GroceryRequest request)
    {
        var grocery = request.ToGrocery(Guid.NewGuid());
        await repository.AddAsync(grocery);
        return grocery;
    }

    /// <returns>The updated grocery, or <c>null</c> if no grocery with <paramref name="id"/> exists.</returns>
    public async Task<Grocery?> UpdateAsync(Guid id, GroceryRequest request)
    {
        var grocery = request.ToGrocery(id);
        return await repository.UpdateAsync(grocery) ? grocery : null;
    }

    /// <returns><c>true</c> if the grocery existed and was removed; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteAsync(Guid id) => repository.DeleteAsync(id);
}
