using LibraryApp.Domain.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Domain.Events;

public record class LoanBorrowedEvent(Guid LoanId, Guid BookId, Guid MemberId, LoanPeriod Period) : IDomainEvent
{
	public Guid Id { get; } = Guid.NewGuid();
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record class LoanReturnedEvent(Guid LoanId, Guid BookId, Guid MemberId) : IDomainEvent
{
	public Guid Id { get; } = Guid.NewGuid();
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}