using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;

namespace StaCruzChallenge.Api.Middleware
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            
            var (statusCode, title) = exception switch
            {
                

                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
                

            };


            logger.LogError(
                exception,
                "Request failed.Path: {Path}.StatusCode: {StatusCode}. TraceId: {TraceId}",
                httpContext.Request.Path,
                statusCode,
                httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = statusCode;

            await Results.Problem(
                statusCode: statusCode,
                title: title,
                extensions: new Dictionary<string, object?>
                {
                    ["traceId"] = httpContext.TraceIdentifier
                }).ExecuteAsync(httpContext);

            return true;

        }
    }
}