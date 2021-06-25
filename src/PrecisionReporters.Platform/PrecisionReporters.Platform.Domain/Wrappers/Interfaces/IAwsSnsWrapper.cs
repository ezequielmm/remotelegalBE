using Amazon.SimpleNotificationService.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Wrappers.Interfaces
{
    public interface IAwsSnsWrapper
    {
        public bool IsMessageSignatureValid(Message message);
        public Message ParseMessage(string content);
    }
}
