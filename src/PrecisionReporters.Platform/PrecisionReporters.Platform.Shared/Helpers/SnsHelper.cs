using FluentResults;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public static class SnsHelper
    {
        // This method is meant to validate and confirm the endpoind added
        // as a valid destination to receive messages from aws notification service
        public static async Task<Result> SubscribeEndpoint(string subscribeURL)
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
