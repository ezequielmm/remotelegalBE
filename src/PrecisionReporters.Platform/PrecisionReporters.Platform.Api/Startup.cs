using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Amazon.CognitoIdentityProvider;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Api.AppConfigurations;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Amazon;
using PrecisionReporters.Platform.Api.Middlewares;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Handlers;
using PrecisionReporters.Platform.Api.Filters;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

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

            services.AddScoped<ITransferUtility>(sp =>
            {
                var appConfig = Configuration.GetApplicationConfig();

                var awsTwilioBucketCredentials = new BasicAWSCredentials(appConfig.TwilioAccountConfiguration.S3DestinationKey,
                    appConfig.TwilioAccountConfiguration.S3DestinationSecret);
                var s3Client = new AmazonS3Client(awsTwilioBucketCredentials);

                return new TransferUtility(s3Client);
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: AllowedOrigins,
                builder =>
                    {
                        builder.SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithOrigins(allowedDomains)
                        .AllowAnyHeader()
                        .WithMethods(allowedMethods);
                    });
            });

            services.AddControllers();
            services.AddHttpContextAccessor();

            // Register the Swagger generator, defining our Swagger documents
            services.AddSwaggerGen();

            services.AddHealthChecks();

            // Appsettings
            services.AddSingleton<IAppConfiguration>(appConfiguration);

            //Configurations
            services.AddOptions();
            services.Configure<UrlPathConfiguration>(x =>
            {
                x.FrontendBaseUrl = appConfiguration.UrlPathConfiguration.FrontendBaseUrl;
                x.VerifyUserUrl = appConfiguration.UrlPathConfiguration.VerifyUserUrl;
            });

            // Filters
            services.AddScoped<ValidateTwilioRequestFilterAttribute>();

            // Mappers
            services.AddSingleton<IMapper<Case, CaseDto, CreateCaseDto>, CaseMapper>();
            services.AddSingleton<IMapper<Room, RoomDto, CreateRoomDto>, RoomMapper>();
            services.AddSingleton<IMapper<User, UserDto, CreateUserDto>, UserMapper>();
            services.AddSingleton<IMapper<VerifyUser, object, CreateVerifyUserDto>, VerifyUserMapper>();
            services.AddSingleton<IMapper<Composition, CompositionDto, CallbackCompositionDto>, CompositionMapper>();

            // Services
            services.AddScoped<ICaseService, CaseService>();
            services.AddScoped<ITwilioService, TwilioService>().Configure<TwilioAccountConfiguration>(x =>
            {
                x.AccountSid = appConfiguration.TwilioAccountConfiguration.AccountSid;
                x.ApiKeySecret = appConfiguration.TwilioAccountConfiguration.ApiKeySecret;
                x.ApiKeySid = appConfiguration.TwilioAccountConfiguration.ApiKeySid;
                x.AuthToken = appConfiguration.TwilioAccountConfiguration.AuthToken;
                x.S3DestinationBucket = appConfiguration.TwilioAccountConfiguration.S3DestinationBucket;
                x.S3DestinationKey = appConfiguration.TwilioAccountConfiguration.S3DestinationKey;
                x.S3DestinationSecret = appConfiguration.TwilioAccountConfiguration.S3DestinationSecret;
                x.StatusCallbackUrl = appConfiguration.TwilioAccountConfiguration.StatusCallbackUrl;
            });

            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<ICognitoService, CognitoService>().Configure<CognitoConfiguration>(x =>
            {
                x.Authority = appConfiguration.CognitoConfiguration.Authority;
                x.AWSAccessKey = appConfiguration.CognitoConfiguration.AWSAccessKey;
                x.AWSRegion = appConfiguration.CognitoConfiguration.AWSRegion;
                x.AWSSecretAccessKey = appConfiguration.CognitoConfiguration.AWSSecretAccessKey;
                x.ClientId = appConfiguration.CognitoConfiguration.ClientId;
                x.UserPoolId = appConfiguration.CognitoConfiguration.UserPoolId;
            });

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVerifyUserService, VerifyUserService>();
            services.AddTransient<IAwsEmailService, AwsEmailService>().Configure<EmailConfiguration>(x =>
            {
                x.Sender = appConfiguration.EmailConfiguration.Sender;
            });

            services.AddSingleton(typeof(IAmazonCognitoIdentityProvider),
                _ => new AmazonCognitoIdentityProviderClient(appConfiguration.CognitoConfiguration.AWSAccessKey, appConfiguration.CognitoConfiguration.AWSSecretAccessKey, RegionEndpoint.USEast1));
            services.AddSingleton(typeof(IAmazonSimpleEmailService),
                _ => new AmazonSimpleEmailServiceClient(appConfiguration.CognitoConfiguration.AWSAccessKey, appConfiguration.CognitoConfiguration.AWSSecretAccessKey, RegionEndpoint.USEast1));

            services.AddScoped<ICompositionService, CompositionService>();

            // Repositories
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IVerifyUserRepository, VerifyUserRepository>();
            services.AddScoped<ICompositionRepository, CompositionRepository>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySQL(appConfiguration.ConnectionStrings.MySqlConnection));

            services.AddScoped<ITransactionHandler, TransactionHandler>();
            services.AddScoped<ITransactionInfo, TransactionInfo>();

            // Enable Bearer token authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Audience = appConfiguration.CognitoConfiguration.ClientId;
                options.Authority = appConfiguration.CognitoConfiguration.Authority;
            });

            services.AddMvc()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAppConfiguration appConfiguration)
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHealthChecks("/healthcheck");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
