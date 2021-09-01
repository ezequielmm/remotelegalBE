using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using pdftron.PDF;
using pdftron.SDF;
using PrecisionReporters.Platform.Shared.Dtos;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UploadExhibitLambda.Wrappers;
using UploadExhibitLambda.Wrappers.Interface;
using static PrecisionReporters.Platform.Shared.Commons.ApplicationConstants;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace UploadExhibitLambda
{
    public class UploadExhibitFunction
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IAmazonSecretsManager _secretsManagerClient;
        private readonly IMetadataWrapper _metadataWrapper;

        /// <summary>
        /// Default constructor used by AWS Lambda to construct the function. Credentials and Region information will
        /// be set by the running Lambda environment.
        /// </summary>
        public UploadExhibitFunction()
        {
            _s3Client = new AmazonS3Client();
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _secretsManagerClient = new AmazonSecretsManagerClient();
            _metadataWrapper = new MetadataWrapper();
        }

        /// <summary>
        /// Constructor used for testing which will pass in the already configured service clients.
        /// </summary>
        /// <param name="s3Client"></param>
        /// <param name="SnsClient"></param>
        /// <param name="secretsManagerClient"></param>
        public UploadExhibitFunction(IAmazonS3 s3Client, IAmazonSimpleNotificationService SnsClient, IAmazonSecretsManager secretsManagerClient, IMetadataWrapper metadataWrapper)
        {
            _s3Client = s3Client;
            _snsClient = SnsClient;
            _secretsManagerClient = secretsManagerClient;
            _metadataWrapper = metadataWrapper;
        }

        /// <summary>
        /// This function will take a file from S3 and convert it to PDF if
        /// possible, otherwise it will return it to another S3 bucket.
        /// At the end of the process, a notification will be published for subscribers through SNS.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> UploadExhibit(S3Event input, ILambdaContext context)
        {
            var cancellationTokenSource = new CancellationTokenSource(context.RemainingTime);
            var cancellationToken = cancellationTokenSource.Token;
            if (input.Records == null || !input.Records.Any())
            {
                context.Logger.LogLine("Event does not have a valid S3 structure");
                context.Logger.LogLine(await SendNotification(new NotificationErrorDto
                {
                    NotificationType = UploadExhibitsNotificationTypes.InvalidS3Structure,
                    Context = new ErrorDocumentDto()
                    {
                        Error = "Event does not have a valid S3 structure"
                    }
                }, cancellationToken));

                return false;
            }

            try
            {
                pdftron.PDFNet.Initialize(await GetSecret(UploadExhibitConstants.PdfTronKey, cancellationToken));
                foreach (var record in input.Records)
                {
                    context.Logger.LogLine(CleanUpTmpFolder());
                    context.Logger.LogLine($"Initializing process for bucket name: {record.S3.Bucket.Name} and Key: {record.S3.Object.Key}");
                    var objectMetadata = await _s3Client.GetObjectMetadataAsync(record.S3.Bucket.Name, record.S3.Object.Key, cancellationToken);
                    var userId = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, UserIdExhibitsMetadata));
                    var depositionId = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, DepositionIdExhibitsMetadata));
                    var caseId = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, CaseIdExhibitsMetadata));
                    var documentType = _metadataWrapper.GetMetadataByKey(objectMetadata, DocumentTypeExhibitsMetadata);
                    var displayName = _metadataWrapper.GetMetadataByKey(objectMetadata, DisplayNameExhibitsMetadata);

                    var extension = Path.GetExtension(displayName)?.ToLower();
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var s3FilePath = $"files/{caseId}/{depositionId}/{documentType}/{fileName}";

                    var file = await _s3Client.GetObjectStreamAsync(record.S3.Bucket.Name, record.S3.Object.Key, null, cancellationToken);

                    var objectSize = record.S3.Object.Size;
                    if (objectSize > long.Parse(Environment.GetEnvironmentVariable(UploadExhibitConstants.MaxFileSize)!))
                    {
                        context.Logger.LogLine($"Fail Upload: File size exceeds the allowed limit. File of deposition {depositionId} from user {userId} extension file {extension} document type {documentType}");
                        context.Logger.LogLine(await SendNotification(new NotificationErrorDto
                        {
                            NotificationType = UploadExhibitsNotificationTypes.ExceededSize,
                            Context = new ErrorDocumentDto()
                            {
                                Error = string.Empty,
                                Document = new DocumentDto
                                {
                                    AddedBy = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, UserIdExhibitsMetadata)),
                                    DepositionId = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, DepositionIdExhibitsMetadata)),
                                    DisplayName = _metadataWrapper.GetMetadataByKey(objectMetadata, DisplayNameExhibitsMetadata),
                                    DocumentType = _metadataWrapper.GetMetadataByKey(objectMetadata, DocumentTypeExhibitsMetadata),
                                    FilePath = input.Records[0].S3.Object.Key
                                }
                            }
                        },
                            cancellationToken));
                        continue;
                    }

                    context.Logger.LogLine($"Uploading file of deposition {depositionId} from user {userId} extension file {extension} document type {documentType}");
                    if (!UploadExhibitConstants.SkipPdfConversionExtensions.Contains(extension))
                    {
                        context.Logger.LogLine($"File of deposition {depositionId} requires PDF optimization as it is of extension {extension}");
                        var pathPdf = await FileToPdf(file, extension, cancellationToken);
                        var pathOptimizedPDF = OptimizePdf(pathPdf);
                        await _s3Client.UploadObjectFromFilePathAsync(Environment.GetEnvironmentVariable(UploadExhibitConstants.BucketName), s3FilePath, pathOptimizedPDF, null, cancellationToken);
                    }
                    else
                    {
                        context.Logger.LogLine($"File of deposition {depositionId} skipped PDF optimization as it is of extension {extension}");
                        await _s3Client.UploadObjectFromStreamAsync(Environment.GetEnvironmentVariable(UploadExhibitConstants.BucketName), s3FilePath, file, null, cancellationToken);
                    }

                    var content = new ExhibitNotificationDto
                    {
                        NotificationType = UploadExhibitsNotificationTypes.ExhibitUploaded,
                        Context = new DocumentDto
                        {
                            DepositionId = depositionId,
                            Type = extension,
                            AddedBy = userId,
                            Name = fileName,
                            DisplayName = displayName,
                            FilePath = s3FilePath,
                            Size = record.S3.Object.Size,
                            DocumentType = documentType,
                            CreationDate = DateTime.UtcNow
                        }
                    };
                    context.Logger.LogLine(await SendNotification(content, cancellationToken));
                }

                return true;
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"UploadExhibit Exception thrown during AWS Lambda function runtime: {e}");
                var objectMetadata = await _s3Client.GetObjectMetadataAsync(input.Records[0].S3.Bucket.Name, input.Records[0].S3.Object.Key, cancellationToken);
                context.Logger.LogLine(await SendNotification(new NotificationErrorDto
                {
                    NotificationType = UploadExhibitsNotificationTypes.ExceptionInLambda,
                    Context = new ErrorDocumentDto()
                    {
                        Error = e.ToString(),
                        Document = new DocumentDto
                        {
                            AddedBy = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, UserIdExhibitsMetadata)),
                            DepositionId = Guid.Parse(_metadataWrapper.GetMetadataByKey(objectMetadata, DepositionIdExhibitsMetadata)),
                            DisplayName = _metadataWrapper.GetMetadataByKey(objectMetadata, DisplayNameExhibitsMetadata),
                            DocumentType = _metadataWrapper.GetMetadataByKey(objectMetadata, DocumentTypeExhibitsMetadata),
                            FilePath = input.Records[0].S3.Object.Key
                        }
                    }
                }, cancellationToken));
            }
            return false;
        }

        private async Task<string> FileToPdf(Stream file, string extension, CancellationToken cancellationToken)
        {
            var fileName = Path.Combine(UploadExhibitConstants.TmpFolder, $"fileToConvert_{Guid.NewGuid()}{extension}");

            await using (var fileStream = File.Create(fileName))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            using var doc = new PDFDoc();
            if (UploadExhibitConstants.OfficeDocumentExtensions.Contains(extension))
            {
                pdftron.PDF.Convert.OfficeToPDF(doc, fileName, null);
            }
            else
            {
                pdftron.PDF.Convert.ToPdf(doc, fileName);
            }

            var filePath = Path.ChangeExtension(fileName, UploadExhibitConstants.PdfExtension);
            doc.Save(filePath, SDFDoc.SaveOptions.e_remove_unused);

            return filePath;
        }

        private string OptimizePdf(string path)
        {
            var type = Path.GetExtension(path);
            var pathOptimizedFile = Path.Combine(UploadExhibitConstants.TmpFolder, $"optimizedPDF_{Guid.NewGuid()}{UploadExhibitConstants.PdfExtension}");

            if (!string.Equals(type, UploadExhibitConstants.PdfExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                return path;
            }

            using var doc = new PDFDoc(path);
            doc.Save(pathOptimizedFile, SDFDoc.SaveOptions.e_linearized);

            return pathOptimizedFile;
        }

        private async Task<string> SendNotification<T>(T message, CancellationToken cancellationToken)
        {
            var request = new PublishRequest
            {
                TargetArn = Environment.GetEnvironmentVariable(UploadExhibitConstants.SnsTopicArn),
                Message = JsonSerializer.Serialize(message)
            };
            var response = await _snsClient.PublishAsync(request, cancellationToken);
            return $"Notification published from lambda, SNS messageID: {response.MessageId} HTTP Response: {response.HttpStatusCode} Message: {request.Message}";
        }

        private async Task<string> GetSecret(string secretName, CancellationToken cancellationToken)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };
            var response = await _secretsManagerClient.GetSecretValueAsync(request, cancellationToken);
            return response?.SecretString;
        }

        private string CleanUpTmpFolder()
        {
            var tmpFolder = UploadExhibitConstants.TmpFolder;
            if (!Directory.Exists(tmpFolder))
                return $"Directory {tmpFolder} not exist";

            var directory = new DirectoryInfo(tmpFolder);
            foreach (var currentFile in directory.EnumerateFiles())
                currentFile.Delete();
            foreach (var dir in directory.EnumerateDirectories())
                dir.Delete(true);
            return $"Removed all files from {tmpFolder} directory";
        }
    }
}