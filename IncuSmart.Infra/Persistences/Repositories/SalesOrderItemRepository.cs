namespace IncuSmart.Infra.Persistences.Repositories
{
    public class SalesOrderItemRepository : ISalesOrderItemRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SalesOrderItemRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddRange(List<SalesOrderItem> items)
        {
            var entities = items.Adapt<List<SalesOrderItemEntity>>();
            await _dbContext.SalesOrderItems.AddRangeAsync(entities);
        }

        public async Task<SalesOrderItem?> FindById(Guid id)
        {
            var entity = await _dbContext.SalesOrderItems
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<SalesOrderItem>();
        }

        public async Task<List<SalesOrderItem>> FindByOrderId(Guid orderId)
        {
            return (await _dbContext.SalesOrderItems
                .Where(x => x.OrderId == orderId && x.DeletedAt == null)
                .ToListAsync())
                .Adapt<List<SalesOrderItem>>();
        }

        public async Task Update(SalesOrderItem item)
        {
            var entity = await _dbContext.SalesOrderItems
                .FirstOrDefaultAsync(x => x.Id == item.Id && x.DeletedAt == null);

            if (entity != null)
            {
                item.Adapt(entity);
            }
        }
    }
}
