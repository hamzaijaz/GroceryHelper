using GroceryHelper.Models;
using GroceryHelper.Repositories;

namespace GroceryHelper.Tests.Repositories;

public class InMemoryGroceryRepositoryTests
{
    private readonly InMemoryGroceryRepository _repository = new();

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

    [Theory]
    [InlineData("Dairy")]
    [InlineData("dairy")]
    [InlineData("DAIRY")]
    public async Task GetAllAsync_WithCategory_ReturnsOnlyMatchingGroceriesIgnoringCase(string category)
    {
        var milk = new Grocery(Guid.NewGuid(), "Milk", 1, "Dairy");
        var cheese = new Grocery(Guid.NewGuid(), "Cheese", 1, "dairy");
        await _repository.AddAsync(milk);
        await _repository.AddAsync(cheese);
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Apples", 6, "Produce"));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Bread", 1));

        var groceries = await _repository.GetAllAsync(category);

        Assert.Equal([cheese, milk], groceries);
    }

    [Fact]
    public async Task GetAllAsync_WithCategory_WhenNoneMatch_ReturnsEmptyList()
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Milk", 1, "Dairy"));

        var groceries = await _repository.GetAllAsync("Bakery");

        Assert.Empty(groceries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_WithoutCategory_ReturnsAllGroceries(string? category)
    {
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Milk", 1, "Dairy"));
        await _repository.AddAsync(new Grocery(Guid.NewGuid(), "Bread", 1));

        var groceries = await _repository.GetAllAsync(category);

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
}
