namespace IncuSmart.Infra.Persistences.Mappers{    
    public class CustomerMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CustomerEntity, Customer>();
            config.NewConfig<Customer, CustomerEntity>();
        }
    }
}
