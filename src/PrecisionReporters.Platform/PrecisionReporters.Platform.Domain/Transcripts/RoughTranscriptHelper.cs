using FluentResults;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Transcripts.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Transcripts
{
    public class RoughTranscriptHelper : IRoughTranscriptHelper
    {
        private readonly IDepositionRepository _depositionRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly ITransactionHandler _transactionHandler;
        private readonly IAwsStorageService _awsStorageService;
        private readonly DocumentConfiguration _documentsConfigurations;

        private const int MAX_CHARACTERS_PER_LINE = 56;

        public RoughTranscriptHelper(IDepositionRepository depositionRepository, IDocumentRepository documentRepository, 
            IDepositionDocumentRepository depositionDocumentRepository, ITransactionHandler transactionHandler, 
            IAwsStorageService awsStorageService, IOptions<DocumentConfiguration> documentConfigurations) 
        {
            _depositionRepository = depositionRepository;
            _documentRepository = documentRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
            _transactionHandler = transactionHandler;
            _awsStorageService = awsStorageService;
            _documentsConfigurations = documentConfigurations.Value;
        }

        public int CalculatePagesToAdd(int transcripts)
        {
            var pages = (decimal)(transcripts - 24) / 25;
            //If we don't have any decimal number we don't need to add an extra page.
            if (pages % 1 == 0)
                return (int)pages;

            return (int)(pages + 1);
        }

        public List<string> SplitSentences(string sentence)
        {
            List<string> lines = new List<string>();
            int bmark = 0; //bookmark position

            Regex.Replace(sentence, @".*?\b\w+\b.[,.:]*?",
                delegate (Match m)
                {
                    if (m.Index - bmark + m.Length + m.NextMatch().Length > MAX_CHARACTERS_PER_LINE
                            || m.Index == bmark && m.Length >= MAX_CHARACTERS_PER_LINE)
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

        public List<string> AddBlankRowsToList(List<string> transcripts)
        {
            //We need to add blank rows in the remaining lines to replace placeholders with empty string
            var blankPages = 25 - transcripts.Count;
            for (int i = 0; i < blankPages; i++)
            {
                transcripts.Add(string.Empty);
            }

            return transcripts;
        }

        public List<string> CreateTranscriptRows(List<Transcription> transcripts, bool isPDF)
        {
            var transcriptsLines = new List<string>();
            foreach (var sentence in transcripts)
            {
                string text;
                var fullName = sentence.User.IsGuest ? $"{sentence.User.FirstName?.Trim()}" : $"{sentence.User.FirstName?.Trim()} {sentence.User.LastName?.Trim()}";

                if (!string.IsNullOrEmpty(sentence.Text))
                {
                    if (isPDF) 
                    {
                        text = $"\t\t{fullName}: {sentence.Text}";
                    }                    
                    else 
                    {
                        text = $"\t{fullName}: {sentence.Text}";
                    }

                    if (text.Length > MAX_CHARACTERS_PER_LINE)
                        transcriptsLines.AddRange(SplitSentences(text));
                    else
                        transcriptsLines.Add(text);
                }
            }

            return transcriptsLines;
        }

        public async Task<Result> SaveDraftTranscription(DraftTranscriptDto draftTranscriptDto, string fileName, string fileType, DocumentType documentType)
        {
            var deposition = await _depositionRepository.GetById(draftTranscriptDto.DepositionId);
            var filePath = $"/{deposition.CaseId}/{deposition.Id}/{ApplicationConstants.TranscriptFolderName}/{fileName}";
            var draftTranscript = await _awsStorageService.GetObjectAsync(filePath, _documentsConfigurations.BucketName);

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                var document = new Document()
                {
                    Name = fileName,
                    DisplayName = fileName,
                    CreationDate = DateTime.UtcNow,
                    Type = fileType,
                    FilePath = filePath,
                    DocumentType = documentType,
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

        public async Task<Result> SaveFinalDraftOnS3(Stream streamDoc, Deposition deposition, string fileName)
        {
            try
            {
                var parentPath = $"/{deposition.CaseId}/{deposition.Id}/{ApplicationConstants.TranscriptFolderName}/";
                await _awsStorageService.UploadObjectFromStreamAsync($"{parentPath}{fileName}", streamDoc, _documentsConfigurations.BucketName);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(new ExceptionalError("Error executing transaction operation in S3.", ex));
            }
        }
    }
}
