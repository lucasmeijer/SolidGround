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

public abstract record PageFragment : IResult, IActionResult
{
    Task IActionResult.ExecuteResultAsync(ActionContext context) => ((IResult)this).ExecuteAsync(context.HttpContext);
    
    async Task IResult.ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = ResponseContentType;
        try
        {
            var html = await RenderAsync(httpContext.RequestServices);
            response.ContentType = ResponseContentType;
            await response.WriteAsync(html);    
        }
        catch (NotFoundException)
        {
            response.StatusCode = StatusCodes.Status404NotFound;
        }
    }

    public abstract Task<Html> RenderAsync(IServiceProvider serviceProvider);
    
    protected virtual string ResponseContentType => "text/html";
}

public abstract record TurboFrame(string TurboFrameId) : PageFragment
{
    public sealed override async Task<Html> RenderAsync(IServiceProvider serviceProvider) => new($"""
              <turbo-frame id={TurboFrameId} {string.Join(" ",TurboFrameAttributes)}>
              {await RenderContentsAsync(serviceProvider)}
              </turbo-frame>
              """);

    protected virtual string[] TurboFrameAttributes => [];
    
    protected abstract Task<Html> RenderContentsAsync(IServiceProvider serviceProvider);

    public Html RenderLazy() => new($"""<turbo-frame id="{TurboFrameId}" src="{LazySrc}" loading="lazy"></turbo-frame>""");

    protected virtual string LazySrc => throw new NotImplementedException();
}

public abstract record TurboFrame<T0>(string TurboFrameId) : TurboFrame(TurboFrameId) where T0 : notnull
{
    protected sealed override Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        return RenderContentsAsync(serviceProvider.GetRequiredService<T0>());
    }
    protected abstract Task<Html> RenderContentsAsync(T0 t0);
}
public abstract record TurboFrame<T0,T1>(string TurboFrameId) : TurboFrame(TurboFrameId) where T0 : notnull
{
    protected sealed override Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        return RenderContentsAsync(serviceProvider.GetRequiredService<T0>(),serviceProvider.GetRequiredService<T1>());
    }
    protected abstract Task<Html> RenderContentsAsync(T0 t0, T1 t1);
}
public abstract record TurboFrame<T0,T1,T2>(string TurboFrameId) : TurboFrame(TurboFrameId) where T0 : notnull where T1 : notnull where T2 : notnull
{
    protected sealed override Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        return RenderContentsAsync(serviceProvider.GetRequiredService<T0>(),serviceProvider.GetRequiredService<T1>(),serviceProvider.GetRequiredService<T2>());
    }
    protected abstract Task<Html> RenderContentsAsync(T0 t0, T1 t1, T2 t2);
}