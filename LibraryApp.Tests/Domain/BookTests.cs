using FluentAssertions;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.Events;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Tests.Domain;

public class BookTests
{
    private static Book CreateBook(int stock = 5)
        => Book.Create(Guid.NewGuid(), "Clean Code", "9780134685991", 29.99m, "TRY", stock);

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsAvailableStatus()
    {
        var book = CreateBook();
        book.Status.Should().Be(BookStatus.Available);
    }

    [Fact]
    public void Create_WithValidData_SetsCorrectStock()
    {
        var book = CreateBook(stock: 3);
        book.TotalStock.Should().Be(3);
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsBusinessRuleException()
    {
        var act = () => Book.Create(Guid.NewGuid(), "", "9780134685991", 10m, "TRY", 5);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyAuthorId_ThrowsBusinessRuleException()
    {
        var act = () => Book.Create(Guid.Empty, "Kitap", "9780134685991", 10m, "TRY", 5);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithZeroStock_ThrowsBusinessRuleException()
    {
        var act = () => Book.Create(Guid.NewGuid(), "Kitap", "9780134685991", 10m, "TRY", 0);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsBusinessRuleException()
    {
        var act = () => Book.Create(Guid.NewGuid(), "Kitap", "9780134685991", 10m, "TRY", -1);
        act.Should().Throw<BusinessRuleException>();
    }

    // ── Borrow ──────────────────────────────────────────────────────────────

    [Fact]
    public void Borrow_DecreasesStockByOne()
    {
        var book = CreateBook(stock: 3);
        book.Borrow();
        book.TotalStock.Should().Be(2);
    }

    [Fact]
    public void Borrow_WhenNotLastCopy_KeepsAvailableStatus()
    {
        var book = CreateBook(stock: 2);
        book.Borrow();
        book.Status.Should().Be(BookStatus.Available);
    }

    [Fact]
    public void Borrow_WhenLastCopy_SetsLoanedStatus()
    {
        var book = CreateBook(stock: 1);
        book.Borrow();
        book.Status.Should().Be(BookStatus.Loaned);
    }

    [Fact]
    public void Borrow_WhenLoaned_ThrowsBusinessRuleException()
    {
        var book = CreateBook(stock: 1);
        book.Borrow(); // stock → 0, Status = Loaned
        var act = () => book.Borrow();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Borrow_RaisesBorrowedEvent()
    {
        var book = CreateBook();
        book.ClearDomainEvents();
        book.Borrow();
        book.DomainEvents.Should().ContainSingle(e => e is BookBorrowedEvent);
    }

    // ── Return ───────────────────────────────────────────────────────────────

    [Fact]
    public void Return_IncreasesStockByOne()
    {
        var book = CreateBook(stock: 1);
        book.Borrow();
        book.Return();
        book.TotalStock.Should().Be(1);
    }

    [Fact]
    public void Return_SetsAvailableStatus()
    {
        var book = CreateBook(stock: 1);
        book.Borrow();
        book.Return();
        book.Status.Should().Be(BookStatus.Available);
    }

    [Fact]
    public void Return_RaisesReturnedEvent()
    {
        var book = CreateBook(stock: 1);
        book.Borrow();
        book.ClearDomainEvents();
        book.Return();
        book.DomainEvents.Should().ContainSingle(e => e is BookReturnedEvent);
    }
}
