using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Core.Usecases;
using IncuSmart.Core.Utils;
using IncuSmart.Infra.Persistences.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Repositories
builder.Services.AddScoped<IIncubatorRepository, IncubatorRepository>();
builder.Services.AddScoped<IWarrantyRepository, WarrantyRepository>();
builder.Services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
builder.Services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();


// Use cases
builder.Services.AddScoped<IIncubatorUseCase, IncubatorUseCase>();
builder.Services.AddScoped<IWarrantyUseCase, WarrantyUseCase>();
builder.Services.AddScoped<IMaintenanceTicketUseCase, MaintenanceTicketUseCase>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }
);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token without 'Bearer ' prefix"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var cfg = builder.Configuration.GetSection("Redis");

    var host = cfg["Host"] ?? throw new Exception("Redis:Host is missing");
    var password = cfg["Password"] ?? throw new Exception("Redis:Password is missing");
    var port = cfg.GetValue<int?>("Port") ?? 11576;

    var opt = new ConfigurationOptions
    {
        EndPoints = { { host, port } },
        Password = password,
        Ssl = false, 
        AbortOnConnectFail = false,
        ConnectTimeout = 10_000,
        SyncTimeout = 10_000,
        IncludeDetailInExceptions = true,
    };

    var mux = ConnectionMultiplexer.Connect(opt);
    mux.ConnectionFailed += (_, e) => Console.WriteLine($"[Redis] {e.FailureType}: {e.Exception?.Message}");
    mux.ConnectionRestored += (_, e) => Console.WriteLine($"[Redis] Restored");
    return mux;
});

builder.Services.Configure<SMSProperties>(
    builder.Configuration.GetSection(SMSProperties.SectionName));


builder.Services.Configure<RedisOptions>(
    builder.Configuration.GetSection(RedisOptions.SectionName));

builder.Services.AddHttpClient();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")
            )
        };
    });


builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
