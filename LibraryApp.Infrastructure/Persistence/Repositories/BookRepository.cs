using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Infrastructure.Persistence.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryDbContext _context;

        public BookRepository(LibraryDbContext context)
        {
            _context = context;
        }
        public async Task<Book> AddAsync(Book book, CancellationToken ct = default)
        {
            await _context.Books.AddAsync(book,ct);
            return book;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, ct);
            entity?.MarkAsDeleted();
        }

        public async Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default)
        {
            return await _context.Books.AnyAsync(b => b.Isbn.Value == isbn, ct);
        }

        public async Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<List<Book>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            return await _context.Books.Where(b => ids.Contains(b.Id)).ToListAsync(ct);
        }

        public Task UpdateAsync(Book book, CancellationToken ct = default)
        {
            _context.Books.Update(book);
            return Task.CompletedTask;
        }
    }
}
