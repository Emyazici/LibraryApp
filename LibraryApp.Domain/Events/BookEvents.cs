using LibraryApp.Domain.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Domain.Events;

public record class BookCreatedEvent(Guid BookId, Guid AuthorId,string Title, ISBN ISBN,Money Price,int TotalStock,BookStatus Status) : IDomainEvent
{
	public Guid Id { get; } = Guid.NewGuid();
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record class BookBorrowedEvent(Guid BookId, Guid AuthorId, string Title) : IDomainEvent
{
	public Guid Id { get; } = Guid.NewGuid();
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record class BookReturnedEvent(Guid BookId, Guid AuthorId, string Title) : IDomainEvent
{
	public Guid Id { get; } = Guid.NewGuid();
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}