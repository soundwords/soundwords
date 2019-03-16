using System.Net;
using ServiceStack;
using ServiceStack.Web;

namespace SoundWords.Tools
{
    public static class ServiceExtensions
    {
        public static IHttpResult RedirectPermanently(this IServiceBase service, string url)
        {
            return RedirectPermanently(service, url, "Moved Permanently");
        }

        public static IHttpResult RedirectPermanently(this IServiceBase service, string url, string message) 
        {
            return new HttpResult(HttpStatusCode.MovedPermanently, message)
            {
                ContentType = service.Request.ResponseContentType,
                Headers =
                    {
                        { HttpHeaders.Location, url }
                    }
            };
        }
    }
}
