using LibraryApp.Domain.Entities;

namespace LibraryApp.Domain.Repositories;

public interface ILoanRepository
{
	Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default);
	Task<List<Loan>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
	Task<bool> HasActiveLoanAsync(Guid memberId,Guid bookId, CancellationToken ct = default);
	Task<Loan> AddAsync(Loan loan, CancellationToken ct = default);
	Task UpdateAsync(Loan loan, CancellationToken ct = default);
	Task DeleteAsync(Guid id, CancellationToken ct = default);
}