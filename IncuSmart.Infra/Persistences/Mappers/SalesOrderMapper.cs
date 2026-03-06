namespace IncuSmart.Infra.Persistences.Mappers
{
    public class SalesOrderMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<SalesOrderEntity, SalesOrder>();
            config.NewConfig<SalesOrder, SalesOrderEntity>();
        }
    }
}
