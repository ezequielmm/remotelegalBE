using Microsoft.AspNetCore.Http;
using PrecisionReporters.Platform.Shared.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Shared.Extensions
{
    public class HttpContextExtensionsTest
    {
        [Fact]
        public void GetRouteGuid_Should_Return_Value()
        {
            //Arrange
            var parameterName = "DepositionId";
            var depositionIdValue = Guid.NewGuid().ToString();
            var routeValues = new Dictionary<string, object>();
            routeValues.Add(parameterName, depositionIdValue);
            var context = new DefaultHttpContext();
            context.Request.RouteValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary(routeValues);

            //Act
            var result = context.GetRouteGuid(parameterName);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(depositionIdValue, result.ToString());
        }

        [Fact]
        public void GetRouteGuid_Should_Return_Null()
        {
            //Arrange
            var parameterName = "DepositionId";
            var depositionIdValue = Guid.NewGuid().ToString();
            var routeValues = new Dictionary<string, object>();
            routeValues.Add(parameterName, depositionIdValue);
            var context = new DefaultHttpContext();
            context.Request.RouteValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary(routeValues);

            //Act
            var result = context.GetRouteGuid("AnyParameter");

            //Assert
            Assert.Null(result);
        }
    }
}
