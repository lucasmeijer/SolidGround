using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TurboFrames;
//
// public record TurboStreams(TurboFrame[] Elements) : IResult
// {
//     public async Task ExecuteAsync(HttpContext httpContext)
//     {
//         var response = httpContext.Response;
//         response.ContentType = "text/vnd.turbo-stream.html";
//         foreach (var element in Elements)
//         {
//             await response.WriteAsync($"<turbo-stream action=\"update\" target=\"{element.TurboFrameId}\">");
//             await response.WriteAsync("<template>");
//             await response.WriteAsync(await element.RenderToStringAsync(httpContext));
//             await response.WriteAsync("</template>");
//             await response.WriteAsync("</turbo-stream>");
//         }
//     }
// }

public record TurboStreams2(TurboStream[] Elements) : IResult, IActionResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/vnd.turbo-stream.html";
        foreach (var element in Elements) 
            await element.WriteAsync(httpContext);
    }

    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}

public record TurboStream(string Action, string? Target = null, string? RawContent = null, TurboFrame? TurboFrameContent = null) : IResult, IActionResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/vnd.turbo-stream.html";
        await WriteAsync(httpContext);
    }

    internal async Task WriteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var target = Target ?? TurboFrameContent?.TurboFrameId ?? throw new NotSupportedException("Either Target or TurboFrameId must be set."); 
        
        await response.WriteAsync($"<turbo-stream action=\"{Action}\" target=\"{target}\">");
        if (RawContent != null || TurboFrameContent != null)
        {
            await response.WriteAsync("<template>");

            if (RawContent != null)
            {
                await response.WriteAsync(RawContent);
                if (TurboFrameContent != null)
                    throw new NotSupportedException("Only one of RawContent or TurboFrameContent can be used");
            }

            if (TurboFrameContent != null)
            {
                await response.WriteAsync(await TurboFrameContent.RenderToStringAsync(httpContext, Action!="update"));
            }
            await response.WriteAsync("</template>");
        }
        await response.WriteAsync("</turbo-stream>");
    }


    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}