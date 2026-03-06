namespace IncuSmart.Core.Ports.Inbound
{
    public interface IOrderUseCase
    {
        Task<ResultModel<Guid?>> CreateOrderByCustomer(CreateOrderByCustomerCommand command);
        Task<ResultModel<Guid?>> CreateOrderByGuest(CreateOrderByGuestCommand command);
    }
}
