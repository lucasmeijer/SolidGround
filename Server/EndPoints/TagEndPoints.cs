using Microsoft.EntityFrameworkCore;
using SolidGround;
using SolidGround.Pages;

static class TagEndPoints
{
    public static class Routes
    {
        public static readonly RouteTemplate tags = RouteTemplate.Create("/tags");
        public static readonly RouteTemplate api_tags = RouteTemplate.Create("/api/tags");
        public static readonly RouteTemplate api_tags_id_delete = RouteTemplate.Create("/api/tags/{id:int}/delete");
    }
    public static void MapTagsEndPoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet(Routes.tags, () => new TagsPage());
        
        builder.MapGet(Routes.api_tags_id_delete, async (int id, AppDbContext db, HttpContext context) =>
        {
            var existing = await db.Tags.FindAsync(id);
            if (existing == null)
                return Results.NotFound("tag not found");

            db.Tags.Remove(existing);
            await db.SaveChangesAsync();
    
            context.Response.Headers.Append("Turbo-Visit-Control","reload");
            return Results.Redirect(Routes.tags);
        });

        builder.MapPost(Routes.api_tags, async (AppDbContext db, HttpContext context) =>
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
            return Results.Redirect(Routes.tags);
        });
    }
}
