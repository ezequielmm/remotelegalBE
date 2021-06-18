using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Helpers
{
    public class FileHelper : IFileHelper
    {
        public async Task CreateFile(FileTransferInfo file)
        {
            using (Stream newFile = File.Create(file.Name))
            {
                await CopyStream(file.FileStream, newFile);
            }
        }

        /// <summary>
        /// Copies the contents of input into a new file.
        /// </summary>
        public async Task CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer, 0, len);
            }
        }

        public void GenerateZipFile(string zipName, List<string> filesName)
        {
            try
            {
                if (File.Exists(zipName))
                    File.Delete(zipName);

                using (var archive = ZipFile.Open(zipName, ZipArchiveMode.Create))
                {
                    foreach (var fileName in filesName)
                    {
                        var entry = archive.CreateEntryFromFile(fileName, fileName, CompressionLevel.Optimal);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                foreach (var fileName in filesName)
                {
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                }
            }
        }
    }
}
