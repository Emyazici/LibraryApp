using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryApp.Infrastructure.Persistence.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly LibraryDbContext _context;

        public MemberRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Member> AddAsync(Member member, CancellationToken ct = default)
        {
            await _context.Members.AddAsync(member, ct);
            return member;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, ct);
            entity?.MarkAsDeleted();
        }

        public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Members.AnyAsync(m => m.Id == id, ct);
        }

        public async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Members.FirstOrDefaultAsync(m => m.Id == id, ct);
        }

        public Task UpdateAsync(Member member, CancellationToken ct = default)
        {
            _context.Members.Update(member);
            return Task.CompletedTask;
        }
    }
}
