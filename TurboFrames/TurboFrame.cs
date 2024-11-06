using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    public sealed override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var renderContentsAsync = await RenderContentsAsync(serviceProvider);
        
        return SkipTurboFrameTags 
            ? renderContentsAsync 
            : new($"""
                   <turbo-frame id="{TurboFrameId}">
                   {renderContentsAsync}
                   </turbo-frame>
                   """);
    }

    protected virtual bool SkipTurboFrameTags => false;
    
    protected virtual Delegate RenderFunc => throw new NotImplementedException();

    Func<IServiceProvider, Task<Html>>? _compiledRenderFunc;

    Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        _compiledRenderFunc ??= ServiceProviderHelper.CompileInjectionFor<Html>(RenderFunc);
        return _compiledRenderFunc(serviceProvider);
    }

    public Html RenderLazy() => RenderLazy(TurboFrameId, LazySrc);

    public static Html RenderLazy(string turboFrameId, string lazySrc) => 
        new($"""<turbo-frame id="{turboFrameId}" src="{lazySrc}" loading="lazy"></turbo-frame>""");
    
    protected virtual string LazySrc => throw new NotImplementedException();
}
