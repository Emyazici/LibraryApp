using FluentAssertions;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Tests.Domain.ValueObjects;

public class MoneyTests
{
    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsAmountAndCurrency()
    {
        var money = Money.Create(10.50m, "TRY");
        money.Amount.Should().Be(10.50m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_CurrencyNormalizesToUpperCase()
    {
        var money = Money.Create(10m, "try");
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        var act = () => Money.Create(0m, "TRY");
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsBusinessRuleException()
    {
        var act = () => Money.Create(-1m, "TRY");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyCurrency_ThrowsBusinessRuleException()
    {
        var act = () => Money.Create(10m, "");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithWhitespaceCurrency_ThrowsBusinessRuleException()
    {
        var act = () => Money.Create(10m, "   ");
        act.Should().Throw<BusinessRuleException>();
    }

    // ── Add ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SameCurrency_ReturnsSummedAmount()
    {
        var result = Money.Create(10m, "TRY").Add(Money.Create(5m, "TRY"));
        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsBusinessRuleException()
    {
        var act = () => Money.Create(10m, "TRY").Add(Money.Create(5m, "USD"));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Add_ReturnsNewInstance()
    {
        var a = Money.Create(10m, "TRY");
        var b = Money.Create(5m, "TRY");
        var result = a.Add(b);
        result.Should().NotBeSameAs(a);
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Money.Create(10m, "TRY").Should().Be(Money.Create(10m, "TRY"));
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        Money.Create(10m, "TRY").Should().NotBe(Money.Create(20m, "TRY"));
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        Money.Create(10m, "TRY").Should().NotBe(Money.Create(10m, "USD"));
    }
}
