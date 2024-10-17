using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SolidGround;

public record TurboStream(TurboFrame[] Elements) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = "text/vnd.turbo-stream.html";
        foreach (var element in Elements)
        {
            await response.WriteAsync($"<turbo-stream action=\"replace\" target=\"{element.TurboFrameId}\">");
            await response.WriteAsync("<template>");
            await response.WriteAsync(await element.RenderToStringAsync(httpContext));
            await response.WriteAsync("</template>");
            await response.WriteAsync("</turbo-stream>");
        }
    }
}

public abstract record TurboFrame(string TurboFrameId) : IResult
{
    // public async Task ExecuteResultAsync(ActionContext actionContext)
    // {
    //     HttpContext httpContext = actionContext.HttpContext;
    //     var html = await this.RenderToStringAsync(httpContext);
    //     actionContext.HttpContext.Response.ContentType = "text/html";
    //     await actionContext.HttpContext.Response.WriteAsync(html);
    // }
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html";
        await httpContext.Response.WriteAsync(await RenderToStringAsync(httpContext));
    }
    
    public async Task<string> RenderToStringAsync(HttpContext httpContext)
    {
        var httpContextRequestServices = httpContext.RequestServices;
        
        var razorViewEngine = httpContextRequestServices.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = httpContextRequestServices.GetRequiredService<ITempDataProvider>();
        
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var viewResult = razorViewEngine.FindView(actionContext, ViewName, isMainPage: false);

        if (viewResult.View == null)
        {
            throw new InvalidOperationException($"View '{ViewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new())
        {
            Model = this
        };

        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions()
        );

        await sw.WriteAsync($"<turbo-frame id=\"{TurboFrameId}\">");
        await viewResult.View.RenderAsync(viewContext);
        await sw.WriteAsync($"</turbo-frame>");

        return sw.ToString();
    }
    
 
    
    protected virtual string ViewName => GetType().Name;
}

public static class HtmlExtensions
{
    public static async Task<IHtmlContent> RenderTurboFrameAsync(this IHtmlHelper helper, TurboFrame turboFrame)
    {
        var builder = new HtmlContentBuilder();
        builder.AppendHtml(await turboFrame.RenderToStringAsync(helper.ViewContext.HttpContext));
        return builder;
    }
}