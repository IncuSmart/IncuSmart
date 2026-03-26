
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

        public async Task Add(User user)
        {
            UserEntity entity = user.Adapt<UserEntity>();
            await _dbContext.AddAsync(entity);
        }

        public async Task<User?> FindById(Guid id)
        {
            UserEntity? entity =  await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.Equals(id));
            return entity?.Adapt<User>();
        }
    }
}
