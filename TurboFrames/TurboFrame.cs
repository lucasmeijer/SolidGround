using System.Globalization;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TurboFrames;

public interface ITurboFrame { string TurboFrameId { get; }}
public abstract record TurboFrame2(string TurboFrameId) : ITurboFrame, IResult, IActionResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = "text/html";
        await response.WriteAsync(await RenderIncludingTurboFrame(httpContext.RequestServices));
    }

    public async Task<Html> RenderIncludingTurboFrame(IServiceProvider serviceProvider) => new($"""
         <turbo-frame id={TurboFrameId}>
         {await RenderAsync(serviceProvider)}
         </turbo-frame>
         """);

    protected abstract Task<Html> RenderAsync(IServiceProvider serviceProvider);

    public Html RenderLazy() => new($"""<turbo-frame id="{TurboFrameId}" src="{LazySrc}" loading="lazy"></turbo-frame>""");

    protected virtual string LazySrc => throw new NotImplementedException();
    
    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract record TurboFrame(string TurboFrameId) : ITurboFrame, IResult, IActionResult
{
    protected virtual string[] AdditionalAttributes => [];

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html";
        await httpContext.Response.WriteAsync(await RenderToStringAsync(httpContext, true));
    }

    public virtual async Task<Html> RenderToStringAsync(HttpContext httpContext, bool includeTurboFrame)
    {
        var turboFrameModel = await BuildModelAsync(httpContext.RequestServices);
        
        var render = await Render(httpContext, turboFrameModel, turboFrameModel.ViewName);
        if (!includeTurboFrame)
            return render;
        
        return new($"""
                <turbo-frame id="{TurboFrameId}" {string.Join(' ',AdditionalAttributes)} >
                {render}
                </turbo-frame>
                """);
    }

    static async Task<Html> Render(HttpContext httpContext, object razorModel, string viewName)
    {
        await using var sw = new StringWriter();
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        var serviceProvider = httpContext.RequestServices;
        var viewContext = new ViewContext(
            actionContext,
            serviceProvider
                .GetRequiredService<IRazorViewEngine>()
                .FindView(actionContext, viewName, isMainPage: false)
                .View
            ?? throw new InvalidOperationException($"View '{viewName}' not found."),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new())
            {
                Model = razorModel,
            },
            new TempDataDictionary(httpContext, serviceProvider.GetRequiredService<ITempDataProvider>()),
            sw,
            new HtmlHelperOptions());
        await viewContext.View.RenderAsync(viewContext);
        return new(sw.ToString());
    }

    protected virtual async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var d = BuildModelDelegate() ?? throw new NotSupportedException("Either implement BuildModelAsync or BuildModelDelegate");
        
        var args = d.Method.GetParameters().Select(p => serviceProvider.GetRequiredService(p.ParameterType)).ToArray();
        var result = d.Method.Invoke(d.Target, args);

        if (result is TurboFrameModel turboFrameModel)
            return turboFrameModel;

        if (result is not Task task)
            throw new NotSupportedException($"Either return a {nameof(TurboFrameModel)} or return a Task<{nameof(TurboFrameModel)}>");

        dynamic dynamicTask = task;
        object result2 = await dynamicTask;

        if (result2 is not TurboFrameModel turboFrameModel2) 
            throw new NotSupportedException($"Your task should return a {nameof(TurboFrameModel)}");
        
        return turboFrameModel2;
    }

    protected virtual Delegate BuildModelDelegate() => null!;
    public Task ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}