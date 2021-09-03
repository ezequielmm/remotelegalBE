using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Google.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public static class ContextFactory
    {
        public static ControllerContext GetControllerContext()
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public static ControllerContext GetControllerContext(string rawBody)
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rawBody));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = stream.Length;

            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext,
            };

            return controllerContext;
        }

        public static ControllerContext GetControllerContextWithLocalIp()
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = new IPAddress(16885952) }
                }
            };
        }

        public static ControllerContext GetControllerContextWithFile()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Content-Type", "multipart/form-data");
            var file = new FormFile(
                baseStream: new MemoryStream(Encoding.UTF8.GetBytes("Mock file")),
                baseStreamOffset: 0,
                length: 0,
                name: "Data",
                fileName: "mock.pdf");
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection { file });
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());

            return new ControllerContext(actionContext);
        }

        public static void AddUserToContext(HttpContext context, string email)
        {
            var userPrincipal = new ClaimsPrincipal();
            userPrincipal.AddIdentity(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            }));

            context.User = userPrincipal;
        }

        public static ControllerContext GetControllerContextWithSnsRequestBody()
        {
            var httpContext = new DefaultHttpContext();
            var reason = GetContextRequestBody();
            // Create the stream to house our content
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(reason));
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = stream.Length;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            return new ControllerContext(actionContext);
        }

        public static string GetContextRequestBody()
        {
            return JsonConvert.SerializeObject(new
            {
                Type = "SubscriptionConfirmation",
                MessageId = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324",
                TopicArn = "arn:aws:sns:us-west-2:123456789012:MyTopic",
                Subject = "My First Message",
                Message = "Hello world!",
                Timestamp = "2012-05-02T00:54:06.655Z",
                SignatureVersion = "1",
                Signature = "EXAMPLEw6JRN...",
                SigningCertURL = "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
                UnsubscribeURL = "https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:c9135db0-26c4-47ec-8998-413945fb5a96"
            }, Formatting.None);
        }
        
        public static string GetContextRequestBodyWithNotificationType()
        {
            return JsonConvert.SerializeObject(new
            {
                Type = "Notification",
                MessageId = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324",
                TopicArn = "arn:aws:sns:us-west-2:123456789012:MyTopic",
                Subject = "My First Message",
                Message = JsonConvert.SerializeObject(new
                {
                    Video = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324.test",
                    ConfigurationId = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324"
                }),
                Timestamp = "2012-05-02T00:54:06.655Z",
                SignatureVersion = "1",
                Signature = "EXAMPLEw6JRN...",
                SigningCertURL = "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
                UnsubscribeURL = "https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:c9135db0-26c4-47ec-8998-413945fb5a96"
            }, Formatting.None);
        }

        public static string GetContextRequestBodyWithNotificationTypeForException()
        {
            return JsonConvert.SerializeObject(new
            {
                Type = "Notification",
                MessageId = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324",
                TopicArn = "arn:aws:sns:us-west-2:123456789012:MyTopic",
                Subject = "My First Message",
                Message = "I'll crash the DeserializeObject",
                Timestamp = "2012-05-02T00:54:06.655Z",
                SignatureVersion = "1",
                Signature = "EXAMPLEw6JRN...",
                SigningCertURL = "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
                UnsubscribeURL = "https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:c9135db0-26c4-47ec-8998-413945fb5a96"
            }, Formatting.None);
        }
    }
}