namespace PrecisionReporters.Platform.Shared.Commons
{
    public static class ApplicationConstants
    {
        public const string VerificationCodeException = "Verification Code is already used or out of date.";
        public const string RoomExistError = "Room exists";
        public const string DraftTranscriptPDFTemplateName = "rough-draft-transcript-template.pdf";
        public const string DraftTranscriptWordTemplateName = "rough-draft-transcript-template.docx";
        public const string DraftTranscriptWordPage5TemplateName = "rough-draft-transcript-template-page-5.docx";
        public const string DraftTranscriptPDFFileName = "RoughTranscript.pdf";
        public const string DraftTranscriptWordFileName = "RoughTranscript.docx";
        public const string TranscriptFolderName = "transcripts";
        public const string DepositionGroupName = "Deposition:";
        public const string DepositionAdminsGroupName = "DepositionAdmins:";
        public const string Mp4 = "mp4";
        public const string Mp3 = "mp3";
        public const string TemporalFileFolder = @"\TemporalFiles\";
        public const string TemporalTranscriptWordFile = "rough-draft-transcript-template-temporal.docx";
        public const string PDFExtension = ".pdf";
        public const string WordExtension = ".docx";
        //TODO: unify ".mp4" and "mp4" constants
        public const string Mp4Extension = ".mp4";
        public const string ZipExtension = ".zip";
        public const int DefaultSampleRate = 48000;
        public const string UserIdExhibitsMetadata = "x-amz-meta-user-id";
        public const string DepositionIdExhibitsMetadata = "x-amz-meta-deposition-id";
        public const string CaseIdExhibitsMetadata = "x-amz-meta-case-id";
        public const string DocumentTypeExhibitsMetadata = "x-amz-meta-document-type";
        public const string DisplayNameExhibitsMetadata = "x-amz-meta-display-name";
        public const string TypeExhibitsMetadata = "x-amz-meta-type";
        public const string ResourceIdExhibitsMetadata = "x-amz-meta-resource-id";
        public static class UploadExhibitsNotificationTypes
        {
            public const string ExhibitUploaded = "ExhibitUploaded";
            public const string ExceededSize = "ExceededSize";
            public const string InvalidS3Structure = "InvalidS3Structure";
            public const string ExceptionInLambda = "ExceptionInLambda";
        }
    }
}
