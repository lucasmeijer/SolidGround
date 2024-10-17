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
            await response.WriteAsync($"<turbo-stream action=\"update\" target=\"{element.TurboFrameId}\">");
            await response.WriteAsync("<template>");
            await response.WriteAsync(await element.RenderToStringAsync(httpContext));
            await response.WriteAsync("</template>");
            await response.WriteAsync("</turbo-stream>");
        }
    }
}

public abstract record TurboFrame(string TurboFrameId) : IResult
{
    public virtual string[] AdditionalAttributes => [];
    public virtual string? DataTurboAction => null;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html";
        await httpContext.Response.WriteAsync(await RenderToStringAsync(httpContext));
    }
    
    public async Task<string> RenderToStringAsync(HttpContext httpContext)
    {
        var serviceProvider = httpContext.RequestServices;
        
        var razorViewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = serviceProvider.GetRequiredService<ITempDataProvider>();
        
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var viewResult = razorViewEngine.FindView(actionContext, ViewName, isMainPage: false);

        if (viewResult.View == null)
        {
            throw new InvalidOperationException($"View '{ViewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new())
        {
            Model = await BuildRazorModelAsync(serviceProvider.GetRequiredService<AppDbContext>()),
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

        await sw.WriteAsync($"<turbo-frame id=\"{TurboFrameId}\" ");
        if (DataTurboAction != null)
            await sw.WriteAsync($"data-turbo-action=\"{DataTurboAction}\" ");
        await sw.WriteAsync(AdditionalAttributes.SeparateWith(" "));
        await sw.WriteLineAsync(">");
        await viewResult.View.RenderAsync(viewContext);
        await sw.WriteLineAsync($"</turbo-frame>");

        return sw.ToString();
    }
    protected virtual string ViewName => GetType().Name;
    protected virtual Task<object> BuildRazorModelAsync(AppDbContext dbContext) => Task.FromResult<object>(this);
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