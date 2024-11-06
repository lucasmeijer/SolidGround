using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TurboFrames;

public abstract record TurboStreamBase : IResult, IActionResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/vnd.turbo-stream.html";
        await WriteAsync(httpContext);
    }

    public abstract Task WriteAsync(HttpContext httpContext);
    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}

public record TurboStream(string Action, string? Target = null, string? RawContent = null, TurboFrame? TurboFrameContent = null, string? Method = null) : TurboStreamBase
{
    record TurboStreamWithPayload(string payload) : TurboStreamBase
    {
        public override Task WriteAsync(HttpContext httpContext) => httpContext.Response.WriteAsync(payload);
    }

    public static TurboStreamBase Refresh() => new TurboStreamWithPayload($"""<turbo-stream action="refresh"></turbo-stream>""");

    public override async Task WriteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        
        var target = Target ?? TurboFrameContent?.TurboFrameId ?? throw new NotSupportedException("Either Target or TurboFrameId must be set."); 
        
        var method = Method == null ? "" : $"method=\"{Method}\"";
        
        await response.WriteAsync($"<turbo-stream action=\"{Action}\" target=\"{target}\" {method}>");
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
                await response.WriteAsync(await TurboFrameContent.RenderAsync(httpContext.RequestServices));
            
            await response.WriteAsync("</template>");
        }
        await response.WriteAsync("</turbo-stream>");
    }


}