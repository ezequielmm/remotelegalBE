using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingsService _service;
        public SystemSettingsController(ISystemSettingsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> SystemSettings()
        {
            var result = await _service.GetAll();

            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result.Value);
        }
    }
}
