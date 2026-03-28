namespace IncuSmart.Core.Ports.Outbound
{
    public interface IUserRepository
    {
        // Quy ước: Get - Luôn có record, Find - Có thể không có record, FindAll - Trả về list, có thể rỗng
        Task<User?> FindByUserNameAndPasswordHashAndDeletedAtIsNull(string userName, string passwordHash);

        Task<User?> FindByUserNameAndDeletedAtIsNull(string userName);

        Task Add(User user);

        Task<User?> FindById(Guid id);
    }
}
