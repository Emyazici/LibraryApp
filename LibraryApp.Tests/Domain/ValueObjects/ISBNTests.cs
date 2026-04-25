using FluentAssertions;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Tests.Domain.ValueObjects;

public class ISBNTests
{
    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidISBN13_ReturnsCleanedValue()
    {
        var isbn = ISBN.Create("9780134685991");
        isbn.Value.Should().Be("9780134685991");
    }

    [Fact]
    public void Create_ValidISBN10_ReturnsCleanedValue()
    {
        var isbn = ISBN.Create("0134685997");
        isbn.Value.Should().Be("0134685997");
    }

    [Fact]
    public void Create_WithDashes_StripsAndAccepts()
    {
        var isbn = ISBN.Create("978-0-13-468599-1");
        isbn.Value.Should().Be("9780134685991");
    }

    [Fact]
    public void Create_WithSpaces_StripsAndAccepts()
    {
        var isbn = ISBN.Create("978 0 13 468599 1");
        isbn.Value.Should().Be("9780134685991");
    }

    [Fact]
    public void Create_Empty_ThrowsBusinessRuleException()
    {
        var act = () => ISBN.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_Whitespace_ThrowsBusinessRuleException()
    {
        var act = () => ISBN.Create("   ");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WrongLength_ThrowsBusinessRuleException()
    {
        var act = () => ISBN.Create("12345");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NonDigitChars_ThrowsBusinessRuleException()
    {
        var act = () => ISBN.Create("978013468599X"); // X harf
        act.Should().Throw<BusinessRuleException>();
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        ISBN.Create("9780134685991").Should().Be(ISBN.Create("9780134685991"));
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        ISBN.Create("9780134685991").Should().NotBe(ISBN.Create("0134685997"));
    }

    [Fact]
    public void Equality_DashVsNoDash_AreEqual()
    {
        ISBN.Create("978-0-13-468599-1").Should().Be(ISBN.Create("9780134685991"));
    }
}
