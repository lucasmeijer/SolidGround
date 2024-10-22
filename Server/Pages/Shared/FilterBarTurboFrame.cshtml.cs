using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record FilterBarTurboFrame(Tag[] Tags) : TurboFrame("filter_bar")
{
    public record Model(Tag[] Tags, Tag[] AllTags) : TurboFrameModel;
    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        return new Model(Tags, await dbContext.Tags.ToArrayAsync());
    }
}