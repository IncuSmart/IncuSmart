namespace IncuSmart.API.Mappers
{
    public class OrderMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<OrderItemRequest, OrderItemCommand>();
            config.NewConfig<CreateOrderByGuestRequest, CreateOrderByGuestCommand>();
            config.NewConfig<CreateOrderByCustomerRequest, CreateOrderByCustomerCommand>();
        }
    }
}
