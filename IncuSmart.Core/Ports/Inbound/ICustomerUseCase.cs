namespace IncuSmart.Core.Ports.Inbound
{
    public interface ICustomerUseCase
    {
        Task<ResultModel<CustomerDetailResponse?>> GetById(Guid id);
        Task<ResultModel<CustomerDetailResponse?>> GetByUserId(Guid userId);
        Task<ResultModel<PagedResult<CustomerSummaryResponse>>> List(string? status, string? search, int page, int pageSize);
        Task<ResultModel<List<SalesOrder>>> GetOrders(Guid customerId);
        Task<ResultModel<bool>> UpdateProfile(UpdateCustomerProfileCommand command);
    }
}
