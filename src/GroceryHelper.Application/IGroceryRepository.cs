using GroceryHelper.Domain;

namespace GroceryHelper.Application;

public interface IGroceryRepository
{
    /// <param name="category">Optional category filter; <c>null</c> returns all groceries.</param>
    Task<IReadOnlyList<Grocery>> GetAllAsync(GroceryCategory? category);

    /// <exception cref="InvalidOperationException">A grocery with the same id already exists.</exception>
    Task AddAsync(Grocery grocery);

    /// <returns><c>true</c> if the grocery existed and was replaced; otherwise <c>false</c>.</returns>
    Task<bool> UpdateAsync(Grocery grocery);

    /// <returns><c>true</c> if the grocery existed and was removed; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid id);
}
