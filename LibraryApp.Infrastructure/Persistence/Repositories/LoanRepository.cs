using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryApp.Infrastructure.Persistence.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly LibraryDbContext _context;

        public LoanRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Loan> AddAsync(Loan loan, CancellationToken ct = default)
        {
            await _context.Loans.AddAsync(loan, ct);
            return loan;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, ct);
            entity?.MarkAsDeleted();
        }

        public async Task<List<Loan>> GetActiveLoansByMemberAsync(Guid memberId, CancellationToken ct = default)
        {
            return await _context.Loans
                .Where(l => l.MemberId == memberId && l.Status == LoanStatus.Active)
                .ToListAsync(ct);
        }

        public async Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Loans.FirstOrDefaultAsync(l => l.Id == id, ct);
        }

        public async Task<List<Loan>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
        {
            return await _context.Loans
                .Where(l => l.MemberId == memberId)
                .ToListAsync(ct);
        }

        public async Task<bool> HasActiveLoanAsync(Guid memberId, Guid bookId, CancellationToken ct = default)
        {
            return await _context.Loans.AnyAsync(
                l => l.MemberId == memberId && l.BookId == bookId && l.Status == LoanStatus.Active,
                ct);
        }

        public Task UpdateAsync(Loan loan, CancellationToken ct = default)
        {
            _context.Loans.Update(loan);
            return Task.CompletedTask;
        }
    }
}
