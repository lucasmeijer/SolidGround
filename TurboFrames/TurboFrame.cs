using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TurboFrames;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract record TurboFrame(string TurboFrameId) : IResult
{
    public virtual string[] AdditionalAttributes => [];
    public virtual string? DataTurboAction => null;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html";
        await httpContext.Response.WriteAsync(await RenderToStringAsync(httpContext));
    }
    
    protected internal virtual async Task<string> RenderToStringAsync(HttpContext httpContext)
    {
        var serviceProvider = httpContext.RequestServices;
        
        var razorViewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = serviceProvider.GetRequiredService<ITempDataProvider>();
        
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var turboFrameModel = await BuildModelAsync(serviceProvider);
        
        var viewResult = razorViewEngine.FindView(actionContext, turboFrameModel.ViewName, isMainPage: false);

        if (viewResult.View == null)
        {
            throw new InvalidOperationException($"View '{turboFrameModel.ViewName}' not found.");
        }
        
        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new())
        {
            Model = turboFrameModel,
        };

        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions());

        await sw.WriteAsync($"<turbo-frame id=\"{TurboFrameId}\" ");
        if (DataTurboAction != null)
            await sw.WriteAsync($"data-turbo-action=\"{DataTurboAction}\" ");
        await sw.WriteAsync(string.Join(' ',AdditionalAttributes));
        await sw.WriteLineAsync(">");
        await viewResult.View.RenderAsync(viewContext);
        await sw.WriteLineAsync($"</turbo-frame>");

        return sw.ToString();
    }
    
    protected abstract Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider);
}