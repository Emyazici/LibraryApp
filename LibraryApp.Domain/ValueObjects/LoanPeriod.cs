using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.ValueObjects;
public sealed class LoanPeriod : ValueObject
{
    public DateTime BorrowedAt { get; }
    public DateTime ExpectedReturnDate   { get; }
    private LoanPeriod(DateTime start, DateTime due)
    {
        BorrowedAt = start;
        ExpectedReturnDate   = due;
    }

    public static LoanPeriod Create(DateTime start, DateTime due)
    {
        if (due <= start)
            throw new BusinessRuleException("Bitiş tarihi başlangıçtan sonra olmalı.");

        if ((due - start).TotalDays > 60)
            throw new BusinessRuleException("Ödünç süresi 60 günü geçemez.");

        return new LoanPeriod(start, due);
    }

    // İş kuralı metodları
    public bool IsOverdue() => DateTime.UtcNow > ExpectedReturnDate;

    public int  DaysRemaining()
        => (int)(ExpectedReturnDate - DateTime.UtcNow).TotalDays;

    public LoanPeriod Extend(int days)
    {
        var newDue = ExpectedReturnDate.AddDays(days);
        return Create(BorrowedAt, newDue); // validasyon tekrar çalışır
    }

    // ★ İki tarih birden eşitliği belirliyor
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BorrowedAt;
        yield return ExpectedReturnDate;
    }

    public override string ToString()
        => $"{BorrowedAt:dd.MM.yyyy} → {ExpectedReturnDate:dd.MM.yyyy}";
}