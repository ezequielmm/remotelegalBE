using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using pdftron.PDF;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DraftTranscriptGeneratorService : IDraftTranscriptGeneratorService
    {
        private readonly ITranscriptionService _transcriptionService;
        private readonly IAwsStorageService _awsStorageService;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ITransactionHandler _transactionHandler;
        private readonly DocumentConfiguration _documentsConfigurations;
        private readonly ILogger<DraftTranscriptGeneratorService> _logger;
        private const int MAX_LENGHT = 56;

        public DraftTranscriptGeneratorService(ITranscriptionService transcriptionService,
            IAwsStorageService awsStorageService,
            IDepositionRepository depositionRepository,
            IOptions<DocumentConfiguration> documentConfigurations,
            IDepositionDocumentRepository depositionDocumentRepository,
            IDocumentRepository documentRepository,
            ITransactionHandler transactionHandler,
            ILogger<DraftTranscriptGeneratorService> logger)
        {
            _transcriptionService = transcriptionService;
            _awsStorageService = awsStorageService;
            _depositionRepository = depositionRepository;
            _documentsConfigurations = documentConfigurations.Value;
            _depositionDocumentRepository = depositionDocumentRepository;
            _documentRepository = documentRepository;
            _transactionHandler = transactionHandler;
            _logger = logger;
        }

        public async Task<Result> GenerateDraftTranscriptionPDF(DraftTranscriptDto draftTranscriptDto)
        {
            var include = new[] { nameof(Deposition.Case), nameof(Deposition.Participants), nameof(Deposition.Requester) };
            var result = await _transcriptionService.GetTranscriptionsByDepositionId(draftTranscriptDto.DepositionId);
            var deposition = await _depositionRepository.GetFirstOrDefaultByFilter(x => x.Id == draftTranscriptDto.DepositionId, include);
            try
            {
                var s3FileStream = await _awsStorageService.GetObjectAsync(ApplicationConstants.DraftTranscriptTemplateName, _documentsConfigurations.EnvironmentFilesBucket);
                using (PDFDoc doc = new PDFDoc(s3FileStream))
                using (ContentReplacer replacer = new ContentReplacer())
                {
                    doc.InitSecurityHandler();

                    var transcriptsLines = CreateTranscriptRows(result.Value);

                    var pagesToAdd = CalculatePagesToAdd(transcriptsLines.Count);

                    AddTemplatePages(pagesToAdd, doc);

                    GeneratePage1(replacer, doc, deposition);

                    GeneratePage4(transcriptsLines, replacer, doc);

                    // Replace the rest of the pages up to 25 lines
                    GenerateTemplatedPages(transcriptsLines, replacer, doc, pagesToAdd);

                    var s3Result = await SaveFinalDraftOnS3(doc, deposition);

                    if (s3Result.IsSuccess)
                        await SaveDraftTranscriptionPDF(draftTranscriptDto);                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Result.Fail(new ExceptionalError("Error generating file form stream.", ex));
            }

            return Result.Ok();
        }

        public async Task<Result> SaveDraftTranscriptionPDF(DraftTranscriptDto draftTranscriptDto)
        {
            var deposition = await _depositionRepository.GetById(draftTranscriptDto.DepositionId);
            var filePath = $"/{deposition.CaseId}/{deposition.Id}/{ApplicationConstants.TranscriptFolderName}/{ApplicationConstants.DraftTranscriptFileName}";
            var draftTranscript = await _awsStorageService.GetObjectAsync(filePath, _documentsConfigurations.BucketName);

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                var document = new Document()
                {
                    Name = ApplicationConstants.DraftTranscriptFileName,
                    DisplayName = ApplicationConstants.DraftTranscriptFileName,
                    CreationDate = DateTime.UtcNow,
                    Type = ".pdf",
                    FilePath = filePath,
                    DocumentType = DocumentType.DraftTranscription,
                    Size = draftTranscript.Length,
                    AddedById = draftTranscriptDto.CurrentUserId,
                };

                var createdDocument = await _documentRepository.Create(document);
                if (createdDocument != null)
                {
                    var depositionDocument = new DepositionDocument
                    {
                        CreationDate = DateTime.UtcNow,
                        DepositionId = deposition.Id,
                        DocumentId = createdDocument.Id
                    };

                    await _depositionDocumentRepository.Create(depositionDocument);
                }
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            return Result.Ok();
        }

        private List<string> CreateTranscriptRows(List<Transcription> transcripts)
        {
            var transcriptsLines = new List<string>();
            foreach (var sentence in transcripts)
            {
                if (!string.IsNullOrEmpty(sentence.Text))
                {
                    var text = $"\t\t{ sentence.User.FirstName } { sentence.User.LastName }: {sentence.Text}";

                    if (text.Length > MAX_LENGHT)
                        transcriptsLines.AddRange(SplitSentences(text));
                    else
                        transcriptsLines.Add(text);
                }
            }

            return transcriptsLines;
        }

        private void AddTemplatePages(int pagesToAdd, PDFDoc doc)
        {
            //Add transcript template page based on the amount of transcripts rows
            var pagesAmount = doc.GetPageCount();
            Page page5 = doc.GetPage(5);

            // Start from 1 because we already have the template page
            for (int i = 1; i < pagesToAdd; i++)
            {
                PageIterator p = doc.GetPageIterator(pagesAmount);
                doc.PageInsert(p, page5);
                pagesAmount++;
            }
        }

        private void GeneratePage1(ContentReplacer replacer, PDFDoc doc, Deposition deposition)
        {
            Page page1 = doc.GetPage(1);
            replacer.AddString("case_n_tmp", deposition.Case.CaseNumber);
            replacer.AddString("mm_tmp", deposition.StartDate.Month.ToString());
            replacer.AddString("dd_tmp", deposition.StartDate.Day.ToString());
            replacer.AddString("yyyy_tmp", deposition.StartDate.Year.ToString());
            replacer.AddString("time_tmp", ConvertTimeZone(deposition.StartDate, deposition.TimeZone));
            replacer.AddString("witness_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.Witness)?.Name ?? "");
            replacer.AddString("reportedBy_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.CourtReporter)?.Name ?? "");
            replacer.AddString("job_n_tmp", deposition.Job ?? string.Empty);
            replacer.Process(page1);
        }

        private void GeneratePage4(List<string> transcriptsLines, ContentReplacer replacer, PDFDoc doc)
        {
            Page page4 = doc.GetPage(4);
            // In the first transcript page we need to start writing on line two after PROCEEDINGS.
            if (transcriptsLines.Count < 24)
            {
                var totalTranscriptLInes = AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                totalTranscriptLInes.ForEach(x => replacer.AddString($"line{x.index + 2}_tmp", x.text));
                transcriptsLines.RemoveRange(0, transcriptsLines.Count);
                doc.PageRemove(doc.GetPageIterator(5));
            }
            else
            {                
                var partialTranscriptLines = transcriptsLines.Take(24).Select((text, index) => new { text, index }).ToList();
                partialTranscriptLines.ForEach(x => replacer.AddString($"line{x.index + 2}_tmp", x.text));
                transcriptsLines.RemoveRange(0, 24);
            }
            replacer.Process(page4);
        }

        private void GenerateTemplatedPages(List<string> transcriptsLines, ContentReplacer replacer, PDFDoc doc, int pagesToAdd)
        {
            for (int x = 1; x <= pagesToAdd; x++)
            {
                Page page = doc.GetPage(x + 4);
                replacer.AddString($"pn", (x + 4).ToString());

                // While list is greater than 25, replace and remove lines
                if (transcriptsLines.Count > 25)
                {
                    var pageTranscripts = transcriptsLines.Take(25).Select((text, index) => new { text, index }).ToList();
                    pageTranscripts.ForEach(x => replacer.AddString($"line{x.index + 1}_tmp", x.text));
                    replacer.Process(page);
                    transcriptsLines.RemoveRange(0, 25);
                }
                else
                {
                    // Replace the remaining lines 
                    var remainingTranscripts = AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                    remainingTranscripts.ForEach(x => replacer.AddString($"line{x.index + 1}_tmp", x.text));
                    replacer.Process(page);
                }
            }
        }

        private async Task<Result> SaveFinalDraftOnS3(PDFDoc doc, Deposition deposition)
        {
            try
            {
                using (Stream streamDoc = new MemoryStream())
                {
                    // Save the document to a stream
                    doc.Save(streamDoc, 0);

                    var fileName = $"{ApplicationConstants.DraftTranscriptFileName}";
                    var parentPath = $"/{deposition.CaseId}/{deposition.Id}/{ApplicationConstants.TranscriptFolderName}/";
                    await _awsStorageService.UploadObjectFromStreamAsync($"{parentPath}{fileName}", streamDoc, _documentsConfigurations.BucketName);
                }
                return Result.Ok();
            }
            catch(Exception ex)
            {
                return Result.Fail(new ExceptionalError("Error executing transaction operation in S3.", ex));
            }
        }

        private List<string> SplitSentences(string sentence)
        {
            List<string> lines = new List<string>();
            int bmark = 0; //bookmark position

            Regex.Replace(sentence, @".*?\b\w+\b.[,.:]*?",
                delegate (Match m)
                {
                    if (m.Index - bmark + m.Length + m.NextMatch().Length > MAX_LENGHT
                            || m.Index == bmark && m.Length >= MAX_LENGHT)
                    {
                        lines.Add(sentence.Substring(bmark, m.Index - bmark + m.Length));
                        bmark = m.Index + m.Length;
                    }
                    return null;
                }, RegexOptions.Singleline);

            if (bmark != sentence.Length) // last portion
                lines.Add(sentence.Substring(bmark));

            return lines;
        }

        private int CalculatePagesToAdd(int transcripts)
        {
            var pages = (decimal)(transcripts - 24) / 25;
            //If we don't have any decimal number we don't need to add an extra page.
            if (pages % 1 == 0)
                return (int)pages;

            return (int)(pages + 1);
        }

        private string ConvertTimeZone(DateTime time, string timeZone)
        {
            var timeZoneFullName = EnumExtensions.GetDescription((USTimeZone)Enum.Parse(typeof(USTimeZone), timeZone));
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZoneFullName);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(time, timeZoneInfo);

            return $"{convertedTime.ToShortTimeString()} {timeZone}";
        }

        private List<string> AddBlankRowsToList(List<string> transcripts)
        {
            //We need to add blank rows in the remaining lines to replace placeholders with empty string
            var blankPages = 25 - transcripts.Count;
            for (int i = 0; i < blankPages; i++)
            {
                transcripts.Add(string.Empty);
            }

            return transcripts;
        }
    }
}
