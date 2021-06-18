using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Helpers.Interfaces
{
    public interface IFileHelper
    {
        Task CreateFile(FileTransferInfo file);
        Task CopyStream(Stream input, Stream output);
        void GenerateZipFile(string zipName, List<string> filesName);
    }
}
