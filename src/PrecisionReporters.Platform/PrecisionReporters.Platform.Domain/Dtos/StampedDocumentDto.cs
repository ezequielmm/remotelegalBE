using Microsoft.AspNetCore.Mvc;
using TestMultipart.ModelBinding;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "json")]
    public class StampedDocumentDto
    {
        public string StampLabel { get; set; }
    }
}
