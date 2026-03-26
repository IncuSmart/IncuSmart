namespace IncuSmart.API.Mappers
{
    public class WarrantyMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateWarrantyRequest, CreateWarrantyCommand>();
            config.NewConfig<CreateMaintenanceTicketRequest, CreateMaintenanceTicketCommand>();
            config.NewConfig<UpdateMaintenanceTicketStatusRequest, UpdateMaintenanceTicketStatusCommand>();
            config.NewConfig<CreateMaintenanceLogRequest, CreateMaintenanceLogCommand>();
        }
    }
}
