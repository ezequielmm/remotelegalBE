using FluentResults;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class SnsHelper : ISnsHelper
    {
        // This method is meant to validate and confirm the endpoind added
        // as a valid destination to receive messages from aws notification service
        public async Task<Result> SubscribeEndpoint(string subscribeURL)
        {
            var request = (HttpWebRequest)WebRequest.Create(subscribeURL);
            try
            {
                await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                return Result.Fail(new Error("There was an error subscribing URL").CausedBy(e));
            }

            return Result.Ok();
        }
    }
}
