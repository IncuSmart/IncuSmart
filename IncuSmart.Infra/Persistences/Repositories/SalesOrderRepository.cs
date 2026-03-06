namespace IncuSmart.Infra.Persistences.Repositories
{
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SalesOrderRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(SalesOrder salesOrder)
        {
            SalesOrderEntity entity = salesOrder.Adapt<SalesOrderEntity>();
            await _dbContext.AddAsync(entity);
        }
    }
}
