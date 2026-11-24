using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using LibraryApp.IntegrationTests.Infrastructure;

namespace LibraryApp.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class LoanFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    public LoanFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RegisteredCustomer_CanBorrowAndReturnBook_AndSeesItInHistory()
    {
        // Register (anonymous) — Keycloak is faked; the returned id equals the Keycloak "sub".
        var anonymous = _factory.CreateAnonymousClient();
        var registerResponse = await anonymous.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Grace",
            surname = "Hopper",
            email = $"grace-{Guid.NewGuid():N}@test.local",
            password = "Sup3rSecret!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var memberId = await registerResponse.Content.ReadFromJsonAsync<Guid>();
        memberId.Should().NotBeEmpty();

        var bookId = await _factory.SeedBookAsync(totalStock: 2, title: "The Pragmatic Programmer");
        var customer = _factory.CreateClientAs(memberId, "Customer");

        // Borrow — member id comes from the token's sub claim.
        var borrowResponse = await customer.PostAsJsonAsync("/api/loans", new
        {
            bookId,
            dueDate = DateTime.UtcNow.AddDays(14)
        });
        borrowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loanId = await borrowResponse.Content.ReadFromJsonAsync<Guid>();
        loanId.Should().NotBeEmpty();

        // Active loans contain it.
        var active = await customer.GetFromJsonAsync<List<LoanDto>>("/api/loans/me/active");
        active.Should().NotBeNull();
        active!.Should().ContainSingle(l => l.Id == loanId)
            .Which.BookId.Should().Be(bookId);

        // Return.
        var returnResponse = await customer.PostAsync($"/api/loans/{loanId}/return", content: null);
        returnResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // No longer active, history shows it as returned.
        var activeAfterReturn = await customer.GetFromJsonAsync<List<LoanDto>>("/api/loans/me/active");
        activeAfterReturn!.Should().NotContain(l => l.Id == loanId);

        var history = await customer.GetFromJsonAsync<List<LoanDto>>("/api/loans/me/history");
        history.Should().NotBeNull();
        history!.Should().ContainSingle(l => l.Id == loanId)
            .Which.Status.Should().Be("Returned");
    }

    [Fact]
    public async Task Borrow_AsEmployee_Returns403()
    {
        var bookId = await _factory.SeedBookAsync();
        var employee = _factory.CreateClientAs(Guid.NewGuid(), "Employee");

        var response = await employee.PostAsJsonAsync("/api/loans", new
        {
            bookId,
            dueDate = DateTime.UtcNow.AddDays(7)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
