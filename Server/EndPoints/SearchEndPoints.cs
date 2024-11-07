using SolidGround;

static class SearchEndPoints
{
    public static void MapSearchEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", (AppDbContext db, string query, AppState appState) => Task.FromResult(MorphedBodyUpdate.For(appState with { Search = query })));
    }
}