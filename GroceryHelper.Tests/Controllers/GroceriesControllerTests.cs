using System.Net;
using System.Net.Http.Json;
using GroceryHelper.Models;
using GroceryHelper.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GroceryHelper.Tests.Controllers;

/// <summary>
/// Integration tests that exercise the full HTTP pipeline (routing, validation, CORS, exception handling).
/// A new factory is created per test so the in-memory store starts empty every time.
/// </summary>
public sealed class GroceriesControllerTests : IDisposable
{
    private const string BaseUrl = "/api/groceries";
    private const string AllowedOrigin = "http://allowed.test";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GroceriesControllerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Get_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await response.Content.ReadFromJsonAsync<List<Grocery>>() ?? []);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsCreatedGrocery()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new GroceryRequest("Milk", 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Grocery>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Milk", created.Name);
        Assert.Equal(2, created.Quantity);
    }

    [Fact]
    public async Task Post_ThenGet_ReturnsCreatedGrocery()
    {
        var created = await CreateGroceryAsync("Milk", 2);

        var groceries = await _client.GetFromJsonAsync<List<Grocery>>(BaseUrl);

        Assert.Equal(created, Assert.Single(groceries!));
    }

    [Fact]
    public async Task Post_WithCategory_ReturnsGroceryWithTrimmedCategory()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new GroceryRequest("Milk", 2, "  Dairy  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Dairy", (await response.Content.ReadFromJsonAsync<Grocery>())?.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_WithoutCategory_ReturnsGroceryWithNullCategory(string? category)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new GroceryRequest("Milk", 2, category));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<Grocery>())?.Category);
    }

    [Fact]
    public async Task Get_WithCategory_ReturnsOnlyMatchingGroceries()
    {
        var milk = await CreateGroceryAsync("Milk", 2, "Dairy");
        await CreateGroceryAsync("Apples", 6, "Produce");
        await CreateGroceryAsync("Bread", 1);

        var groceries = await _client.GetFromJsonAsync<List<Grocery>>($"{BaseUrl}?category=dairy");

        Assert.Equal(milk, Assert.Single(groceries!));
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Post_WithInvalidRequest_ReturnsBadRequest(GroceryRequest request)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
    }

    [Fact]
    public async Task Put_WhenGroceryExists_ReturnsOkWithUpdatedGrocery()
    {
        var created = await CreateGroceryAsync("Milk", 2);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new GroceryRequest("Oat Milk", 3, "Plant-based"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = new Grocery(created.Id, "Oat Milk", 3, "Plant-based");
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<Grocery>());
        Assert.Equal(expected, Assert.Single((await _client.GetFromJsonAsync<List<Grocery>>(BaseUrl))!));
    }

    [Fact]
    public async Task Put_WhenGroceryMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", new GroceryRequest("Milk", 2));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Put_WithInvalidRequest_ReturnsBadRequest(GroceryRequest request)
    {
        var created = await CreateGroceryAsync("Milk", 2);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenGroceryExists_ReturnsNoContentAndRemovesIt()
    {
        var created = await CreateGroceryAsync("Milk", 2);

        var response = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<List<Grocery>>(BaseUrl))!);
    }

    [Fact]
    public async Task Delete_WhenGroceryMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_ReturnsInternalServerErrorProblemDetailsWithoutLeakingDetails()
    {
        var repository = Substitute.For<IGroceryRepository>();
        repository.GetAllAsync(Arg.Any<string?>()).ThrowsAsync(new InvalidOperationException("secret internal detail"));
        using var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(repository)))
            .CreateClient();

        var response = await client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret internal detail", body);
    }

    [Fact]
    public async Task CorsPreflight_FromAllowedOrigin_ReturnsAllowOriginHeader()
    {
        var response = await _client.SendAsync(CreatePreflightRequest(AllowedOrigin));

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(AllowedOrigin, Assert.Single(values));
    }

    [Fact]
    public async Task CorsPreflight_FromDisallowedOrigin_DoesNotReturnAllowOriginHeader()
    {
        var response = await _client.SendAsync(CreatePreflightRequest("http://evil.test"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    public static TheoryData<GroceryRequest> InvalidRequests() => new()
    {
        new GroceryRequest("", 1),
        new GroceryRequest("   ", 1),
        new GroceryRequest(new string('a', GroceryRequest.MaxNameLength + 1), 1),
        new GroceryRequest("Milk", 0),
        new GroceryRequest("Milk", -1),
        new GroceryRequest("Milk", GroceryRequest.MaxQuantity + 1),
        new GroceryRequest("Milk", 1, new string('a', GroceryRequest.MaxCategoryLength + 1)),
    };

    private async Task<Grocery> CreateGroceryAsync(string name, int quantity, string? category = null)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new GroceryRequest(name, quantity, category));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Grocery>())!;
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, BaseUrl);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }
}
