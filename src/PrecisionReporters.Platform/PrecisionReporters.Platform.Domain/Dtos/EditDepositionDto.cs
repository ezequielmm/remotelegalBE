using Microsoft.AspNetCore.Mvc;
using TestMultipart.ModelBinding;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "json")]
    public class EditDepositionDto
    {
        public DepositionDto Deposition { get; set; }
        public bool DeleteCaption { get; set; }
    }
}
