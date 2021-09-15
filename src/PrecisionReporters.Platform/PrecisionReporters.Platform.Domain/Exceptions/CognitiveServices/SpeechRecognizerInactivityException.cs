using Microsoft.CognitiveServices.Speech;
using System;
using System.Runtime.Serialization;

namespace PrecisionReporters.Platform.Domain.Exceptions.CognitiveServices
{
    [Serializable]
    public class SpeechRecognizerInactivityException : Exception
    {
        public CancellationDetails CancellationDetails { get; private set; }

        public SpeechRecognizerInactivityException()
        {
        }

        public SpeechRecognizerInactivityException(CancellationDetails cancellationDetails)
            : base($"ErrorCode: {cancellationDetails.ErrorCode}. ErrorDetails: {cancellationDetails.ErrorDetails}.")
        {
            CancellationDetails = cancellationDetails;
        }

        public SpeechRecognizerInactivityException(string message) : base(message)
        {
        }

        public SpeechRecognizerInactivityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SpeechRecognizerInactivityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
