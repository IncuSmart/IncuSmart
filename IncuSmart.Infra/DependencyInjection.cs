using IncuSmart.Core.Ports.Inbound;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"))
                   .UseSnakeCaseNamingConvention());

        JwtOptions.Init
            (config
                .GetSection("Jwt")
                .Get<JwtOptionsDto>()!
            );

        // Inject use cases
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IOrderUseCase, OrderUseCase>();
        services.AddScoped<IIncubatorUseCase, IncubatorUseCase>();
        services.AddScoped<IWarrantyUseCase, WarrantyUseCase>();
        services.AddScoped<IMaintenanceTicketUseCase, MaintenanceTicketUseCase>();
        services.AddScoped<IConfigUseCase, ConfigUseCase>();
        services.AddScoped<ISensorReadingUseCase, SensorReadingUseCase>();
        services.AddScoped<ISensorUseCase, SensorUseCase>();
        services.AddScoped<IHatchingSeasonTemplateUseCase, HatchingSeasonTemplateUseCase>();
        services.AddScoped<IHatchingSeasonUseCase, HatchingSeasonUseCase>();
        services.AddScoped<IHatchingBatchUseCase, HatchingBatchUseCase>();

        // Inject repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IIncubatorRepository, IncubatorRepository>();
        services.AddScoped<IIncubatorModelRepository, IncubatorModelRepository>();
        services.AddScoped<IIncubatorModelConfigRepository, IncubatorModelConfigRepository>();
        services.AddScoped<IIncubatorConfigInstanceRepository, IncubatorConfigInstanceRepository>();
        services.AddScoped<IGuestOrderInfoRepository, GuestOrderInfoRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();
        services.AddScoped<IIncubatorModelRepository, IncubatorModelRepository>();
        services.AddScoped<IIncubatorModelConfigRepository, IncubatorModelConfigRepository>();
        services.AddScoped<IConfigUseCase, ConfigUseCase>();
        services.AddScoped<IIncubatorModelUseCase, IncubatorModelUseCase>();
        services.AddScoped<IIncubatorRepository, IncubatorRepository>();
        services.AddScoped<IWarrantyRepository, WarrantyRepository>();
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();
        services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IMasterboardRepository, MasterboardRepository>();
        services.AddScoped<IHatchingSeasonRepository, HatchingSeasonRepository>();
        services.AddScoped<IHatchingBatchRepository, HatchingBatchRepository>();
        services.AddScoped<IHatchingBatchConfigRepository, HatchingBatchConfigRepository>();
        services.AddScoped<IHatchingSeasonTemplateRepository, HatchingSeasonTemplateRepository>();
        services.AddScoped<IHatchingSeasonTemplateBatchRepository, HatchingSeasonTemplateBatchRepository>();
        services.AddScoped<IHatchingSeasonTemplateBatchConfigRepository, HatchingSeasonTemplateBatchConfigRepository>();



        // Inject utils
        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<ISMSService, SMSService>();

        return services;
    }
}
