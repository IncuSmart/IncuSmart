using IncuSmart.Infra.Persistences.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; } = null!;
        public DbSet<CustomerEntity> Customers { get; set; } = null!;
        public DbSet<SalesOrderEntity> SalesOrders { get; set; } = null!;
        public DbSet<IncubatorEntity> Incubators { get; set; } = null!;
        public DbSet<IncubatorModelEntity> IncubatorModels { get; set; } = null!;
        public DbSet<IncubatorModelConfigEntity> IncubatorModelConfigs { get; set; } = null!;
        public DbSet<IncubatorConfigInstanceEntity> IncubatorConfigInstances { get; set; } = null!;
        public DbSet<GuestOrderInfoEntity> GuestOrderInfos { get; set; } = null!;

        public DbSet<ControlDeviceEntity> ControlDevices { get; set; } = null!;
        public DbSet<MasterboardEntity> Masterboards { get; set; } = null!;
        public DbSet<ControlBoardTypeEntity> ControlBoardTypes { get; set; } = null!;

        public DbSet<ConfigEntity> Configs { get; set; } = null!;
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<Enum>()
                .HaveConversion<string>();
        }
    }
}
