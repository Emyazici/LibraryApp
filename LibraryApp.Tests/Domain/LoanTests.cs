using FluentAssertions;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.Events;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Tests.Domain;

public class LoanTests
{
    private static readonly Guid BookId   = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    private static LoanPeriod OnTimePeriod()
        => LoanPeriod.Create(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(14));

    private static LoanPeriod OverduePeriod()
        => LoanPeriod.Create(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsActiveStatus()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public void Create_WithValidData_FeeIsZero()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Fee.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_WithValidData_RaisesBorrowedEvent()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.DomainEvents.Should().ContainSingle(e => e is LoanBorrowedEvent);
    }

    [Fact]
    public void Create_WithEmptyBookId_ThrowsBusinessRuleException()
    {
        var act = () => Loan.Create(Guid.Empty, MemberId, OnTimePeriod());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyMemberId_ThrowsBusinessRuleException()
    {
        var act = () => Loan.Create(BookId, Guid.Empty, OnTimePeriod());
        act.Should().Throw<BusinessRuleException>();
    }

    // ── Return (zamanında) ───────────────────────────────────────────────────

    [Fact]
    public void Return_WhenOnTime_SetsReturnedStatus()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Return();
        loan.Status.Should().Be(LoanStatus.Returned);
    }

    [Fact]
    public void Return_WhenOnTime_SetsActualReturnDate()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Return();
        loan.ActualReturnDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Return_WhenOnTime_FeeRemainsZero()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Return();
        loan.Fee.Amount.Should().Be(0);
    }

    [Fact]
    public void Return_RaisesReturnedEvent()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Return();
        loan.DomainEvents.Should().Contain(e => e is LoanReturnedEvent);
    }

    // ── Return (geç iade) ────────────────────────────────────────────────────

    [Fact]
    public void Return_WhenOverdue_SetsOverdueStatus()
    {
        var loan = Loan.Create(BookId, MemberId, OverduePeriod());
        loan.Return();
        loan.Status.Should().Be(LoanStatus.Overdue);
    }

    [Fact]
    public void Return_WhenOverdue_SetsPositiveFee()
    {
        var loan = Loan.Create(BookId, MemberId, OverduePeriod());
        loan.Return();
        loan.Fee.Amount.Should().BePositive();
    }

    [Fact]
    public void Return_WhenOverdue_SetsPositiveOverDueDays()
    {
        var loan = Loan.Create(BookId, MemberId, OverduePeriod());
        loan.Return();
        loan.OverDueDays.Should().BePositive();
    }

    // ── Guard clause'lar ─────────────────────────────────────────────────────

    [Fact]
    public void Return_WhenAlreadyReturned_ThrowsBusinessRuleException()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.Return();
        var act = () => loan.Return();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Return_WhenAlreadyOverdue_ThrowsBusinessRuleException()
    {
        var loan = Loan.Create(BookId, MemberId, OverduePeriod());
        loan.Return(); // Status → Overdue
        var act = () => loan.Return();
        act.Should().Throw<BusinessRuleException>();
    }

    // ── CalculateFee ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFee_WhenNotOverdue_ReturnsZero()
    {
        var loan = Loan.Create(BookId, MemberId, OnTimePeriod());
        loan.CalculateFee().Should().Be(0);
    }

    [Fact]
    public void CalculateFee_WhenOverdue_ReturnsPositive()
    {
        var loan = Loan.Create(BookId, MemberId, OverduePeriod());
        loan.CalculateFee().Should().BePositive();
    }
}
