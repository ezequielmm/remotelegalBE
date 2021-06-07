using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Domain;
using PrecisionReporters.Platform.Domain.AppConfigurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Hubs;
using PrecisionReporters.Platform.Transcript.Api.WebSockets;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddUserSecrets("9c1879dd-b940-4fbe-87e6-7e813a64acbb")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        readonly string AllowedOrigins = "_AllowedOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appConfiguration = Configuration.GetApplicationConfig();
            var allowedDomains = appConfiguration.CorsConfiguration.GetOrigingsAsArray();
            var allowedMethods = appConfiguration.CorsConfiguration.Methods;

            var data = new StartupDataConfiguration();
            data.DataConfigureServices(services);

            var domain = new StartupDomainConfiguration();
            domain.DomainConfigureServices(services, appConfiguration);

            // Services
            services.AddScoped<ITranscriptionService, TranscriptionService>();

            if (appConfiguration.CloudServicesConfiguration.TranscriptionProvider.Equals("GCP"))
            {
                services.AddTransient<ITranscriptionLiveService, TranscriptionLiveGCPService>();
            }
            else
            {
                services.AddTransient<ITranscriptionLiveService, TranscriptionLiveAzureService>();
            }

            services.AddScoped<ISignalRTranscriptionManager, SignalRTranscriptionManager>();

            // Websockets
            services.AddTransient<ITranscriptionsHandler, TranscriptionsHandler>();

            // Repositories
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySQL(appConfiguration.ConnectionStrings.MySqlConnection);
            });

            services.Configure<ConnectionStrings>(x =>
            {
                x.RedisConnectionString = appConfiguration.ConnectionStrings.RedisConnectionString;
            });

            // Enable Bearer token authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer("FullUserAuthenticationScheme", options =>
            {
                options.Audience = appConfiguration.CognitoConfiguration.ClientId;
                options.Authority = appConfiguration.CognitoConfiguration.Authority;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = GetTokenFromWebSocket
                };
            })
            .AddJwtBearer("GuestAuthenticationScheme", options =>
            {
                options.Authority = appConfiguration.CognitoConfiguration.Authority;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = GetTokenFromWebSocket
                };
            }
            );

            services.AddMvc()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddSignalR()
                .AddNewtonsoftJsonProtocol(opt => opt.PayloadSerializerSettings.Converters.Add(new StringEnumConverter()))
                .AddStackExchangeRedis(appConfiguration.ConnectionStrings.RedisConnectionString);

            services.AddCors(options =>
            {
                options.AddPolicy(name: AllowedOrigins,
                builder =>
                {
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains()
                    .WithOrigins(allowedDomains)
                    .AllowAnyHeader()
                    .WithMethods(allowedMethods)
                    .AllowCredentials();
                });
            });

            services.AddControllers();
            services.AddHttpContextAccessor();

            services.AddHealthChecks();

            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAppConfiguration appConfiguration,
            ApplicationDbContext db)
        {
            if (appConfiguration.ConfigurationFlags.IsDeveloperExceptionPageEnabled)
            {
                app.UseDeveloperExceptionPage();
            }

            //TODO: Add Error Handler Middleware

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(AllowedOrigins);

            app.Use(async (context, next) =>
            {
                var result = await context.AuthenticateAsync("FullUserAuthenticationScheme");
                if (!result.Succeeded)
                {
                    result = await context.AuthenticateAsync("GuestAuthenticationScheme");
                }
                context.User = result.Principal;

                await next();
            });

            app.UseAuthorization();
            app.UseHealthChecks("/healthcheck");
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TranscriptionHub>("/transcriptionHub");
            });

            app.UseResponseCompression();
        }

        private Task GetTokenFromWebSocket(MessageReceivedContext context)
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }

            // Trasncriptions WS sends it like this
            accessToken = context.Request.Query["token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
