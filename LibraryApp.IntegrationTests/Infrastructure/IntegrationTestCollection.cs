namespace LibraryApp.IntegrationTests.Infrastructure;

/// <summary>One PostgreSQL container and one API host shared by every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
