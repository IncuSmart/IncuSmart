namespace IncuSmart.Core.Ports.Inbound
{
    public interface IOrderUseCase
    {
        Task<ResultModel<CreateOrderResponse?>> CreateOrderByCustomer(CreateOrderByCustomerCommand command);
        Task<ResultModel<CreateOrderResponse?>> CreateOrderByGuest(CreateOrderByGuestCommand command);
        Task<ResultModel<CreateOrderResponse?>> CreateOrderBySales(CreateOrderBySalesCommand command);
        Task<ResultModel<bool>> AssignIncubatorToOrderItem(AssignIncubatorToOrderItemCommand command);
        Task<ResultModel<bool>> CompleteOrder(CompleteOrderCommand command);
        Task<ResultModel<bool>> ClaimGuestOrder(ClaimGuestOrderCommand command);
        Task<ResultModel<bool>> HandlePaymentWebhook(HandleOrderPaymentWebhookCommand command);
        Task<ResultModel<bool>> CancelOrder(CancelOrderCommand command);
        Task<ResultModel<SalesOrderDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<PagedResult<SalesOrder>>> List(string? status, Guid? customerId, Guid? currentUserId, string role, int page, int pageSize);
    }
}
