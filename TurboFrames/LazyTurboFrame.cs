using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using TurboFrames;

public record LazyFrame(string TurboFrameId, string Src) : TurboFrame(TurboFrameId)
{
    protected override Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    public override Task<Html> RenderToStringAsync(HttpContext httpContext, bool includeTurboFrame)
    {
        if (!includeTurboFrame)
            throw new NotSupportedException("Cannot use lazy without including turboframe");
        
        return Task.FromResult(new Html($"""
                                <turbo-frame id="{TurboFrameId}" src="{Src}" loading="lazy"></turbo-frame>    
                                """));
    }
}