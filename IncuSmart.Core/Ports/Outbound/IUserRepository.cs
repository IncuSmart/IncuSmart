namespace IncuSmart.Core.Ports.Outbound
{
    public interface IUserRepository
    {
        // Quy ước: Get - Luôn có record, Find - Có thể không có record, List - Trả về list, có thể rỗng
        Task<User?> FindByUserNameAndPasswordHashAndDeletedAtIsNull(string userName, string passwordHash);
        Task<User?> FindByUserNameAndDeletedAtIsNull(string userName);
        Task<User?> FindByEmailAndDeletedAtIsNull(string email);
        Task<User?> FindByPhoneAndDeletedAtIsNull(string phone);
        Task<User?> FindById(Guid id);
        Task<List<User>> FindByIds(List<Guid> ids);
        Task<List<User>> List(string? role, string? status);
        Task Add(User user);
        Task Update(User user);
    }
}
