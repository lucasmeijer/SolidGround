using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using TurboFrames;

static class SearchEndPoints
{
    public static void MapSearchEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search", async (HttpRequest request, AppDbContext db) =>
        {
            var json = (await JsonDocument.ParseAsync(request.Body)).RootElement;
            if (!json.TryGetProperty("tags", out var tagsElement))
                throw new BadHttpRequestException("no tags found");
    
            var tags = await Task.WhenAll(tagsElement.EnumerateArray().Select(async t => await TagHelper.FindTag(t, db)));
    
            if (!json.TryGetProperty("search", out var searchElement))
                throw new BadHttpRequestException("no search element found");

            if (!json.TryGetProperty("tags_changed", out var tagsChangedElement))
                throw new BadHttpRequestException("no tags changed");
            var tagsChanged = tagsChangedElement.GetBoolean();
    
            var searchString = searchElement.GetString()?.Trim();

            var searchTagsIds = tags
                .Select(t=>t.Id)
                .ToArray();

            var queryable = db.Inputs
                .Include(i => i.Tags)
                .Where(i => searchTagsIds.All(searchTagId => i.Tags.Any(it => it.Id == searchTagId)));

            if (!string.IsNullOrEmpty(searchString))
                queryable = queryable.Where(i => i.Name!.Contains(searchString));

            return new TurboStreamCollection([
                new("replace", TurboFrameContent: new InputListTurboFrame(await queryable.Select(t => t.Id).ToArrayAsync())),
                ..tagsChanged ? 
                    [new("replace", TurboFrameContent: new FilterBarTurboFrame(tags))] 
                    : Array.Empty<TurboStream>()
            ]);
        });
    }
}