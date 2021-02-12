using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompositionsController : ControllerBase
    {
        private readonly ICompositionService _compositionService;
        private readonly IRoomService _roomService;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;

        public CompositionsController(ICompositionService compositionService,
            IRoomService roomService, IMapper<Composition, CompositionDto, CallbackCompositionDto> compositionMapper)
        {
            _compositionService = compositionService;
            _roomService = roomService;
            _compositionMapper = compositionMapper;
        }

        [ServiceFilter(typeof(ValidateTwilioRequestFilterAttribute))]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> CompositionStatusCallback([FromForm] CallbackCompositionDto compositionDto)
        {
            var compositionModel = _compositionMapper.ToModel(compositionDto);
            var updateCompositionResult = await _compositionService.UpdateCompositionCallback(compositionModel);
            if (updateCompositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(updateCompositionResult);

            return Ok();
        }
    }
}
