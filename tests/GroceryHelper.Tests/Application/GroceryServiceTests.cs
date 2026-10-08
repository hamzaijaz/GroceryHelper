using GroceryHelper.Application;
using GroceryHelper.Domain;
using NSubstitute;

namespace GroceryHelper.Tests.Application;

public class GroceryServiceTests
{
    private readonly IGroceryRepository _repository = Substitute.For<IGroceryRepository>();
    private readonly GroceryService _service;

    public GroceryServiceTests()
    {
        _service = new GroceryService(_repository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsGroceriesFromRepositoryForCategory()
    {
        IReadOnlyList<Grocery> groceries = [new Grocery(Guid.NewGuid(), "Milk", 2, GroceryCategory.Dairy)];
        _repository.GetAllAsync(GroceryCategory.Dairy).Returns(groceries);

        var result = await _service.GetAllAsync(GroceryCategory.Dairy);

        Assert.Same(groceries, result);
    }

    [Fact]
    public async Task CreateAsync_AssignsNewIdTrimsNameAndAddsToRepository()
    {
        var created = await _service.CreateAsync(new GroceryRequest("  Milk  ", 2, GroceryCategory.Dairy));

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(new Grocery(created.Id, "Milk", 2, GroceryCategory.Dairy), created);
        await _repository.Received(1).AddAsync(created);
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryExists_ReturnsUpdatedGrocery()
    {
        var id = Guid.NewGuid();
        var expected = new Grocery(id, "Oat Milk", 3);
        _repository.UpdateAsync(expected).Returns(true);

        var result = await _service.UpdateAsync(id, new GroceryRequest("Oat Milk", 3));

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_WhenGroceryMissing_ReturnsNull()
    {
        _repository.UpdateAsync(Arg.Any<Grocery>()).Returns(false);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new GroceryRequest("Milk", 2));

        Assert.Null(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsRepositoryResult(bool deleted)
    {
        var id = Guid.NewGuid();
        _repository.DeleteAsync(id).Returns(deleted);

        Assert.Equal(deleted, await _service.DeleteAsync(id));
    }
}
