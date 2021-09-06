using System.IO;
using UploadExhibitLambda.Wrappers.Interface;

namespace UploadExhibitLambda.Wrappers
{
    public class CleanUpTmpFolderWrapper : ICleanUpTmpFolderWrapper
    {
        public string CleanUpTmpFolder()
        {
            var tmpFolder = UploadExhibitConstants.TmpFolder;
            if (!Directory.Exists(tmpFolder))
                return $"Directory {tmpFolder} not exist";

            var directory = new DirectoryInfo(tmpFolder);
            foreach (var currentFile in directory.EnumerateFiles())
                currentFile.Delete();
            foreach (var dir in directory.EnumerateDirectories())
                dir.Delete(true);
            return $"Removed all files from {tmpFolder} directory";
        }
    }
}