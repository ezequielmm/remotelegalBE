using Newtonsoft.Json;
using PrecisionReporters.Platform.Shared.Dtos;
using System;
using static PrecisionReporters.Platform.Shared.Commons.ApplicationConstants;

namespace PrecisionReporters.Platform.Domain.Parsers
{
    internal static class SnsMessageParser
    {
        /// <summary>
        /// Parse a SNS Message text and create a ExhibitNotificationDto instace
        /// Input:
        /// {
        ///	"NotificationType": "ExhibitUploaded",
        ///	"Context": {
        ///		"Name": "256fb04e-8309-4100-b6ff-8813e991d327.pdf",
        ///		"DisplayName": "test.pdf",
        ///		"FilePath": "files/0bf0b97c-890f-4713-982c-08d957836a06/a112dd26-e5bf-4343-90e8-188da7d3e016/Exhibit/256fb04e-8309-4100-b6ff-8813e991d327.pdf",
        ///		"Size": 185109,
        ///		"AddedBy": "96ec144d-ed54-4bdb-95b4-08d94ae4acad",
        ///		"DocumentType": "Exhibit",
        ///		"Type": ".pdf",
        ///		"DepositionId": "a112dd26-e5bf-4343-90e8-188da7d3e016"
        ///	}
        /// }
        /// </summary>
        /// <param name="message"></param>
        public static ExhibitNotificationDto ParseExhibitNotification(string message)
        {
            try
            {
                var notification = JsonConvert.DeserializeObject<ExhibitNotificationDto>(message);
                if (notification.NotificationType == UploadExhibitsNotificationTypes.ExhibitUploaded)
                    return notification;
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Parse a SNS Message text and create a NotificationErrorDto instace
        /// Input:
        /// {
        ///	 "NotificationType": "ExceptionInLambda",
        ///	 "Context": "System.ArgumentNullException: Value cannot be null. ...."
        ///  }
        /// </summary>
        /// <param name="message"></param>
        public static NotificationErrorDto ParseExceptionInLambda(string message)
        {
            try
            {
                var notification = JsonConvert.DeserializeObject<NotificationErrorDto>(message);
                if (notification.NotificationType == UploadExhibitsNotificationTypes.ExceptionInLambda)
                    return notification;
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
