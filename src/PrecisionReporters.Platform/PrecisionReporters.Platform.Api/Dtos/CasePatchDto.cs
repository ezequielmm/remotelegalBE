using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TestMultipart.ModelBinding;

namespace PrecisionReporters.Platform.Api.Dtos
{
    [ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "json")]
    public class CasePatchDto
    {
        public List<CreateDepositionDto> Depositions { get; set; }
    }
}
