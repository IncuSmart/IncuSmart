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
        {
            await _ctx.SaveChangesAsync();

            if (_tx != null)
            {
                await _tx.CommitAsync();
                await _tx.DisposeAsync();
                _tx = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_tx != null)
            {
                await _tx.RollbackAsync();
                await _tx.DisposeAsync();
                _tx = null;
            }
        }
    }
}
