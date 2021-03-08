using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMultipart.ModelBinding;

namespace PrecisionReporters.Platform.Api.Dtos
{
    [ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "json")]
    public class EditDepositionDto
    {
        public DepositionDto Deposition { get; set; }
        public bool DeleteCaption { get; set; }
    }
}
