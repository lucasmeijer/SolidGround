using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace TurboFrames;

public static class HtmlHelperExtensions
{
    public static async Task<IHtmlContent> RenderTurboFrameAsync(this IHtmlHelper helper, TurboFrame turboFrame)
    {
        return await turboFrame.RenderToStringAsync(helper.ViewContext.HttpContext, true);
    }
    public static async Task<IHtmlContent> RenderTurboFrameAsync(this IHtmlHelper helper, TurboFrame2 turboFrame)
    {
        return await turboFrame.RenderIncludingTurboFrame(helper.ViewContext.HttpContext.RequestServices);
    }
}