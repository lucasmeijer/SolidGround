using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record TagsTurboFrameModel(Tag[] Tags, Tag[] AllTags, string Endpoint) : TurboFrameModel
{
    public override string ViewName => "TagsTurboFrame";
}

public record SearchTagsTurboFrame(Tag[] Tags) : TurboFrame("search_tags")
{
    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        return new TagsTurboFrameModel(Tags, await dbContext.Tags.ToArrayAsync(), $"api/search/tags");
    }
}

public record InputTagsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_tags")
{
    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var input = await dbContext
                        .Inputs
                        .Include(i => i.Tags)
                        .FirstOrDefaultAsync(i => i.Id == InputId)
                    ?? throw new BadHttpRequestException("input not found");
        
        return new TagsTurboFrameModel(input.Tags.ToArray(), await dbContext.Tags.ToArrayAsync(), $"api/input/{InputId}/tags");
    }
}