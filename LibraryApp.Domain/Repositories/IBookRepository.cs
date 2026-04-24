using LibraryApp.Domain.Entities;
namespace LibraryApp.Domain.Repositories;
public interface IBookRepository
{
	Task<Book?> GetByIdAsync(Guid id,CancellationToken ct = default);
	Task<Book> AddAsync(Book book,CancellationToken ct = default);
	Task UpdateAsync(Book book,CancellationToken ct = default);
	Task DeleteAsync(Guid id,CancellationToken ct = default);
}