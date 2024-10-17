using Microsoft.EntityFrameworkCore;

namespace SolidGround;

public record InputTags(string TurboFrameId, Tag[] Tags, Tag[] AllTags, string Endpoint) : TurboFrame(TurboFrameId)
{
    public static async Task<InputTags> ForSearchTags(Tag[] tags, AppDbContext db)
    {
        return ForSearchTags(tags, await db.Tags.ToArrayAsync());
    }

    public static InputTags ForSearchTags(Tag[] tags, Tag[] allTags)
    {
        return new("search_tags", tags, allTags, "/api/search/tags");
    }
}