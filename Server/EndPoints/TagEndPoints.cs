using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using SolidGround.Pages;
using TurboFrames;

static class TagEndPoints
{
    public static class Routes
    {
        public static readonly RouteTemplate tags = RouteTemplate.Create("/tags");
        public static readonly RouteTemplate api_tags = RouteTemplate.Create("/api/tags");
        public static readonly RouteTemplate api_tags_id = RouteTemplate.Create("/api/tags/{id:int}");
    }

    public class CreateTagDto
    {
         [JsonPropertyName("name")] public required string Name { get; init; }
    }
    
    public static void MapTagsEndPoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet(Routes.tags, () => new TagsPage());
        
        builder.MapDelete(Routes.api_tags_id, async (int id, AppDbContext db, HttpContext context) =>
        {
            var existing = await db.Tags.FindAsync(id);
            if (existing == null)
                return Results.NotFound("tag not found");

            db.Tags.Remove(existing);
            await db.SaveChangesAsync();

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return TurboStream.Refresh();
        });

        builder.MapPost(Routes.api_tags,
            async ([FromForm] CreateTagDto createTagDto, AppDbContext db, HttpContext httpContext) =>
            {
                var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name == createTagDto.Name);
                if (existing != null)
                    return Results.Conflict();

                db.Tags.Add(new Tag { Name = createTagDto.Name });
                await db.SaveChangesAsync();

                httpContext.Response.StatusCode = StatusCodes.Status201Created;
                return TurboStream.Refresh();
            }).DisableAntiforgery();
    }
}
