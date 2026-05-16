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

        public async Task<SalesOrder?> FindById(Guid id)
        {
            var entity = await _dbContext.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<SalesOrder>();
        }

        public async Task<SalesOrder?> FindByPaymentOrderCode(long paymentOrderCode)
        {
            var entity = await _dbContext.SalesOrders
                .FirstOrDefaultAsync(x => x.PaymentOrderCode == paymentOrderCode && x.DeletedAt == null);
            return entity?.Adapt<SalesOrder>();
        }

        public async Task<List<SalesOrder>> FindByCustomerId(Guid customerId)
        {
            return (await _dbContext.SalesOrders
                .Where(x => x.CustomerId == customerId && x.DeletedAt == null)
                .ToListAsync())
                .Adapt<List<SalesOrder>>();
        }

        public async Task<List<SalesOrder>> FindAll(string? status, Guid? customerId)
        {
            var query = _dbContext.SalesOrders.Where(x => x.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (customerId.HasValue)
                query = query.Where(x => x.CustomerId == customerId.Value);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<SalesOrder>>();
        }

        public async Task Update(SalesOrder salesOrder)
        {
            var entity = await _dbContext.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == salesOrder.Id && x.DeletedAt == null);

            if (entity != null)
            {
                salesOrder.Adapt(entity);
            }
        }
    }
}
