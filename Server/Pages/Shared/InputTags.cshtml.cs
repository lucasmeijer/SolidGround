using Microsoft.EntityFrameworkCore;

namespace SolidGround;

public record InputTags(string TurboFrameId, Tag[] Tags, string Endpoint) : TurboFrame(TurboFrameId)
{
    public record Model(Tag[] Tags, Tag[] AllTags, string Endpoint);

    protected override async Task<object> BuildRazorModelAsync(AppDbContext dbContext)
    {
        return new Model(Tags, await dbContext.Tags.ToArrayAsync(), Endpoint);
    }

    public static InputTags ForSearchTags(Tag[] tags)
    {
        return new("search_tags", tags, "/api/search/tags");
    }
}