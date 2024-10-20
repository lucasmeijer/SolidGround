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

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract record TurboFrame(string TurboFrameId) : IResult, IActionResult
{
    protected virtual string[] AdditionalAttributes => [];

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/html";
        await httpContext.Response.WriteAsync(await RenderToStringAsync(httpContext));
    }

    protected internal virtual async Task<string> RenderToStringAsync(HttpContext httpContext)
    {
        var turboFrameModel = await BuildModelAsync(httpContext.RequestServices);
        
        return $"""
                 <turbo-frame id="{TurboFrameId}" {string.Join(' ',AdditionalAttributes)} >
                 {await Render(httpContext, turboFrameModel, turboFrameModel.ViewName) }
                 </turbo-frame>
                 """;
    }

    static async Task<string> Render(HttpContext httpContext, object razorModel, string viewName)
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
        return sw.ToString();
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