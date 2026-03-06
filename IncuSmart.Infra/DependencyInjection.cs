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


        // Inject utils
        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<ISMSService, SMSService>();

        return services;
    }
}
