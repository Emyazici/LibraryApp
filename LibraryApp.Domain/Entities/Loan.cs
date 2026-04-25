using LibraryApp.Domain.Common;
using LibraryApp.Domain.ValueObjects;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.Events;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.Entities;
public class Loan : AggregateRoot
{
	public Guid BookId { get; private set; }
	public Guid MemberId { get; private set; }
	public LoanPeriod Period { get; private set; }
	public Money Fee { get; private set; }
	public DateTime? ActualReturnDate { get; private set; }
	public LoanStatus Status { get; private set; }
	public int OverDueDays { get; private set; }

	private Loan() {}

	public static Loan Create(Guid bookId, Guid memberId, LoanPeriod period)
	{
		if (bookId == Guid.Empty)
			throw new BusinessRuleException("Geçersiz kitap ID'si.");
		if (memberId == Guid.Empty)
			throw new BusinessRuleException("Geçersiz üye ID'si.");

		var loan = new Loan
		{
			Id = Guid.NewGuid(),
			BookId = bookId,
			MemberId = memberId,
			Period = period,
			Fee = Money.Create(0, "TRY"), // Başlangıçta ücret sıfır
			OverDueDays=0, 
			Status = LoanStatus.Active
		};
		loan.AddDomainEvent(new LoanBorrowedEvent(loan.Id, loan.BookId, loan.MemberId, loan.Period));
		return loan;
	}


	public int CalculateFee()
	{
		var overdueDays = (int)(DateTime.UtcNow - Period.ExpectedReturnDate).TotalDays;
		return overdueDays > 0 ? overdueDays * 2 : 0; // Örnek: Gecikme başına 5 TRY
	}
	public void Return()
	{
		if (Status == LoanStatus.Returned || Status == LoanStatus.Overdue)
			throw new BusinessRuleException("Bu ödünç işlemi zaten iade edilmiş.");

		ActualReturnDate = DateTime.UtcNow;

		if (Period.IsOverdue())
		{
			Status = LoanStatus.Overdue;
			OverDueDays = (int)(ActualReturnDate.Value - Period.ExpectedReturnDate).TotalDays;
			Fee = Money.Create(CalculateFee(), "TRY"); 
		}

		else
		{
			Status = LoanStatus.Returned;
		}

		AddDomainEvent(new LoanReturnedEvent(Id, BookId, MemberId));
	}

	// İhtiyaç halinde ek özellikler eklenebilir (örneğin, gecikme ücreti, durum vb.)
}