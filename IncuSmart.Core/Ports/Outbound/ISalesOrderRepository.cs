namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISalesOrderRepository
    {
        Task Add(SalesOrder salesOrder);
    }
}
