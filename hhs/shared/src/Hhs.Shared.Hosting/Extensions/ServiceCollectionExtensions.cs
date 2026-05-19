using System.Reflection;
using System.Security.Cryptography;
using Hhs.Shared.Hosting.Exceptions;
using HsnSoft.Base;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.AspNetCore.Logging;
using HsnSoft.Base.AspNetCore.Mvc.Services;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.Settings;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Serilog.Loggers;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Hhs.Shared.Hosting.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddCommonAspNetCoreHosting(IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<HostingSettings>(configuration.GetSection("HostingSettings"));
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;
            });

            services.AddHttpContextAccessor();
            services.AddSingleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();

            services.AddSingleton<ILogMasker, LogMasker>();

            services.AddSingleton<IAppConsoleLogger, AppLogger>();
            services.AddSingleton<IFrameworkLogger, FrameworkLogger>();

            services.AddBaseAspNetCoreJsonLocalization();
            services.AddSingleton<IApiExceptionMapper, ApiExceptionMapper>();
            services.AddSingleton<IApiResponseWriter, ApiResponseWriter>();
            services.AddSingleton<IStatusMessageProvider, StatusMessageProvider>();

            services.AddExceptionHandler<GlobalApiExceptionHandler>();
            services.AddProblemDetails();
        }
        public IServiceCollection AddDefaultCorsSettings(string corsName)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(corsName, policy =>
                {
                    policy.SetIsOriginAllowed(isOriginAllowed: _ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }
        public IServiceCollection AddMvcRazorRender()
        {
            services.AddScoped<IRazorRenderService, RazorRenderService>();

            return services;
        }

        public IServiceCollection AddRequestResponseLogger()
        {
            services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
            services.AddSingleton<IRequestResponseLogger, RequestLogger>();

            return services;
        }

        public IServiceCollection AddJwtServerAuthentication(IConfiguration configuration, IWebHostEnvironment env, string audience)
        {
            string keyPath = Path.Combine(AppContext.BaseDirectory, "public_key.xml");

            if (!File.Exists(keyPath)) throw new BaseException("You need to provide public_key.xml to use auth");

            var rsa = RSA.Create();
            rsa.FromXmlString(File.ReadAllText(keyPath));

            var signingKey = new RsaSecurityKey(rsa);

            services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For disable Account/Login redirect when unauthorized identity jwt server
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = env.IsHostProduction() && Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                    options.SaveToken = false;
                    options.IncludeErrorDetails = !env.IsHostProduction();

                    options.Audience = audience; // Api audience

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true, // JWTs are required to have "aud" property set for Api audience
                        ValidAudience = audience,

                        ValidateIssuer = env.IsHostProduction(),
                        ValidIssuer = configuration["AuthServer:Authority"],

                        RequireExpirationTime = true, // JWTs are required to have "exp" property set
                        ValidateLifetime = true, // The "exp" will be validated
                        ClockSkew = TimeSpan.Zero,
                        RequireSignedTokens = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidTypes = ["JWT"]
                    };

                    // options.Events = new JwtBearerEvents
                    // {
                    //     OnTokenValidated = async context =>
                    //     {
                    //         var db = context.HttpContext.RequestServices.GetRequiredService<AuthServiceDbContext>();
                    //
                    //         var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    //         var securityStamp = context.Principal?.FindFirstValue("security_stamp");
                    //         var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                    //
                    //         if (!Guid.TryParse(userId, out var parsedUserId))
                    //         {
                    //             context.Fail("Invalid user id.");
                    //             return;
                    //         }
                    //
                    //         var user = await db.AuthUsers.FirstOrDefaultAsync(x => x.Id == parsedUserId);
                    //
                    //         if (user is null || !user.IsActive || user.SecurityStamp != securityStamp)
                    //         {
                    //             context.Fail("Invalid security stamp.");
                    //             return;
                    //         }
                    //
                    //         if (!string.IsNullOrWhiteSpace(jti))
                    //         {
                    //             bool revoked = await db.AuthTokenRevocations.AnyAsync(x => x.Jti == jti);
                    //
                    //             if (revoked)
                    //             {
                    //                 context.Fail("Token revoked.");
                    //                 return;
                    //             }
                    //         }
                    //     }
                    // };
                });

            return services;
        }

        public IServiceCollection AddCustomAuthorization(string[] servicePermissions)
        {
            services.AddBaseAuthorizationServiceCollection();

            services.AddAuthorization(options =>
            {
                foreach (string permissionPolicyName in servicePermissions)
                {
                    options.AddPolicy(permissionPolicyName, policyBuilder =>
                    {
                        // policyBuilder.RequireAuthenticatedUser();
                        // policyBuilder.RequireClaim("role");
                        policyBuilder.RequireUserPermission(permissionPolicyName);
                    });
                }
            });

            return services;
        }

        public IServiceCollection AddHostingRedis(IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(opt => { opt.Configuration = configuration["Redis:Configuration"] ?? throw new InvalidOperationException(); });
            services.AddSingleton<RedisLockService>();

            // services.Configure<BaseDistributedCacheOptions>(options =>
            // {
            //     options.KeyPrefix = "HsNsH:";
            // });

            // var dataProtectionBuilder = services.AddDataProtection().SetApplicationName("eShop");
            // var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            // dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "eShop-Protection-Keys");

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisConf = ConfigurationOptions.Parse(configuration["Redis:Configuration"] ?? throw new InvalidOperationException(), true);
                redisConf.ResolveDns = true;

                // if (redisConf.EndPoints.Count > 0 && env.EnvironmentName == "Local")
                // {
                //     redisConf.ServiceName = null;
                //     redisConf.EndPoints.Clear();
                //     redisConf.EndPoints.Add("host.docker.internal:6379"); //master
                //     redisConf.TieBreaker = "";
                // }

                return ConnectionMultiplexer.Connect(redisConf);
            });

            // var connectionString = Configuration["Redis:Configuration"];
            // var multiplexer = ConnectionMultiplexer.Connect(connectionString);
            // services.AddSingleton<IConnectionMultiplexer>(sp => multiplexer);

            services.AddSingleton(typeof(IRedisRepository<>), typeof(RedisRepository<>));
            services.AddSingleton<IRequestLimitStore, RedisRequestLimitStore>();

            return services;
        }

        public IServiceCollection AddEventBus(IConfiguration configuration, Assembly assembly)
        {
            services.AddRabbitMqEventBus(configuration);

            // Add All Event Handlers
            services.AddEventHandlers(assembly);

            return services;
        }

        private void AddRabbitMqEventBus(IConfiguration configuration)
        {
            // Configuration objects
            services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
            services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));

            // HttpContext & scoped services
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();

            // Singleton services
            services.AddSingleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
            services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
            services.AddSingleton<IEventBusLogger, EventBusLogger>();

            services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
            services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();

            // Singleton EventBusRabbitMq (uses scopes internally for scoped services)
            services.AddSingleton<IEventBus, EventBusRabbitMq>(sp => new EventBusRabbitMq(sp));
        }

        private void AddEventHandlers(Assembly assembly)
        {
            var refType = typeof(IIntegrationEventHandler);
            var types = assembly.GetTypes()
                .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });

            foreach (var type in types.ToList())
            {
                services.AddTransient(type);
            }
        }

        public IServiceCollection AddHostingHealthChecks(
            IConfiguration configuration,
            string serviceName,
            bool checkMongo = false,
            [CanBeNull] string mongoConnectionName = null,
            bool checkPostgresql = false,
            [CanBeNull] string postgresqlConnectionName = null,
            bool checkRedis = false,
            bool checkBroker = false)
        {
            return services.AddHostingHealthChecks<NoOpHealthCheck>(
                configuration,
                serviceName,
                checkMongo,
                mongoConnectionName,
                checkPostgresql,
                postgresqlConnectionName,
                checkRedis,
                checkBroker,
                addExtraHealthCheck: false);
        }

        public IServiceCollection AddHostingHealthChecks<TExtraHealthCheck>(
            IConfiguration configuration,
            string serviceName,
            bool checkMongo = false,
            [CanBeNull] string mongoConnectionName = null,
            bool checkPostgresql = false,
            [CanBeNull] string postgresqlConnectionName = null,
            bool checkRedis = false,
            bool checkBroker = false,
            bool addExtraHealthCheck = true)
            where TExtraHealthCheck : class, IHealthCheck
        {
            string healthCheckPrefix = serviceName ?? "service";
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self-check", () => HealthCheckResult.Healthy(description: "App is live"), tags: ["dependencies"]);

            // hcBuilder.AddUrlGroup
            // (
            //     new Uri(configuration["AuthServer:Authority"] ?? throw new InvalidOperationException()),
            //     name: $"{healtCheckPrefix}-auth-check",
            //     tags: new[] { "auth" }
            // );

            if (checkMongo)
            {
                hcBuilder.AddMongoDb(_ =>
                    {
                        var mongoUrl = MongoUrl.Create(configuration.GetConnectionString(mongoConnectionName ?? throw new ArgumentNullException(nameof(mongoConnectionName))) ?? throw new InvalidOperationException());
                        return new MongoClient(MongoClientSettings.FromConnectionString(mongoUrl.Url)).GetDatabase(mongoUrl.DatabaseName);
                    },
                    name: $"{healthCheckPrefix}-mongo-check",
                    tags: ["dependencies", "database"]
                );
                // hcBuilder.AddMongoDb(
                //     mongodbConnectionString: configuration.GetConnectionString(mongoConnectionName) ?? throw new InvalidOperationException(),
                //     name: $"{healtCheckPrefix}-mongo-check",
                //     tags: new[] { "dependencies", "database" }
                // );
            }

            if (checkPostgresql)
            {
                hcBuilder.AddNpgSql(
                    connectionString: configuration.GetConnectionString(postgresqlConnectionName ?? throw new ArgumentNullException(nameof(postgresqlConnectionName))) ?? throw new InvalidOperationException(),
                    name: $"{healthCheckPrefix}-postgresql-check",
                    tags: ["dependencies", "database"]
                );
            }

            if (checkRedis)
            {
                services.AddSingleton<CustomRedisHealthCheck>();
                hcBuilder.AddCheck<CustomRedisHealthCheck>(
                    name: $"{healthCheckPrefix}-redis-check",
                    tags: ["dependencies", "database"]);
            }

            if (checkBroker)
            {
                services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMQ:Connection"));
                services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqConnectionSettings>>().Value);
                services.AddSingleton<RabbitMqHealthCheck>();
                hcBuilder.AddCheck<RabbitMqHealthCheck>(
                    name: $"{healthCheckPrefix}-rabbitmq-check",
                    tags: ["dependencies", "broker"]);

                // var connectionSettings = serviceProvider.GetRequiredService<IOptions<KafkaConnectionSettings>>().Value;
                // var producerConfig = new ProducerConfig
                // {
                //     BootstrapServers = $"{connectionSettings.HostName}:{connectionSettings.Port}",
                // };
                // hcBuilder.AddKafka(producerConfig, name: $"{healtCheckPrefix}-kafka-check", tags: new[] { "dependencies" });
            }

            if (addExtraHealthCheck)
            {
                services.AddSingleton<TExtraHealthCheck>();
                hcBuilder.AddCheck<TExtraHealthCheck>(
                    name: $"{healthCheckPrefix}-extra-check",
                    tags: ["dependencies", "extra"]);
            }

            return services;
        }
    }
}

internal abstract class NoOpHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) => Task.FromResult(HealthCheckResult.Healthy());
}