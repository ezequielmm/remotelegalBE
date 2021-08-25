using System.Collections.Generic;

namespace UploadExhibitLambda
{
    public static class UploadExhibitConstants
    {
        public const string PdfTronKey = "arn:aws:secretsmanager:us-east-1:747865543072:secret:AppConfiguration__DocumentConfiguration__PDFTronLicenseKey-ZF4Gtw";
        public const string SnsTopicArn = "notificationarn";
        public const string PdfExtension = ".pdf";
        public const string TmpFolder = "/tmp/";
        public const string BucketName = "BucketName";
        public const long MaxFileSize = 52428800;
        public static HashSet<string> SkipPdfConversionExtensions { get; } = new HashSet<string> { ".mp4", ".mov", ".mp3", ".m4a", ".wav", ".ogg" };
        public static HashSet<string> OfficeDocumentExtensions { get; } = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
    }
}
