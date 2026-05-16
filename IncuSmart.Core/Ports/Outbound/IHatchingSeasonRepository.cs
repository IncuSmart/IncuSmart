namespace IncuSmart.Core.Ports.Outbound
{
    public interface IHatchingSeasonRepository
    {
        Task Add(HatchingSeason season);
        Task Update(HatchingSeason season);
        Task<HatchingSeason?> FindById(Guid id);
        Task<List<HatchingSeason>> List(Guid? incubatorId, Guid? customerId, string? status);
        Task<List<HatchingSeason>> FindByIncubatorId(Guid incubatorId, string? status, string? eggType);
        Task<bool> ExistsByCode(string seasonCode);
    }
}
