using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace TurboFrames;

public static class HtmlHelperExtensions
{
    public static async Task<IHtmlContent> RenderTurboFrameAsync(this IHtmlHelper helper, TurboFrame turboFrame)
    {
        var builder = new HtmlContentBuilder();
        builder.AppendHtml(await turboFrame.RenderToStringAsync(helper.ViewContext.HttpContext, true));
        return builder;
    }
}