using FluentResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Transcripts.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xceed.Words.NET;

namespace PrecisionReporters.Platform.Domain.Transcripts
{
    public class GenerateRoughTranscriptWord : IRoughTranscriptGenerator
    {
        private readonly IRoughTranscriptHelper _roughTranscriptHelper;
        private readonly IAwsStorageService _awsStorageService;
        private readonly DocumentConfiguration _documentsConfigurations;
        private readonly ILogger<GenerateRoughTranscriptWord> _logger;
        private readonly string _filePath;

        public GenerateRoughTranscriptWord(IRoughTranscriptHelper roughTranscriptHelper, IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations,
            ILogger<GenerateRoughTranscriptWord> logger, IHostEnvironment env)
        {
            _roughTranscriptHelper = roughTranscriptHelper;
            _awsStorageService = awsStorageService;
            _documentsConfigurations = documentConfigurations.Value;
            _logger = logger;
            _filePath = env.ContentRootPath;
        }

        public async Task<Result> GenerateTranscriptTemplate(DraftTranscriptDto draftTranscriptDto, Deposition deposition, List<Transcription> transcripts)
        {
            try
            {
                var s3docStream = await _awsStorageService.GetObjectAsync(ApplicationConstants.DraftTranscriptWordTemplateName, _documentsConfigurations.EnvironmentFilesBucket);

                using (DocX document = DocX.Load(s3docStream))
                {
                    var transcriptsLines = _roughTranscriptHelper.CreateTranscriptRows(transcripts, false);
                    var pagesToAdd = _roughTranscriptHelper.CalculatePagesToAdd(transcriptsLines.Count);

                    GeneratePage1(document, deposition);

                    GeneratePage4(document, transcriptsLines);

                    if (pagesToAdd > 0)
                        GenerateTemplatedPages(document, pagesToAdd, transcriptsLines);

                    var temporalFolder = @"TemporalFiles\";
                    var temporalFilePath = $"{temporalFolder}{ApplicationConstants.TemporalTranscriptWordFile}";

                    // Save this document temporally to disk.
                    document.SaveAs(Path.Combine(_filePath, temporalFilePath));

                    using (DocX temporalDoc = DocX.Load(Path.Combine(_filePath, temporalFilePath)))
                    {
                        using (Stream streamDoc = new MemoryStream())
                        {
                            temporalDoc.SaveAs(streamDoc);
                            streamDoc.Position = 0;

                            var s3Result = await _roughTranscriptHelper.SaveFinalDraftOnS3(streamDoc, deposition, ApplicationConstants.DraftTranscriptWordFileName);

                            if (s3Result.IsSuccess)
                                await _roughTranscriptHelper.SaveDraftTranscription(draftTranscriptDto, ApplicationConstants.DraftTranscriptWordFileName, ApplicationConstants.WordExtension, DocumentType.DraftTranscriptionWord);

                            File.Delete(Path.Combine(_filePath, temporalFilePath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error generating Word file form stream of deposition id: {0}", deposition.Id);
                return Result.Fail(new ExceptionalError("Error generating Word file form stream.", ex));
            }
            return Result.Ok();
        }

        private void GeneratePage1(DocX document, Deposition deposition)
        {
            var startDate = deposition.GetActualStartDate() ?? deposition.StartDate;
            // Do the replacement of all the found tags and with green bold strings.                    
            document.ReplaceText("case_n_tmp", deposition.Case.CaseNumber);
            document.ReplaceText("date_tmp", startDate.ToString("dddd, MMMM d, yyyy"));
            document.ReplaceText("time_tmp", startDate.ConvertTimeZone(deposition.TimeZone));
            document.ReplaceText("witness_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.Witness)?.GetFullName()?.ToUpper() ?? "");
            document.ReplaceText("reportedBy_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.CourtReporter)?.GetFullName() ?? "");
            document.ReplaceText("job_n_tmp", deposition.Job ?? string.Empty);
        }

        private void GeneratePage4(DocX document, List<string> transcriptsLines)
        {
            if (transcriptsLines.Count < 24)
            {
                var totalTranscriptLines = _roughTranscriptHelper.AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                totalTranscriptLines.ForEach(x => document.ReplaceText($"line{x.index + 2}_tmp", x.text));
                transcriptsLines.RemoveRange(0, transcriptsLines.Count);
            }
            else
            {
                var partialTranscriptLines = transcriptsLines.Take(24).Select((text, index) => new { text, index }).ToList();
                partialTranscriptLines.ForEach(x => document.ReplaceText($"line{x.index + 2}_tmp", x.text));
                transcriptsLines.RemoveRange(0, 24);
            }
        }

        private void GenerateTemplatedPages(DocX document, int pagesToAdd, List<string> transcriptsLines)
        {
            for (int i = 1; i <= pagesToAdd; i++)
            {
                var templateStream = _awsStorageService.GetObjectAsync(ApplicationConstants.DraftTranscriptWordPage5TemplateName, _documentsConfigurations.EnvironmentFilesBucket).Result;
                using (var template = DocX.Load(templateStream))
                {
                    template.ReplaceText($"pn", (i + 4).ToString());
                    // While list is greater than 25, replace and remove lines
                    if (transcriptsLines.Count > 25)
                    {
                        var pageTranscripts = transcriptsLines.Take(25).Select((text, index) => new { text, index }).ToList();
                        pageTranscripts.ForEach(x => template.ReplaceText($"line{x.index + 1}_tmp", x.text));
                        transcriptsLines.RemoveRange(0, 25);
                    }
                    else
                    {
                        // Replace the remaining lines 
                        var remainingTranscripts = _roughTranscriptHelper.AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                        remainingTranscripts.ForEach(x => template.ReplaceText($"line{x.index + 1}_tmp", x.text));
                    }

                    // Insert a document at the end of another document.
                    // When true, document is added at the end. When false, document is added at beginning.
                    document.InsertDocument(template, true);
                }
            }
        }
    }
}
