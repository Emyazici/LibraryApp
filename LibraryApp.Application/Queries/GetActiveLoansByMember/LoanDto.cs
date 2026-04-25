namespace LibraryApp.Application.Queries.GetActiveLoansByMember;

public record LoanDto(
    Guid      Id,
    Guid      BookId,
    string    BookTitle,       // kullanıcıya Guid değil isim göster
    DateTime  BorrowedAt,
    DateTime  ExpectedReturnDate,
    string    Status,          // LoanStatus enum → string
    bool      IsOverdue,       // frontend için kolaylık
    decimal   LateFeeAmount,
    string   LateFeeCurrency
);