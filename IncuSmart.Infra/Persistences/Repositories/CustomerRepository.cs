namespace IncuSmart.Infra.Persistences.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public CustomerRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(Customer customer)
        {
            CustomerEntity entity = customer.Adapt<CustomerEntity>();
            await _dbContext.AddAsync(entity);
        }

        public async Task<Customer?> FindById(Guid id)
        {
            CustomerEntity? entity = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id);
            return entity != null ? entity.Adapt<Customer>() : null;
        }

        public async Task<Customer?> FindByUserId(Guid userId)
        {
            CustomerEntity? entity = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId.Equals(userId));
            return entity != null ? entity.Adapt<Customer>() : null;

        }
    }
}
