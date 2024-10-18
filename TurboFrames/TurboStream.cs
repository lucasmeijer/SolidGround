using Microsoft.AspNetCore.Http;

namespace TurboFrames;

public record TurboStream(TurboFrame[] Elements) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = "text/vnd.turbo-stream.html";
        foreach (var element in Elements)
        {
            await response.WriteAsync($"<turbo-stream action=\"update\" target=\"{element.TurboFrameId}\">");
            await response.WriteAsync("<template>");
            await response.WriteAsync(await element.RenderToStringAsync(httpContext));
            await response.WriteAsync("</template>");
            await response.WriteAsync("</turbo-stream>");
        }
    }
}