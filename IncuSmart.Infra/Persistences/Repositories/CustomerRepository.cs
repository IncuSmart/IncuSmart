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
            CustomerEntity? entity = await _dbContext.Customers
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Customer>();
        }

        public async Task<Customer?> FindByUserId(Guid userId)
        {
            CustomerEntity? entity = await _dbContext.Customers
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeletedAt == null);
            return entity?.Adapt<Customer>();
        }

        public async Task<List<Customer>> List(string? status, string? search)
        {
            var query = _dbContext.Customers
                .Where(x => x.DeletedAt == null)
                .Join(
                    _dbContext.Users.Where(u => u.DeletedAt == null),
                    customer => customer.UserId,
                    user => user.Id,
                    (customer, user) => new { Customer = customer, User = user });

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BaseStatus>(status, out var statusEnum))
            {
                query = query.Where(x => x.Customer.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.User.FullName != null && x.User.FullName.Contains(search)) ||
                    (x.User.Phone != null && x.User.Phone.Contains(search)));
            }

            return (await query.Select(x => x.Customer).ToListAsync()).Adapt<List<Customer>>();
        }

        public async Task Update(Customer customer)
        {
            CustomerEntity? entity = await _dbContext.Customers
                .FirstOrDefaultAsync(x => x.Id == customer.Id && x.DeletedAt == null);

            if (entity != null)
            {
                customer.Adapt(entity);
            }
        }
    }
}
