using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data
{
    public class StartupDataConfiguration
    {
        public void DataConfigureServices(IServiceCollection services)
        {
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
            services.AddScoped<IDeviceInfoRepository, DeviceInfoRepository>();
            services.AddScoped<ITwilioParticipantRepository, TwilioParticipantRepository>();
        }
    }
}