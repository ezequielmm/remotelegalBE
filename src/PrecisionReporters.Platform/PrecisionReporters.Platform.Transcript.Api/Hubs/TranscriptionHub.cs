using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs
{
    [Authorize]
    [HubName("/transcriptionHub")]
    public class TranscriptionHub
    {
    }
}
