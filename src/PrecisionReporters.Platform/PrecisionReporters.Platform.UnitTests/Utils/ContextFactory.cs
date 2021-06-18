using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

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

        public static ControllerContext GetControllerContextWithLocalIp()
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = new IPAddress(16885952)}
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
    }
}