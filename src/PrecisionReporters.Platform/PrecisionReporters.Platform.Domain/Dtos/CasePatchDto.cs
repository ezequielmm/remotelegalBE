using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TestMultipart.ModelBinding;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "json")]
    public class CasePatchDto
    {
        public List<CreateDepositionDto> Depositions { get; set; }
    }
}
