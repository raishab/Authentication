using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Extension.MiddleWare
{
    public class ExceptionHandler
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<ExceptionHandler> _logger;

        public ExceptionHandler(RequestDelegate next, ILogger<ExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch(Exception ex)
            {

                _logger.LogError(ex.Message + Environment.NewLine + " StackTrack :" + ex.StackTrace);

                await WriteExceptionToResponseStream(httpContext, ex);
            }
        }

        private async Task WriteExceptionToResponseStream(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.Headers.Clear();
            context.Response.Headers.Add("ExceptionClass", ex.GetType().AssemblyQualifiedName);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(ex));
        }
    }

    public static class ExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseExceptionHandlers(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandler>();
        }
    }
}
