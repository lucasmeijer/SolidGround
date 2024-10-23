using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TurboFrames;

public record TurboStreamCollection(TurboStream[] Elements) : IResult, IActionResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/vnd.turbo-stream.html";
        foreach (var element in Elements) 
            await element.WriteAsync(httpContext);
    }

    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}