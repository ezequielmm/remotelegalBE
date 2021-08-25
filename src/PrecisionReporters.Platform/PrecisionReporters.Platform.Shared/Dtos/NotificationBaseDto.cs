namespace PrecisionReporters.Platform.Shared.Dtos
{
    public abstract class NotificationBaseDto<T>
    {
        public string NotificationType { get; set; }
        public T Context { get; set; }
    }
}