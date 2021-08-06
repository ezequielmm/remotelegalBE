using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Api.Hubs;
using PrecisionReporters.Platform.Api.Middlewares;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Domain;
using PrecisionReporters.Platform.Domain.AppConfigurations;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api
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

            // Filters
            services.AddScoped<ValidateTwilioRequestFilterAttribute>();

            // Services
            services.AddScoped<ICaseService, CaseService>();
            services.AddScoped<IBreakRoomService, BreakRoomService>();
            services.AddScoped<ISignalRDepositionManager, SignalRDepositionManager>();
            services.AddScoped<IDepositionService, DepositionService>();
            services.AddScoped<ITranscriptionService, TranscriptionService>();
            services.AddScoped<IAnnotationEventService, AnnotationEventService>();
            services.AddScoped<ICompositionService, CompositionService>();
            services.AddScoped<ITwilioCallbackService, TwilioCallbackService>();
            services.AddScoped<IDepositionDocumentService, DepositionDocumentService>();
            services.AddScoped<IActivityHistoryService, ActivityHistoryService>();
            services.AddScoped<IDraftTranscriptGeneratorService, DraftTranscriptGeneratorService>();
            services.AddScoped<IParticipantService, ParticipantService>();
            services.AddScoped<IDocumentService, DocumentService>().Configure<DocumentConfiguration>(x =>
            {
                x.BucketName = appConfiguration.DocumentConfiguration.BucketName;
                x.AcceptedFileExtensions = appConfiguration.DocumentConfiguration.AcceptedFileExtensions;
                x.AcceptedTranscriptionExtensions = appConfiguration.DocumentConfiguration.AcceptedTranscriptionExtensions;
                x.MaxFileSize = appConfiguration.DocumentConfiguration.MaxFileSize;
                x.PreSignedUrlValidHours = appConfiguration.DocumentConfiguration.PreSignedUrlValidHours;
                x.PostDepoVideoBucket = appConfiguration.DocumentConfiguration.PostDepoVideoBucket;
                x.EnvironmentFilesBucket = appConfiguration.DocumentConfiguration.EnvironmentFilesBucket;
                x.FrontEndContentBucket = appConfiguration.DocumentConfiguration.FrontEndContentBucket;
                x.CloudfrontPrivateKey = appConfiguration.DocumentConfiguration.CloudfrontPrivateKey;
                x.CloudfrontXmlKey = appConfiguration.DocumentConfiguration.CloudfrontXmlKey;
                x.CloudfrontPolicyStatement = appConfiguration.DocumentConfiguration.CloudfrontPolicyStatement;
            });
            services.AddScoped<IAwsSnsWrapper, AwsSnsWrapper>();
            services.AddScoped<ISnsHelper, SnsHelper>();
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();
            services.Configure<KestrelServerOptions>(options =>
            {
                // TODO: Check how to return a valid error message with this validation and reduce the value to MaxFileSize only
                options.Limits.MaxRequestBodySize = appConfiguration.DocumentConfiguration.MaxRequestBodySize;
            });

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

            // Register the Swagger generator, defining our Swagger documents
            services.AddSwaggerGen();

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

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            app.UseMiddleware<ErrorHandlingMiddleware>(appConfiguration.ConfigurationFlags.IsShowErrorMessageEnabled);

            // Enable middleware to serve swagger-ui, specifying the Swagger JSON endpoint.
            if (appConfiguration.ConfigurationFlags.IsSwaggerUiEnabled)
            {
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint(appConfiguration.Swagger.Url, appConfiguration.Swagger.Name);
                });
            }

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
                endpoints.MapHub<DepositionHub>("/depositionHub");
            });

            app.UseResponseCompression();

            db.Database.Migrate();
        }

        private Task GetTokenFromWebSocket(MessageReceivedContext context)
        {
            // SignalR WS sends it like this
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
