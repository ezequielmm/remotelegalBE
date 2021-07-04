using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using pdftron.PDF;
using pdftron.SDF;
using PrecisionReporters.Platform.Data.Enums;

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
                        archive.CreateEntryFromFile(fileName, fileName, CompressionLevel.Optimal);
                    }
                }
            }
            catch (Exception)
            {
                throw;
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

        public string CompressFile(string path)
        {
            var pathCompressedFile = path + ".gz";

            using (FileStream originalFileStream = File.OpenRead(path))
            {
                using (FileStream compressedFileStream = File.Create(pathCompressedFile))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                       CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                }
            }

            return pathCompressedFile;
        }

        public async Task<string> ConvertFileToPDF(FileTransferInfo file)
        {
            var type = Path.GetExtension(file.Name);
            var fileName = $"fileToConvert_{Guid.NewGuid()}{type}";

            using (var newFile = File.Create(fileName))
            {
                await CopyStream(file.FileStream, newFile);
            }

            using var doc = new PDFDoc();
            if (type != null && Enum.IsDefined(typeof(OfficeDocumentExtensions), type.Remove(0, 1)))
            {
                pdftron.PDF.Convert.OfficeToPDF(doc, fileName, null);
            }
            else
            {
                pdftron.PDF.Convert.ToPdf(doc, fileName);
            }

            var filePath = Path.ChangeExtension(fileName, ApplicationConstants.PDFExtension);
            doc.Save(filePath, SDFDoc.SaveOptions.e_remove_unused);

            return filePath;
        }

        public string OptimizePDF(string path)
        {
            var type = Path.GetExtension(path);
            var pathOptimizedFile = $"optimizedPDF_{Guid.NewGuid()}{ApplicationConstants.PDFExtension}";

            if (type != ApplicationConstants.PDFExtension)
                return path;

            using var doc = new PDFDoc(path);
            doc.Save(pathOptimizedFile, SDFDoc.SaveOptions.e_linearized);

            return pathOptimizedFile;
        }
    }
}
