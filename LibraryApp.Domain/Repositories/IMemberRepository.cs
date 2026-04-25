using LibraryApp.Domain.Entities;
namespace LibraryApp.Domain.Repositories;
public interface IMemberRepository
{
	Task<Member?> GetByIdAsync(Guid id,CancellationToken ct = default);
	Task<Member> AddAsync(Member member,CancellationToken ct = default);
	Task UpdateAsync(Member member,CancellationToken ct = default);
	Task DeleteAsync(Guid id,CancellationToken ct = default);
	Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default);
}