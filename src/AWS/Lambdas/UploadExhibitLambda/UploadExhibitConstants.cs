using System.Collections.Generic;

namespace UploadExhibitLambda
{
    public static class UploadExhibitConstants
    {
        public const string PdfTronKey = "PdfTronKey";
        public const string SnsTopicArn = "notificationarn";
        public const string PdfExtension = ".pdf";
        public const string TmpFolder = "/tmp/";
        public const string BucketName = "BucketName";
        public const string MaxFileSize = "MaxFileSize";
        public static HashSet<string> SkipPdfConversionExtensions { get; } = new HashSet<string> { ".mp4", ".mov", ".mp3", ".m4a", ".wav", ".ogg" };
        public static HashSet<string> OfficeDocumentExtensions { get; } = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
    }
}
