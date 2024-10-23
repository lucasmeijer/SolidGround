using Microsoft.EntityFrameworkCore;
using SolidGround;

static class TagEndPoints
{
    public static void MapTagsEndPoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/tags/{tagId}/delete", async (int tagId, AppDbContext db, HttpContext context) =>
        {
            var existing = await db.Tags.FindAsync(tagId);
            if (existing == null)
                return Results.NotFound("tag not found");

            db.Tags.Remove(existing);
            await db.SaveChangesAsync();
    
            context.Response.Headers.Append("Turbo-Visit-Control","reload");
            return Results.Redirect("/tags");
        });

        builder.MapPost("/api/tags", async (AppDbContext db, HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            if (!form.TryGetValue("tag_name", out var tagNameValues))
                return Results.BadRequest("no tag_name found");
            var tagName = tagNameValues.ToString();

            var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
            if (existing != null)
                return Results.BadRequest("tag already exists");
    
            db.Tags.Add(new Tag { Name = tagName });
            await db.SaveChangesAsync();
    
            context.Response.Headers.Append("Turbo-Visit-Control","reload");
            return Results.Redirect("/tags");
        });
    }
}
