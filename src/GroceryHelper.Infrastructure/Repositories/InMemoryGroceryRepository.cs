using System.Collections.Concurrent;
using GroceryHelper.Application;
using GroceryHelper.Domain;
using Microsoft.Extensions.Logging;

namespace GroceryHelper.Infrastructure.Repositories;

/// <summary>
/// Thread-safe, non-persistent store. Register as a singleton so data lives for the lifetime of the app.
/// </summary>
public sealed class InMemoryGroceryRepository(ILogger<InMemoryGroceryRepository> logger) : IGroceryRepository
{
    private readonly ConcurrentDictionary<Guid, Grocery> _groceries = new();

    public Task<IReadOnlyList<Grocery>> GetAllAsync(GroceryCategory? category)
    {
        IReadOnlyList<Grocery> groceries = _groceries.Values
            .Where(g => category is null || g.Category == category)
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.LogInformation(
            "Retrieved {GroceryCount} groceries (category filter: {Category})",
            groceries.Count,
            category?.ToString() ?? "none");

        return Task.FromResult(groceries);
    }

    public Task AddAsync(Grocery grocery)
    {
        if (!_groceries.TryAdd(grocery.Id, grocery))
        {
            throw new InvalidOperationException($"A grocery with id '{grocery.Id}' already exists.");
        }

        logger.LogInformation("Added grocery '{GroceryName}' (Id: {GroceryId})", grocery.Name, grocery.Id);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Grocery grocery)
    {
        // TryUpdate only succeeds if the entry still holds the value we read, so a concurrent delete can't be undone.
        if (!_groceries.TryGetValue(grocery.Id, out var existing) || !_groceries.TryUpdate(grocery.Id, grocery, existing))
        {
            logger.LogWarning(
                "Cannot update grocery '{GroceryName}' (Id: {GroceryId}): not found", grocery.Name, grocery.Id);
            return Task.FromResult(false);
        }

        logger.LogInformation(
            "Updated grocery '{PreviousGroceryName}' to '{GroceryName}' (Id: {GroceryId})",
            existing.Name,
            grocery.Name,
            grocery.Id);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        if (!_groceries.TryRemove(id, out var removed))
        {
            logger.LogWarning("Cannot delete grocery with Id {GroceryId}: not found", id);
            return Task.FromResult(false);
        }

        logger.LogInformation("Deleted grocery '{GroceryName}' (Id: {GroceryId})", removed.Name, removed.Id);
        return Task.FromResult(true);
    }
}
