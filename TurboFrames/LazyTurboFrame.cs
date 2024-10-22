using Microsoft.AspNetCore.Http;
using TurboFrames;

public record LazyFrame(string TurboFrameId, string Src) : TurboFrame(TurboFrameId)
{
    protected override Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    protected internal override Task<string> RenderToStringAsync(HttpContext httpContext, bool includeTurboFrame)
    {
        if (!includeTurboFrame)
            throw new NotSupportedException("Cannot use lazy without including turboframe");
        
        return Task.FromResult($"""
                                <turbo-frame id="{TurboFrameId}" src="{Src}" loading="lazy"></turbo-frame>    
                                """);
    }
}