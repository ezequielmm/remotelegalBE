using MediaToolkit;
using MediaToolkit.Model;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Wrappers
{
    public class MediaToolKitWrapper : IMediaToolKitWrapper
    {
        public int GetVideoDuration(string filePath)
        {
            var inputFile = new MediaFile { Filename = filePath };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
            }

            return (int)inputFile?.Metadata?.Duration.TotalSeconds;
        }
    }
}
