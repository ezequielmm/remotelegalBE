using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using PrecisionReporters.Platform.Domain.Commons;

namespace PrecisionReporters.Platform.Api.Helpers
{
    public static class FileHandlerHelper
    {
        public static Dictionary<string, FileTransferInfo> GetFilesFromRequest(IFormFileCollection files)
        {
            var filesMap = new Dictionary<string, FileTransferInfo>();
            foreach (var file in files)
            {
                var fileTransferInfo = new FileTransferInfo
                {
                    FileStream = file.OpenReadStream(),
                    Name = file.FileName,
                    Length = file.Length
                };
                filesMap.Add(file.Name, fileTransferInfo);
            }

            return filesMap;
        }
    }
}
