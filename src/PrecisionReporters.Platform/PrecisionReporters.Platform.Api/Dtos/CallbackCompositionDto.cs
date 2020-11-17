namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CallbackCompositionDto
    {
        public string RoomSid { get; set; }
        public string CompositionSid { get; set; }
        public string StatusCallbackEvent { get; set; }
        public string Url { get; set; }
        public string MediaUri { get; set; }
    }
}
