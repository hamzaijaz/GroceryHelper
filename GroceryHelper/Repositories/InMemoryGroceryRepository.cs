using System.Collections.Concurrent;
using GroceryHelper.Models;

namespace GroceryHelper.Repositories;

/// <summary>
/// Thread-safe, non-persistent store. Register as a singleton so data lives for the lifetime of the app.
/// </summary>
public sealed class InMemoryGroceryRepository : IGroceryRepository
{
    private readonly ConcurrentDictionary<Guid, Grocery> _groceries = new();

    public Task<IReadOnlyList<Grocery>> GetAllAsync(GroceryCategory? category)
    {
        IReadOnlyList<Grocery> groceries = _groceries.Values
            .Where(g => category is null || g.Category == category)
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(groceries);
    }

    public Task AddAsync(Grocery grocery)
    {
        if (!_groceries.TryAdd(grocery.Id, grocery))
        {
            throw new InvalidOperationException($"A grocery with id '{grocery.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Grocery grocery)
    {
        // TryUpdate only succeeds if the entry still holds the value we read, so a concurrent delete can't be undone.
        var updated = _groceries.TryGetValue(grocery.Id, out var existing)
            && _groceries.TryUpdate(grocery.Id, grocery, existing);

        return Task.FromResult(updated);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_groceries.TryRemove(id, out _));
}
