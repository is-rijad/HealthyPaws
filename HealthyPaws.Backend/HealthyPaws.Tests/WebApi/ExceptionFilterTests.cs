using System.Net;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.WebApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyPaws.Tests.WebApi;

public class ExceptionFilterTests
{
    static (ExceptionContext Context, DefaultHttpContext Http) Run(Exception exception)
    {
        var http = new DefaultHttpContext();
        var context = new ExceptionContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), [])
        {
            Exception = exception,
        };
        new ExceptionFilter(NullLogger<ExceptionFilter>.Instance).OnException(context);
        return (context, http);
    }

    static string MessageOf(ExceptionContext context) =>
        context.Result is JsonResult { Value: { } value } ? (string)value.GetType().GetProperty("message")!.GetValue(value)! : throw new Xunit.Sdk.XunitException("nema JSON rezultata");

    [Theory]
    [InlineData(typeof(BadRequestException), HttpStatusCode.BadRequest, "Loš zahtjev!")]
    [InlineData(typeof(NotFoundException), HttpStatusCode.NotFound, "Nije pronađeno!")]
    [InlineData(typeof(UnauthorizedException), HttpStatusCode.Unauthorized, "Nema pristupa!")]
    public void Known_exceptions_map_to_status_and_keep_their_message(Type type, HttpStatusCode status, string message)
    {
        var (context, http) = Run((Exception)Activator.CreateInstance(type, message)!);
        Assert.Equal((int)status, http.Response.StatusCode);
        Assert.Equal(message, MessageOf(context));
    }

    [Fact]
    public void Unknown_exception_is_500_and_does_not_leak_internal_details()
    {
        var (context, http) = Run(new InvalidOperationException("connection string: Server=prod;Password=tajna"));
        Assert.Equal(500, http.Response.StatusCode);
        Assert.Equal("Neočekivana greška!", MessageOf(context));
        Assert.DoesNotContain("tajna", System.Text.Json.JsonSerializer.Serialize(((JsonResult)context.Result!).Value));
    }

    [Fact]
    public void Response_contains_errors_dictionary_keyed_by_kind()
    {
        var (context, _) = Run(new NotFoundException("x"));
        var value = ((JsonResult)context.Result!).Value!;
        var errors = (IDictionary<string, IEnumerable<string>?>)value.GetType().GetProperty("errors")!.GetValue(value)!;
        Assert.Equal(["x"], errors["notFound"]);
    }
}
