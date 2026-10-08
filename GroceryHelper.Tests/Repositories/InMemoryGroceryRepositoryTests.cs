using GroceryHelper.Models;
using GroceryHelper.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace GroceryHelper.Tests.Repositories;

public class InMemoryGroceryRepositoryTests
{
    private readonly FakeLogger<InMemoryGroceryRepository> _logger = new();
    private readonly InMemoryGroceryRepository _repository;

    public InMemoryGroceryRepositoryTests()
    {
        _repository = new InMemoryGroceryRepository(_logger);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var groceries = await _repository.GetAllAsync(category: null);

        Assert.Empty(groceries);
    }

    [Fact]
    public async Task AddAsync_AddsGrocery()
    {
        var grocery = new Grocery(Guid.NewGuid(), "Milk", 2);

        await _repository.AddAsync(grocery);

        Assert.Equal(grocery, Assert.Single(await _repository.GetAllAsync(category: null)));
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_Throws()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new Grocery(id, "Milk", 2));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.AddAsync(new Grocery(id, "Bread", 1)));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsGroceriesOrderedByName()
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "milk", 1));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Apples", 6));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Bread", 1));

        var groceries = await _repository.GetAllAsync(category: null);

        Assert.Equal(["Apples", "Bread", "milk"], groceries.Select(g => g.Name));
    }

    [Fact]
    public async Task GetAllAsync_WithCategory_ReturnsOnlyMatchingGroceries()
    {
        var milk = new Grocery(Guid.NewGuid(), "Milk", 1, GroceryCategory.Dairy);
        var cheese = new Grocery(Guid.NewGuid(), "Cheese", 1, GroceryCategory.Dairy);
        await _repository.AddAsync(milk);
        await _repository.AddAsync(cheese);
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Apples", 6, GroceryCategory.Fruit));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Bread", 1));

        var groceries = await _repository.GetAllAsync(GroceryCategory.Dairy);

        Assert.Equal([cheese, milk], groceries);
    }

    [Fact]
    public async Task GetAllAsync_WithCategory_WhenNoneMatch_ReturnsEmptyList()
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Milk", 1, GroceryCategory.Dairy));

        var groceries = await _repository.GetAllAsync(GroceryCategory.Snacks);

        Assert.Empty(groceries);
    }

    [Fact]
    public async Task GetAllAsync_WithoutCategory_ReturnsAllGroceries()
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Milk", 1, GroceryCategory.Dairy));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Bread", 1));

        var groceries = await _repository.GetAllAsync(category: null);

        Assert.Equal(2, groceries.Count);
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryExists_ReplacesItAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new Grocery(id, "Milk", 2));
        var updated = new Grocery(id, "Oat Milk", 3);

        var result = await _repository.UpdateAsync(updated);

        Assert.True(result);
        Assert.Equal(updated, Assert.Single(await _repository.GetAllAsync(category: null)));
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryMissing_ReturnsFalseAndDoesNotAdd()
    {
        var result = await _repository.UpdateAsync(new Grocery(Guid.NewGuid(), "Milk", 2));

        Assert.False(result);
        Assert.Empty(await _repository.GetAllAsync(category: null));
    }

    [Fact]
    public async Task DeleteAsync_WhenGroceryExists_RemovesItAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new Grocery(id, "Milk", 2));

        var result = await _repository.DeleteAsync(id);

        Assert.True(result);
        Assert.Empty(await _repository.GetAllAsync(category: null));
    }

    [Fact]
    public async Task DeleteAsync_WhenGroceryMissing_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_LogsCountAndCategory()
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Milk", 1, GroceryCategory.Dairy));

        await _repository.GetAllAsync(GroceryCategory.Dairy);

        AssertLatestLog(LogLevel.Information, "1", nameof(GroceryCategory.Dairy));
    }

    [Fact]
    public async Task AddAsync_LogsNameAndId()
    {
        var grocery = new Grocery(Guid.NewGuid(), "Milk", 2);

        await _repository.AddAsync(grocery);

        AssertLatestLog(LogLevel.Information, "Milk", grocery.Id.ToString());
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryExists_LogsPreviousAndNewNameAndId()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new Grocery(id, "Milk", 2));

        await _repository.UpdateAsync(new Grocery(id, "Oat Milk", 3));

        AssertLatestLog(LogLevel.Information, "'Milk'", "'Oat Milk'", id.ToString());
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryMissing_LogsWarningWithNameAndId()
    {
        var id = Guid.NewGuid();

        await _repository.UpdateAsync(new Grocery(id, "Milk", 2));

        AssertLatestLog(LogLevel.Warning, "Milk", id.ToString());
    }

    [Fact]
    public async Task DeleteAsync_WhenGroceryExists_LogsNameAndId()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new Grocery(id, "Milk", 2));

        await _repository.DeleteAsync(id);

        AssertLatestLog(LogLevel.Information, "Milk", id.ToString());
    }

    [Fact]
    public async Task DeleteAsync_WhenGroceryMissing_LogsWarningWithId()
    {
        var id = Guid.NewGuid();

        await _repository.DeleteAsync(id);

        AssertLatestLog(LogLevel.Warning, id.ToString());
    }

    private void AssertLatestLog(LogLevel expectedLevel, params string[] expectedFragments)
    {
        var record = _logger.LatestRecord;
        Assert.Equal(expectedLevel, record.Level);
        Assert.All(expectedFragments, fragment => Assert.Contains(fragment, record.Message));
    }
}
