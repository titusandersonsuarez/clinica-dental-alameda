using DentalAlameda.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace DentalAlameda.Api.Middleware;

/// <summary>Traduce excepciones de negocio a ProblemDetails con mensajes en español.</summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                DomainException => (StatusCodes.Status409Conflict, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intenta de nuevo.")
            };
            if (status == 500) logger.LogError(ex, "Error no controlado en {Path}", ctx.Request.Path);

            ctx.Response.StatusCode = status;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Instance = ctx.Request.Path },
                options: null, contentType: "application/problem+json");
        }
    }
}
