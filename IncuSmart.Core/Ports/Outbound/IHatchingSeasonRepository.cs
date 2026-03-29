namespace IncuSmart.Core.Ports.Outbound
{
    public interface IHatchingSeasonRepository
    {
        Task Add(HatchingSeason season);
        Task<HatchingSeason?> FindById(Guid id);
        Task<List<HatchingSeason>> FindAll(Guid? incubatorId, Guid? customerId);
        Task<bool> ExistsByCode(string seasonCode);
    }
}
