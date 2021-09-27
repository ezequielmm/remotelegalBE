using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Wrappers
{
    public class TagLibWrapper : ITagLibWrapper
    {
        public int GetVideoDuration(string filePath)
        {
            var tfile = TagLib.File.Create(filePath);
            return (int)tfile.Properties.Duration.TotalSeconds;
        }
    }
}
