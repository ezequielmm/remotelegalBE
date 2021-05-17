using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Api.AppConfigurations;
using PrecisionReporters.Platform.Api.Authorization;
using PrecisionReporters.Platform.Api.Authorization.Handlers;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Api.Hubs;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Api.Middlewares;
using PrecisionReporters.Platform.Api.WebSockets;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Domain.Transcripts.Interfaces;
using PrecisionReporters.Platform.Domain.Transcripts;

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
                var config = new AmazonS3Config
                {
                    Timeout = TimeSpan.FromMinutes(15),
                    RetryMode = RequestRetryMode.Standard,
                    MaxErrorRetry = 3,
                    RegionEndpoint = RegionEndpoint.GetBySystemName(appConfiguration.AwsStorageConfiguration.S3BucketRegion)
                };

                var s3Client = new AmazonS3Client(config);

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
                        .WithMethods(allowedMethods)
                        .AllowCredentials();
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
                x.ForgotPasswordUrl = appConfiguration.UrlPathConfiguration.ForgotPasswordUrl;
            });
            services.Configure<KestrelServerOptions>(options =>
            {
                // TODO: Check how to return a valid error message with this validation and reduce the value to MaxFileSize only
                options.Limits.MaxRequestBodySize = appConfiguration.DocumentConfiguration.MaxRequestBodySize;
            });
            services.Configure<VerificationLinkConfiguration>(x => { x.ExpirationTime = appConfiguration.VerificationLinkConfiguration.ExpirationTime; });
            services.Configure<DepositionConfiguration>(x => { x.CancelAllowedOffsetSeconds = appConfiguration.DepositionConfiguration.CancelAllowedOffsetSeconds; });
            services.Configure<DepositionConfiguration>(x => { x.MinimumReScheduleSeconds = appConfiguration.DepositionConfiguration.MinimumReScheduleSeconds; });
            // Filters
            services.AddScoped<ValidateTwilioRequestFilterAttribute>();

            // Authorization
            services.AddScoped<IAuthorizationHandler, UserAuthorizeHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddTransient<IAuthorizationHandler, HubAuthorizeHandler>();

            // Mappers
            services.AddSingleton<IMapper<Case, CaseDto, CreateCaseDto>, CaseMapper>();
            services.AddSingleton<IMapper<Room, RoomDto, CreateRoomDto>, RoomMapper>();
            services.AddSingleton<IMapper<User, UserDto, CreateUserDto>, UserMapper>();
            services.AddSingleton<IMapper<VerifyUser, object, CreateVerifyUserDto>, VerifyUserMapper>();
            services.AddSingleton<IMapper<Composition, CompositionDto, CallbackCompositionDto>, CompositionMapper>();
            services.AddSingleton<IMapper<Deposition, DepositionDto, CreateDepositionDto>, DepositionMapper>();
            services.AddSingleton<IMapper<Document, DocumentDto, CreateDocumentDto>, DocumentMapper>();
            services.AddSingleton<IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto>, DepositionDocumentMapper>();
            services.AddSingleton<IMapper<Participant, ParticipantDto, CreateParticipantDto>, ParticipantMapper>();
            services.AddSingleton<IMapper<Member, MemberDto, CreateMemberDto>, MemberMapper>();
            services.AddSingleton<IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto>, DepositionEventMapper>();
            services.AddSingleton<IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto>, AnnotationEventMapper>();
            services.AddSingleton<IMapper<Transcription, TranscriptionDto, object>, TranscriptionMapper>();
            services.AddSingleton<IMapper<BreakRoom, BreakRoomDto, object>, BreakRoomMapper>();
            services.AddSingleton<IMapper<Participant, AddParticipantDto, CreateGuestDto>, GuestParticipantMapper>();
            services.AddSingleton<IMapper<Document, DocumentWithSignedUrlDto, object>, DocumentWithSignedUrlMapper>();
            services.AddSingleton<IUserIdProvider, UserIdProvider>();
            services.AddSingleton<IMapper<Participant, EditParticipantDto, object>, EditParticipantMapper>();
            // Websockets
            services.AddTransient<ITranscriptionsHandler, TranscriptionsHandler>();

            // Services
            services.AddScoped<ICaseService, CaseService>();
            services.AddScoped<ITwilioService, TwilioService>().Configure<TwilioAccountConfiguration>(x =>
            {
                x.AccountSid = appConfiguration.TwilioAccountConfiguration.AccountSid;
                x.ApiKeySecret = appConfiguration.TwilioAccountConfiguration.ApiKeySecret;
                x.ApiKeySid = appConfiguration.TwilioAccountConfiguration.ApiKeySid;
                x.AuthToken = appConfiguration.TwilioAccountConfiguration.AuthToken;
                x.S3DestinationBucket = appConfiguration.TwilioAccountConfiguration.S3DestinationBucket;
                x.StatusCallbackUrl = appConfiguration.TwilioAccountConfiguration.StatusCallbackUrl;
                x.ConversationServiceId = appConfiguration.TwilioAccountConfiguration.ConversationServiceId;
                x.TwilioStartedDateReference = appConfiguration.TwilioAccountConfiguration.TwilioStartedDateReference;
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
                x.GuestUsersGroup = appConfiguration.CognitoConfiguration.GuestUsersGroup;
                x.GuestUsersPass = appConfiguration.CognitoConfiguration.GuestUsersPass;
                x.GuestClientId = appConfiguration.CognitoConfiguration.GuestClientId;
            });

            services.AddScoped<ITranscriptionService, TranscriptionService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVerifyUserService, VerifyUserService>();
            services.AddScoped<IAwsStorageService, AwsStorageService>();
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
            });
            services.AddScoped<IDepositionService, DepositionService>();
            services.AddScoped<IAnnotationEventService, AnnotationEventService>();
            services.AddTransient<IAwsEmailService, AwsEmailService>().Configure<EmailConfiguration>(x =>
            {
                x.Sender = appConfiguration.EmailConfiguration.Sender;
                x.EmailNotification = appConfiguration.EmailConfiguration.EmailNotification;
                x.ImagesUrl = appConfiguration.EmailConfiguration.ImagesUrl;
                x.LogoImageName = appConfiguration.EmailConfiguration.LogoImageName;
                x.CalendarImageName = appConfiguration.EmailConfiguration.CalendarImageName;
                x.PreDepositionLink = appConfiguration.EmailConfiguration.PreDepositionLink;
                x.JoinDepositionTemplate = appConfiguration.EmailConfiguration.JoinDepositionTemplate;
            });

            services.AddSingleton(typeof(IAmazonCognitoIdentityProvider),
                _ => new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(appConfiguration.CognitoConfiguration.AWSRegion)));
            services.AddSingleton(typeof(IAmazonSimpleEmailService),
                _ => new AmazonSimpleEmailServiceClient(RegionEndpoint.GetBySystemName(appConfiguration.EmailConfiguration.SesRegion)));

            services.AddScoped<ICompositionService, CompositionService>();
            services.AddScoped<ITwilioCallbackService, TwilioCallbackService>();
            services.AddScoped<IBreakRoomService, BreakRoomService>();
            services.AddScoped<IDepositionDocumentService, DepositionDocumentService>();
            services.AddScoped<IActivityHistoryService, ActivityHistoryService>();
            if (appConfiguration.CloudServicesConfiguration.TranscriptionProvider.Equals("GCP"))
            {
                services.AddTransient<ITranscriptionLiveService, TranscriptionLiveGCPService>();
            }
            else
            {
                services.AddTransient<ITranscriptionLiveService, TranscriptionLiveAzureService>();
            }

            services.AddScoped<IDraftTranscriptGeneratorService, DraftTranscriptGeneratorService>();
            services.AddHostedService<BackgroundHostedService>();
            services.AddScoped<IParticipantService, ParticipantService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddScoped<ISignalRNotificationManager, SignalRNotificationManager>();
            services.AddScoped<IRoughTranscriptGenerator, GenerateRoughTranscriptPDF>();
            services.AddScoped<IRoughTranscriptGenerator, GenerateRoughTranscriptWord>();
            services.AddScoped<IRoughTranscriptHelper, RoughTranscriptHelper>();
            services.AddScoped<IDepositionEmailService, DepositionEmailService>();
            services.Configure<GcpConfiguration>(x =>
            {
                x.type = appConfiguration.GcpConfiguration.type;
                x.project_id = appConfiguration.GcpConfiguration.project_id;
                x.private_key_id = appConfiguration.GcpConfiguration.private_key_id;
                x.private_key = appConfiguration.GcpConfiguration.private_key;
                x.client_email = appConfiguration.GcpConfiguration.client_email;
                x.client_id = appConfiguration.GcpConfiguration.client_id;
                x.auth_uri = appConfiguration.GcpConfiguration.auth_uri;
                x.token_uri = appConfiguration.GcpConfiguration.token_uri;
                x.auth_provider_x509_cert_url = appConfiguration.GcpConfiguration.auth_provider_x509_cert_url;
                x.client_x509_cert_url = appConfiguration.GcpConfiguration.client_x509_cert_url;
            });

            services.Configure<AzureCognitiveServiceConfiguration>(x =>
            {
                x.SubscriptionKey = appConfiguration.AzureCognitiveServiceConfiguration.SubscriptionKey;
                x.RegionCode = appConfiguration.AzureCognitiveServiceConfiguration.RegionCode;
            });


            // Repositories
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IVerifyUserRepository, VerifyUserRepository>();
            services.AddScoped<ICompositionRepository, CompositionRepository>();
            services.AddScoped<IDepositionRepository, DepositionRepository>();
            services.AddScoped<IParticipantRepository, ParticipantRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDepositionEventRepository, DepositionEventRepository>();
            services.AddScoped<IUserResourceRoleRepository, UserResourceRoleRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IDocumentUserDepositionRepository, DocumentUserDepositionRepository>();
            services.AddScoped<IAnnotationEventRepository, AnnotationEventRepository>();
            services.AddTransient<ITranscriptionRepository, TranscriptionRepository>();
            services.AddScoped<IBreakRoomRepository, BreakRoomRepository>();
            services.AddScoped<IDepositionDocumentRepository, DepositionDocumentRepository>();
            services.AddScoped<IActivityHistoryRepository, ActivityHistoryRepository>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySQL(appConfiguration.ConnectionStrings.MySqlConnection);
            });

            services.Configure<ConnectionStrings>(x =>
            {
                x.RedisConnectionString = appConfiguration.ConnectionStrings.RedisConnectionString;
            });

            services.AddScoped<IDatabaseTransactionProvider, ApplicationDbContextTransactionProvider>();
            services.AddScoped<ITransactionHandler, TransactionHandler>();

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

            if (!string.IsNullOrWhiteSpace(appConfiguration.DocumentConfiguration.PDFTronLicenseKey))
            {
                pdftron.PDFNet.Initialize(appConfiguration.DocumentConfiguration.PDFTronLicenseKey);
            }
            else
            {
                pdftron.PDFNet.Initialize();
            }
        }

        private Task GetTokenFromWebSocket(MessageReceivedContext context)
        {
            // Trasncriptions WS sends it like this
            var accessToken = context.Request.Query["token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            // SignalR WS sends it like this
            accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
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

            db.Database.Migrate();
        }
    }
}
