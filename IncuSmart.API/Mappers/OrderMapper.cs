namespace IncuSmart.API.Mappers
{
    public class OrderMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<OrderItemRequest, OrderItemCommand>();
            config.NewConfig<CreateOrderByGuestRequest, CreateOrderByGuestCommand>()
                .Map(dest => dest.ShippingAddress, src => src.Address);
            config.NewConfig<CreateOrderByCustomerRequest, CreateOrderByCustomerCommand>();
            config.NewConfig<AssignIncubatorToOrderItemRequest, AssignIncubatorToOrderItemCommand>();
            config.NewConfig<ClaimGuestOrderRequest, ClaimGuestOrderCommand>();
        }
    }
}
