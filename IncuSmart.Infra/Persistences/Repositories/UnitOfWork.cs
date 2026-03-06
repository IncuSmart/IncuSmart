using Microsoft.EntityFrameworkCore.Storage;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _ctx;
        private IDbContextTransaction? _tx;

        public UnitOfWork(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task BeginAsync()
            => _tx = await _ctx.Database.BeginTransactionAsync();

        public async Task SaveChangesAsync()
            => await _ctx.SaveChangesAsync();

        public async Task CommitAsync()
            => await _tx!.CommitAsync();

        public async Task RollbackAsync()
            => await _tx!.RollbackAsync();
    }
}
