using System.IO;

namespace PrecisionReporters.Platform.Shared.Commons
{
    public class FileTransferInfo
    {
        public Stream FileStream { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
    }
}
