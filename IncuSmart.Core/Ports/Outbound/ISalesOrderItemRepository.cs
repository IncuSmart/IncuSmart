namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISalesOrderItemRepository
    {
        Task AddRange(List<SalesOrderItem> items);
        Task<SalesOrderItem?> FindById(Guid id);
        Task<List<SalesOrderItem>> FindByOrderId(Guid orderId);
        Task Update(SalesOrderItem item);
    }
}
