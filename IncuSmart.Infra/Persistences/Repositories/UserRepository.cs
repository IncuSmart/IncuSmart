namespace IncuSmart.Infra.Persistences.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task<User?> FindByUserNameAndPasswordHashAndDeletedAtIsNull(string userName, string passwordHash)
        {
            UserEntity? entity = await _dbContext.Users
                .Where(x => x.Username == userName && x.PasswordHash == passwordHash && x.DeletedAt == null)
                .FirstOrDefaultAsync();

            return entity != null ? entity.Adapt<User>() : null;
        }

        public async Task<User?> FindByUserNameAndDeletedAtIsNull(string userName)
        {
            UserEntity? entity = await _dbContext.Users
                            .Where(x => x.Username == userName && x.DeletedAt == null)
                            .FirstOrDefaultAsync();

            return entity?.Adapt<User>();
        }

        public async Task<User?> FindByEmailAndDeletedAtIsNull(string email)
        {
            var entity = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Email == email && x.DeletedAt == null);

            return entity?.Adapt<User>();
        }

        public async Task<User?> FindByPhoneAndDeletedAtIsNull(string phone)
        {
            var entity = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Phone == phone && x.DeletedAt == null);

            return entity?.Adapt<User>();
        }

        public async Task Add(User user)
        {
            UserEntity entity = user.Adapt<UserEntity>();
            await _dbContext.AddAsync(entity);
        }

        public async Task<User?> FindById(Guid id)
        {
            UserEntity? entity = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<User>();
        }

        public async Task<List<User>> FindByIds(List<Guid> ids)
        {
            return (await _dbContext.Users
                .Where(x => ids.Contains(x.Id) && x.DeletedAt == null)
                .ToListAsync())
                .Adapt<List<User>>();
        }

        public async Task<List<User>> List(string? role, string? status)
        {
            var query = _dbContext.Users.Where(x => x.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var roleEnum))
            {
                query = query.Where(x => x.Role == roleEnum);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BaseStatus>(status, out var statusEnum))
            {
                query = query.Where(x => x.Status == statusEnum);
            }

            return (await query.ToListAsync()).Adapt<List<User>>();
        }

        public async Task Update(User user)
        {
            UserEntity? entity = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
            if (entity == null)
            {
                return;
            }

            user.Adapt(entity);
        }
    }
}
