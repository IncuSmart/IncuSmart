namespace IncuSmart.Core.Ports.Inbound
{
    public interface IWarrantyUseCase
    {
        Task<ResultModel<Guid?>>      Create(CreateWarrantyCommand command);
        Task<ResultModel<bool>>       Update(UpdateWarrantyCommand command);
        Task<ResultModel<Warranty?>>  GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role);
    }
}
