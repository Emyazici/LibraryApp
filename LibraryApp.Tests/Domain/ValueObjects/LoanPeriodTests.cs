using FluentAssertions;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Tests.Domain.ValueObjects;

public class LoanPeriodTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidPeriod_SetsDatesCorrectly()
    {
        var start  = Now;
        var due    = Now.AddDays(14);
        var period = LoanPeriod.Create(start, due);

        period.BorrowedAt.Should().Be(start);
        period.ExpectedReturnDate.Should().Be(due);
    }

    [Fact]
    public void Create_DueBeforeStart_ThrowsBusinessRuleException()
    {
        var act = () => LoanPeriod.Create(Now.AddDays(5), Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_DueEqualToStart_ThrowsBusinessRuleException()
    {
        var act = () => LoanPeriod.Create(Now, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_Over60Days_ThrowsBusinessRuleException()
    {
        var act = () => LoanPeriod.Create(Now, Now.AddDays(61));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_Exactly60Days_Succeeds()
    {
        var act = () => LoanPeriod.Create(Now, Now.AddDays(60));
        act.Should().NotThrow();
    }

    // ── IsOverdue ────────────────────────────────────────────────────────────

    [Fact]
    public void IsOverdue_WhenExpiredYesterday_ReturnsTrue()
    {
        var period = LoanPeriod.Create(Now.AddDays(-30), Now.AddDays(-1));
        period.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenDueIsFuture_ReturnsFalse()
    {
        var period = LoanPeriod.Create(Now.AddMinutes(-1), Now.AddDays(14));
        period.IsOverdue().Should().BeFalse();
    }

    // ── DaysRemaining ────────────────────────────────────────────────────────

    [Fact]
    public void DaysRemaining_WhenFuture_ReturnsPositive()
    {
        var period = LoanPeriod.Create(Now.AddMinutes(-1), Now.AddDays(10));
        period.DaysRemaining().Should().BePositive();
    }

    [Fact]
    public void DaysRemaining_WhenOverdue_ReturnsNegativeOrZero()
    {
        var period = LoanPeriod.Create(Now.AddDays(-30), Now.AddDays(-1));
        period.DaysRemaining().Should().BeLessThan(1);
    }

    // ── Extend ───────────────────────────────────────────────────────────────

    [Fact]
    public void Extend_AddsCorrectDays()
    {
        var due    = Now.AddDays(10);
        var period = LoanPeriod.Create(Now, due);

        var extended = period.Extend(5);

        extended.ExpectedReturnDate.Should().Be(due.AddDays(5));
    }

    [Fact]
    public void Extend_PreservesStartDate()
    {
        var start  = Now;
        var period = LoanPeriod.Create(start, Now.AddDays(10));

        var extended = period.Extend(5);

        extended.BorrowedAt.Should().Be(start);
    }

    [Fact]
    public void Extend_WhenExceeds60Days_ThrowsBusinessRuleException()
    {
        var period = LoanPeriod.Create(Now, Now.AddDays(59));
        var act    = () => period.Extend(5); // toplam 64 gün
        act.Should().Throw<BusinessRuleException>();
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameDates_AreEqual()
    {
        var start = Now;
        var due   = Now.AddDays(14);
        LoanPeriod.Create(start, due).Should().Be(LoanPeriod.Create(start, due));
    }

    [Fact]
    public void Equality_DifferentDue_AreNotEqual()
    {
        LoanPeriod.Create(Now, Now.AddDays(14))
                  .Should().NotBe(LoanPeriod.Create(Now, Now.AddDays(20)));
    }
}
