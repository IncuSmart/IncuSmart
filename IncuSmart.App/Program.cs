using IncuSmart.Infra.Persistences;
using IncuSmart.Core.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddInfrastructure(builder.Configuration);

// SignalR (real-time push to UI)
builder.Services.AddSignalR();

// Wire SignalR implementation of IDeviceNotifier (defined in Core, used by MqttBackgroundService in Infra)
builder.Services.AddSingleton<IncuSmart.Core.Ports.Outbound.IDeviceNotifier,
                               IncuSmart.API.Services.SignalRDeviceNotifier>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:RunCodeFirst"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<IncuSmart.API.Hubs.IncubatorHub>("/hubs/incubator");

app.Run();
