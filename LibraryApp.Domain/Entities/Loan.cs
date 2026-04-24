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
	public DateTime? ActualReturnDate { get; private set; }
	public LoanStatus Status { get; private set; }

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
			Status = LoanStatus.Active
		};
		loan.AddDomainEvent(new LoanBorrowedEvent(loan.Id, loan.BookId, loan.MemberId, loan.Period));
		return loan;
	}


	public int CalculateFee()
	{
		var overdueDays = (int)(DateTime.UtcNow - Period.ExpectedReturnDate).TotalDays;
		return overdueDays > 0 ? overdueDays * 2 : 0; // Örneğin, her gecikme günü için 2 birim ücret
	}
	public void Return()
	{
		if (Status == LoanStatus.Returned)
			throw new BusinessRuleException("Bu ödünç işlemi zaten iade edilmiş.");

		ActualReturnDate = DateTime.UtcNow;

		if (Period.IsOverdue())
		{
			Status = LoanStatus.Overdue;
			CalculateFee(); // Gecikme ücreti hesaplanır, istenirse bu değer bir property olarak saklanabilir
		}

		else
		{
			Status = LoanStatus.Returned;
		}

		AddDomainEvent(new LoanReturnedEvent(Id, BookId, MemberId));
	}

	// İhtiyaç halinde ek özellikler eklenebilir (örneğin, gecikme ücreti, durum vb.)
}