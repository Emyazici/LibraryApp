using System.Net;
using FluentAssertions;
using LibraryApp.IntegrationTests.Infrastructure;

namespace LibraryApp.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthorizationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ActiveLoans_WithoutToken_Returns401()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/loans/me/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteBook_AsCustomer_Returns403_AndBookStillExists()
    {
        var bookId = await _factory.SeedBookAsync();
        var customer = _factory.CreateClientAs(Guid.NewGuid(), "Customer");

        var deleteResponse = await customer.DeleteAsync($"/api/books/{bookId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var getResponse = await customer.GetAsync($"/api/books/{bookId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteBook_AsAdmin_Returns204_ThenBookIsNotFound()
    {
        var bookId = await _factory.SeedBookAsync();
        var admin = _factory.CreateClientAs(Guid.NewGuid(), "Admin");

        var deleteResponse = await admin.DeleteAsync($"/api/books/{bookId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await admin.GetAsync($"/api/books/{bookId}");
        getResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
