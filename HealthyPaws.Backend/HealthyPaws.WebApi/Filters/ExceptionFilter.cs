using System.Linq;
using HealthyPaws.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Microsoft.Extensions.Logging;

namespace HealthyPaws.WebApi.Filters;

public class ExceptionFilter(ILogger<ExceptionFilter> logger) : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context) {
        logger.LogError(context.Exception, context.Exception.Message);

        if (context.Exception is BadRequestException) {
            context.ModelState.AddModelError("badRequest", context.Exception.Message);
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else if (context.Exception is NotFoundException) {
            context.ModelState.AddModelError("notFound", context.Exception.Message);
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else if (context.Exception is UnauthorizedException) {
            context.ModelState.AddModelError("unauthorized", context.Exception.Message);
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else {
            context.ModelState.AddModelError("serverError", "Neočekivana greška!");
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        var list = context.ModelState.Where(x => x.Value != null && x.Value.Errors.Count > 0)
            .ToDictionary(x => x.Key, y => y.Value?.Errors.Select(z => z.ErrorMessage));

        var lastError = list.SelectMany(x => x.Value!).Last();
        context.Result = new JsonResult(new
        {
            message = lastError,
            errors = list
        });
    }
}