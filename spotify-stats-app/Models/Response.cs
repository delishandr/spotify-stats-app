using System.Net;

namespace spotify_stats_app.Models
{
    public class Response<T>
    {
        public string? message { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public T? data { get; set; }

        public Response()
        {
            statusCode = HttpStatusCode.InternalServerError;
            message = string.Empty;
            data = default(T);
        }
    }
}
