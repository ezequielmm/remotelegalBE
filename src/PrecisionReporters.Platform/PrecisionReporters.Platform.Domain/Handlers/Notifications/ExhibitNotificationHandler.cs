using Amazon.SimpleNotificationService.Util;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Parsers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class ExhibitNotificationHandler : HandlerBase<Message>, IExhibitNotificationHandler
    {
        private readonly IMapper<Document, Shared.Dtos.DocumentDto, object> _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IDocumentUserDepositionRepository _documentUserDepositionRepository;
        private readonly IPermissionService _permissionService;
        private readonly ISignalRDepositionManager _signalRDepositionManager;
        private readonly ITransactionHandler _transactionHandler;
        private readonly ILogger<ExhibitNotificationHandler> _logger;
        public ExhibitNotificationHandler(ILogger<ExhibitNotificationHandler> logger, IUserRepository userRepository, IMapper<Document, Shared.Dtos.DocumentDto, object> mapper, IDocumentRepository documentRepository, IDocumentUserDepositionRepository documentUserDepositionRepository, IDepositionRepository depositionRepository, IPermissionService permissionService, ISignalRDepositionManager signalRDepositionManager, ITransactionHandler transactionHandler)
        {
            _mapper = mapper;
            _documentRepository = documentRepository;
            _documentUserDepositionRepository = documentUserDepositionRepository;
            _depositionRepository = depositionRepository;
            _permissionService = permissionService;
            _signalRDepositionManager = signalRDepositionManager;
            _userRepository = userRepository;
            _transactionHandler = transactionHandler;
            _logger = logger;
        }
        public override async Task HandleRequest(Message message)
        {
            try
            {
                var exhibitNotification = SnsMessageParser.ParseExhibitNotification(message.MessageText);

                if (exhibitNotification != null)
                {
                    // Persist Exhibit data

                    var documentEntity = _mapper.ToModel(exhibitNotification.Context);
                    Document document = null;
                    User user = null;
                    var transactionResult = await _transactionHandler.RunAsync(async () =>
                    {
                        document = await _documentRepository.Create(documentEntity);
                        user = await _userRepository.GetById(documentEntity.AddedById);
                        var documentUserDeposition = new DocumentUserDeposition
                        {
                            DepositionId = exhibitNotification.Context.DepositionId,
                            DocumentId = document.Id,
                            UserId = document.AddedById
                        };
                        await _documentUserDepositionRepository.Create(documentUserDeposition);
                        await _permissionService.AddUserRole(document.AddedById, document.Id, ResourceType.Document, RoleName.DocumentOwner);
                    });

                    if (transactionResult.IsFailed)
                    {
                        throw new NotificationHandlerException("ExhibitNotificationHandler - Transaction Fail");
                    }

                    // Send Notification using SignalR
                    var notificationDto = new NotificationDto()
                    {
                        Action = NotificationAction.Create,
                        EntityType = NotificationEntity.Exhibit,
                        Content = document.Id
                    };
                    _logger.LogInformation($"Sending Notification - Email: {user.EmailAddress} - Document: {document.Id}");
                    await _signalRDepositionManager.SendDirectMessage(user.EmailAddress, notificationDto);
                }
                else if (successor != null)
                    await successor.HandleRequest(message);
            }
            catch (Exception ex)
            {
                var msg = $"ExhibitNotificationHanlder Fail Message: {message.MessageId}";
                throw new NotificationHandlerException(msg, ex);
            }
        }
    }
}
