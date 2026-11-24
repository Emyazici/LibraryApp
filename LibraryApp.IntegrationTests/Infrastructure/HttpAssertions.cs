using System.Text.Json;
using FluentAssertions;

namespace LibraryApp.IntegrationTests.Infrastructure;

public static class HttpAssertions
{
    public const string ProblemJson = "application/problem+json";

    /// <summary>Asserts the response is RFC 7807 problem details and returns the parsed body.</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be(ProblemJson);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>Case-insensitive property lookup (serializers may camelCase keys).</summary>
    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
