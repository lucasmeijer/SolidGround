using System.Text.Json;
using SolidGround;

public static class TagHelper
{
    public static async Task<Tag> FindTag(JsonElement tagidElement, AppDbContext appDbContext)
    {
        if (tagidElement.ValueKind != JsonValueKind.Number)
            throw new BadHttpRequestException("Tag not a number");

        var tagid = tagidElement.GetInt32();
        var t = await appDbContext.Tags.FindAsync(tagid);
        if (t == null)
            throw new BadHttpRequestException("Tag not found");
        return t;
    }
}