using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryApp.Application.Queries.GetBookById;
using LibraryApp.IntegrationTests.Infrastructure;

namespace LibraryApp.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class BooksEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;

    public BooksEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ListBooks_AsAuthenticatedUser_ContainsSeededBook()
    {
        var bookId = await _factory.SeedBookAsync();
        var client = _factory.CreateClientAs(Guid.NewGuid(), "Customer");

        var response = await client.GetAsync("/api/books");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        books.Should().Contain(b => b.Id == bookId);
    }
}
