using System.Net;
using FluentAssertions;
using LibraryApp.IntegrationTests.Infrastructure;

namespace LibraryApp.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class HealthEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_IsAnonymous_AndReportsHealthyDatabase()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
