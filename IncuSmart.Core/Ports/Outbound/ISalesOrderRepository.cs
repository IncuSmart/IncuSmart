namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISalesOrderRepository
    {
        Task Add(SalesOrder salesOrder);
        Task<SalesOrder?> FindById(Guid id);
        Task<SalesOrder?> FindByOrderCode(string orderCode);
        Task<SalesOrder?> FindByPaymentOrderCode(long paymentOrderCode);
        Task<List<SalesOrder>> FindByCustomerId(Guid customerId);
        Task<List<SalesOrder>> FindAll(string? status, Guid? customerId);
        Task Update(SalesOrder salesOrder);
    }
}
