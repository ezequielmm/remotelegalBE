using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using pdftron.PDF;
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

namespace PrecisionReporters.Platform.Domain.Transcripts
{
    public class GenerateRoughTranscriptPDF : IRoughTranscriptGenerator
    {
        private readonly IRoughTranscriptHelper _roughTranscriptHelper;
        private readonly IAwsStorageService _awsStorageService;
        private readonly DocumentConfiguration _documentsConfigurations;
        private readonly ILogger<GenerateRoughTranscriptPDF> _logger;

        public GenerateRoughTranscriptPDF(IRoughTranscriptHelper roughTranscriptHelper, IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations,
            ILogger<GenerateRoughTranscriptPDF> logger)
        {
            _roughTranscriptHelper = roughTranscriptHelper;
            _awsStorageService = awsStorageService;
            _documentsConfigurations = documentConfigurations.Value;
            _logger = logger;

        }

        public async Task<Result> GenerateTranscriptTemplate(DraftTranscriptDto draftTranscriptDto, Deposition deposition, List<Transcription> transcripts)
        {
            try
            {
                var s3FileStream = await _awsStorageService.GetObjectAsync(ApplicationConstants.DraftTranscriptPDFTemplateName, _documentsConfigurations.EnvironmentFilesBucket);

                using (PDFDoc doc = new PDFDoc(s3FileStream))
                using (ContentReplacer replacer = new ContentReplacer())
                {
                    doc.InitSecurityHandler();

                    var transcriptsLines = _roughTranscriptHelper.CreateTranscriptRows(transcripts, true);
                    var pagesToAdd = _roughTranscriptHelper.CalculatePagesToAdd(transcriptsLines.Count);

                    AddTemplatePages(pagesToAdd, doc);

                    GeneratePage1(replacer, doc, deposition);

                    GeneratePage4(transcriptsLines, replacer, doc);

                    // Replace the rest of the pages up to 25 lines
                    GenerateTemplatedPages(transcriptsLines, replacer, doc, pagesToAdd);

                    using (Stream streamDoc = new MemoryStream())
                    {
                        // Save the document to a stream
                        doc.Save(streamDoc, 0);

                        var s3Result = await _roughTranscriptHelper.SaveFinalDraftOnS3(streamDoc, deposition, ApplicationConstants.DraftTranscriptPDFFileName);

                        if (s3Result.IsSuccess)
                            await _roughTranscriptHelper.SaveDraftTranscription(draftTranscriptDto, ApplicationConstants.DraftTranscriptPDFFileName, ApplicationConstants.PDFExtension, DocumentType.DraftTranscription);
                    }
                    return Result.Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error generating PDF file form stream of Deposition: {0}", deposition.Id);
                return Result.Fail(new ExceptionalError("Error generating PDF file form stream.", ex));
            }
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
            var startDate = deposition.GetActualStartDate() ?? deposition.StartDate;
            replacer.AddString("case_n_tmp", deposition.Case.CaseNumber);
            replacer.AddString("date_tmp", startDate.ToString("dddd, MMMM d, yyyy"));
            replacer.AddString("time_tmp", startDate.ConvertTimeZone(deposition.TimeZone));
            replacer.AddString("witness_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.Witness)?.GetFullName()?.ToUpper() ?? "");
            replacer.AddString("reportedBy_tmp", deposition.Participants?.FirstOrDefault(x => x.Role == ParticipantType.CourtReporter)?.GetFullName() ?? "");
            replacer.AddString("job_n_tmp", deposition.Job ?? string.Empty);
            replacer.Process(page1);
        }

        private void GeneratePage4(List<string> transcriptsLines, ContentReplacer replacer, PDFDoc doc)
        {
            Page page4 = doc.GetPage(4);
            // In the first transcript page we need to start writing on line two after PROCEEDINGS.
            if (transcriptsLines.Count < 24)
            {
                var totalTranscriptLines = _roughTranscriptHelper.AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                totalTranscriptLines.ForEach(x => replacer.AddString($"line{x.index + 2}_tmp", x.text));
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
                    var remainingTranscripts = _roughTranscriptHelper.AddBlankRowsToList(transcriptsLines).Select((text, index) => new { text, index }).ToList();
                    remainingTranscripts.ForEach(x => replacer.AddString($"line{x.index + 1}_tmp", x.text));
                    replacer.Process(page);
                }
            }
        }
    }
}
