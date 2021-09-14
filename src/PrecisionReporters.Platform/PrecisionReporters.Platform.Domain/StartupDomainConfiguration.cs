using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Domain.AppConfigurations;
using PrecisionReporters.Platform.Domain.Authorization;
using PrecisionReporters.Platform.Domain.Authorization.Handlers;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Helpers;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Transcripts;
using PrecisionReporters.Platform.Domain.Transcripts.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using PrecisionReporters.Platform.Shared.Hubs;
using System;

namespace PrecisionReporters.Platform.Domain
{
    public class StartupDomainConfiguration
    {
        public IConfiguration Configuration { get; }

        public void DomainConfigureServices(IServiceCollection services, AppConfiguration appConfiguration)
        {
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

            services.Configure<VerificationLinkConfiguration>(x => { x.ExpirationTime = appConfiguration.VerificationLinkConfiguration.ExpirationTime; });
            services.Configure<DepositionConfiguration>(x => { x.CancelAllowedOffsetSeconds = appConfiguration.DepositionConfiguration.CancelAllowedOffsetSeconds; });
            services.Configure<DepositionConfiguration>(x => { x.MinimumReScheduleSeconds = appConfiguration.DepositionConfiguration.MinimumReScheduleSeconds; });

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
            services.AddSingleton<IMapper<Case, EditCaseDto, object>, EditCaseMapper>();
            services.AddSingleton<IMapper<UserSystemInfo, UserSystemInfoDto, object>, UserSystemInfoMapper>();
            services.AddSingleton<IMapper<DeviceInfo, DeviceInfoDto, object>, DeviceInfoMapper>();
            services.AddSingleton<IMapper<Participant, ParticipantTechStatusDto, object>, ParticipantTechStatusMapper>();
            services.AddSingleton<IMapper<Document, Shared.Dtos.DocumentDto, object>, ExhibitDocumentMapper>();
            services.AddSingleton<IMapper<AwsSessionInfo, AwsInfoDto, object>, AwsInfoMapper>();

            // Services            
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
                x.ClientTokenExpirationMinutes = appConfiguration.TwilioAccountConfiguration.ClientTokenExpirationMinutes;
                x.DeleteRecordingsEnabled = appConfiguration.TwilioAccountConfiguration.DeleteRecordingsEnabled;
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

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVerifyUserService, VerifyUserService>();
            services.AddScoped<IAwsStorageService, AwsStorageService>();

            services.AddScoped<IReminderService, ReminderService>().Configure<ReminderConfiguration>(x =>
            {
                x.MinutesBefore = appConfiguration.ReminderConfiguration.MinutesBefore;
                x.DailyExecution = appConfiguration.ReminderConfiguration.DailyExecution;
                x.ReminderRecurrency = appConfiguration.ReminderConfiguration.ReminderRecurrency;
            });

            services.AddTransient<IAwsEmailService, AwsEmailService>().Configure<EmailConfiguration>(x =>
            {
                x.Sender = appConfiguration.EmailConfiguration.Sender;
                x.EmailNotification = appConfiguration.EmailConfiguration.EmailNotification;
                x.ImagesUrl = appConfiguration.EmailConfiguration.ImagesUrl;
                x.LogoImageName = appConfiguration.EmailConfiguration.LogoImageName;
                x.CalendarImageName = appConfiguration.EmailConfiguration.CalendarImageName;
                x.PreDepositionLink = appConfiguration.EmailConfiguration.PreDepositionLink;
                x.JoinDepositionTemplate = appConfiguration.EmailConfiguration.JoinDepositionTemplate;
                x.SenderLabel = appConfiguration.EmailConfiguration.SenderLabel;
                x.XSesConfigurationSetHeader = appConfiguration.EmailConfiguration.XSesConfigurationSetHeader;
            });

            services.AddHostedService<BackgroundHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddScoped<IRoughTranscriptGenerator, GenerateRoughTranscriptPDF>();
            services.AddScoped<IRoughTranscriptGenerator, GenerateRoughTranscriptWord>();
            services.AddScoped<IRoughTranscriptHelper, RoughTranscriptHelper>();
            services.AddScoped<IDepositionEmailService, DepositionEmailService>();
            services.AddScoped<ICompositionHelper, CompositionHelper>();
            services.AddScoped<IFileHelper, FileHelper>();

            services.AddSingleton(typeof(IAmazonCognitoIdentityProvider),
                _ => new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(appConfiguration.CognitoConfiguration.AWSRegion)));
            services.AddSingleton(typeof(IAmazonSimpleEmailService),
                _ => new AmazonSimpleEmailServiceClient(RegionEndpoint.GetBySystemName(appConfiguration.EmailConfiguration.SesRegion)));

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

            services.AddScoped<IDatabaseTransactionProvider, ApplicationDbContextTransactionProvider>();
            services.AddScoped<ITransactionHandler, TransactionHandler>();
            services.AddScoped<IAwsSnsWrapper, AwsSnsWrapper>();
            services.AddScoped<ISnsHelper, SnsHelper>();

            if (!string.IsNullOrWhiteSpace(appConfiguration.DocumentConfiguration.PDFTronLicenseKey))
            {
                pdftron.PDFNet.Initialize(appConfiguration.DocumentConfiguration.PDFTronLicenseKey);
            }
            else
            {
                pdftron.PDFNet.Initialize();
            }
        }
    }
}
