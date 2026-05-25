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

        services.Configure<PayOSOptions>(options =>
            config.GetSection(PayOSOptions.SectionName).Bind(options));

        // Inject use cases
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IAuditLogUseCase, AuditLogUseCase>();
        services.AddScoped<IOrderUseCase, OrderUseCase>();
        services.AddScoped<IUserUseCase, UserUseCase>();
        services.AddScoped<ICustomerUseCase, CustomerUseCase>();
        services.AddScoped<IAlertUseCase, AlertUseCase>();
        services.AddScoped<IConfigUseCase, ConfigUseCase>();
        services.AddScoped<IControlDeviceUseCase, ControlDeviceUseCase>();
        services.AddScoped<IIncubatorUseCase, IncubatorUseCase>();
        services.AddScoped<IIncubatorModelUseCase, IncubatorModelUseCase>();
        services.AddScoped<IHatchingSeasonTemplateUseCase, HatchingSeasonTemplateUseCase>();
        services.AddScoped<IHatchingSeasonUseCase, HatchingSeasonUseCase>();
        services.AddScoped<IHatchingBatchUseCase, HatchingBatchUseCase>();
        services.AddScoped<ISensorUseCase, SensorUseCase>();
        services.AddScoped<ISensorReadingUseCase, SensorReadingUseCase>();
        services.AddScoped<IWarrantyUseCase, WarrantyUseCase>();
        services.AddScoped<IMaintenanceTicketUseCase, MaintenanceTicketUseCase>();

        // Inject repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ISalesOrderItemRepository, SalesOrderItemRepository>();
        services.AddScoped<IIncubatorRepository, IncubatorRepository>();
        services.AddScoped<IIncubatorModelRepository, IncubatorModelRepository>();
        services.AddScoped<IIncubatorModelConfigRepository, IncubatorModelConfigRepository>();
        services.AddScoped<IIncubatorConfigInstanceRepository, IncubatorConfigInstanceRepository>();
        services.AddScoped<IGuestOrderInfoRepository, GuestOrderInfoRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IControlDeviceRepository, ControlDeviceRepository>();
        services.AddScoped<IMasterboardRepository, MasterboardRepository>();
        services.AddScoped<IHatchingSeasonRepository, HatchingSeasonRepository>();
        services.AddScoped<IHatchingSeasonTemplateRepository, HatchingSeasonTemplateRepository>();
        services.AddScoped<IHatchingSeasonTemplateBatchRepository, HatchingSeasonTemplateBatchRepository>();
        services.AddScoped<IHatchingSeasonTemplateBatchConfigRepository, HatchingSeasonTemplateBatchConfigRepository>();
        services.AddScoped<IHatchingBatchRepository, HatchingBatchRepository>();
        services.AddScoped<IHatchingBatchConfigRepository, HatchingBatchConfigRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
        services.AddScoped<IWarrantyRepository, WarrantyRepository>();
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();

        services.AddScoped<IPaymentGatewayService, PayOSPaymentGatewayService>();

        // Inject utils
        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<ISMSService, SMSService>();
        services.AddHostedService<PayOSWebhookRegistrationHostedService>();

        return services;
    }
}
