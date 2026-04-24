using LibraryApp.Domain.Common;
using LibraryApp.Domain.ValueObjects;
using LibraryApp.Domain.Events;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.Entities;
public class Book : AggregateRoot
{
	public Guid AuthorId { get; private set; }
	public string Title { get; private set; }
	public ISBN ISBN { get; private set; }
	public Money Price { get; private set; }
	public int TotalStock { get; private set; }	
	public BookStatus Status { get; private set; }
	

	private Book(){}

	public static Book Create(Guid authorId,string title,  ISBN isbn, Money price ,int totalStock)
	{
		if (string.IsNullOrWhiteSpace(title))
			throw new BusinessRuleException("Kitap başlığı boş olamaz.");

		if (authorId == Guid.Empty)
			throw new BusinessRuleException("Yazar bilgisi boş olamaz.");

		if (totalStock <= 0)
			throw new BusinessRuleException("Toplam stok negatif veya 0 olamaz.");

		var book = new Book
		{
			Id = Guid.NewGuid(),
			AuthorId = authorId,
			Title = title,
			ISBN = isbn,
			Price = price,
			TotalStock = totalStock,
			Status = BookStatus.Available
		};
		book.AddDomainEvent(new BookCreatedEvent(book.Id, book.AuthorId, book.Title, book.ISBN, book.Price, book.TotalStock, book.Status));

		return book;
	}

	public void Borrow()
	{
		if(Status == BookStatus.Loaned)
			throw new BusinessRuleException("Bu Kitabın Tamamı Ödünç Verilmiş.");
		if(TotalStock <= 0)
			throw new BusinessRuleException("Bu Kitabın Stokları Tükenmiş.");
		
		TotalStock--;
		if(TotalStock == 0)
			Status = BookStatus.Loaned;
		
		AddDomainEvent(new BookBorrowedEvent(Id, AuthorId, Title));
	}

	public void Return()
	{
		TotalStock++;
		Status = BookStatus.Available;
		AddDomainEvent(new BookReturnedEvent(Id, AuthorId, Title));
	}

}