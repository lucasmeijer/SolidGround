using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using TurboFrames;

static class SearchEndPoints
{

    record SearchRequestDto
    {
        [JsonPropertyName("tags")]
        public required int[] Tags { get; init; }

        [JsonPropertyName("search")]
        public required string Search { get; init; }
        
        [JsonPropertyName("tags_changed")]
        public required bool TagsChanged { get; init; }

        [JsonPropertyName("executions")] 
        public required int[] Executions { get; init; }
    }
    
    public static void MapSearchEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search", async (AppDbContext db, SearchRequestDto searchRequestDto, AppStateAccessor appStateAccessor) =>
        {
            // var tags = (await Task.WhenAll(searchRequestDto.Tags.Select(async tagId => await db.Tags.FindAsync(tagId))))
            //     .OfType<Tag>()//<-- using this as null check.
            //     .ToArray();
            await Task.CompletedTask;
            var newState = new AppState(searchRequestDto.Tags, searchRequestDto.Executions, searchRequestDto.Search);
            appStateAccessor.Set(newState);
            
            // var tagsChanged = searchRequestDto.TagsChanged;
            //
            // var searchString = searchRequestDto.Search?.Trim();
            //
            // var searchTagsIds = tags
            //     .Select(t=>t.Id)
            //     .ToArray();
            //
            // var queryable = db.Inputs
            //     .Include(i => i.Tags)
            //     .Where(i => searchTagsIds.All(searchTagId => i.Tags.Any(it => it.Id == searchTagId)));
            //
            // if (!string.IsNullOrEmpty(searchString))
            //     queryable = queryable.Where(i => EF.Functions.Like(i.Name, $"%{searchString}%"));
            //
            // var inputIds = await queryable.Select(t => t.Id).ToArrayAsync();
            
            return new TurboStreamCollection([
                TurboStream.Refresh()
                // new TurboStream("replace", TurboFrameContent: new InputListTurboFrame(inputIds,searchRequestDto.Executions), Method: "morph"),
                // ..tagsChanged ? 
                //     [new("replace", TurboFrameContent: new FilterBarTurboFrame(tags), Method: "morph")] 
                //     : Array.Empty<TurboStream>()
            ]);
        });
    }
}