using GroceryHelper.Models;

namespace GroceryHelper.Repositories;

public interface IGroceryRepository
{
    Task<IReadOnlyList<Grocery>> GetAllAsync();

    /// <exception cref="InvalidOperationException">A grocery with the same id already exists.</exception>
    Task AddAsync(Grocery grocery);

    /// <returns><c>true</c> if the grocery existed and was replaced; otherwise <c>false</c>.</returns>
    Task<bool> UpdateAsync(Grocery grocery);

    /// <returns><c>true</c> if the grocery existed and was removed; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid id);
}
