using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class ImagesEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate images_inputid_imageindex = RouteTemplate.Create("/images/{inputId:int}/{imageIndex:int}/{imageFileName:alpha}");
    }

    public static void MapImagesEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/images/{inputId:int}/{imageIndex:int}/{imageFileName}", async (int inputId, int imageIndex, AppDbContext db, HttpContext httpContext) =>
        {
            var inputFile = await db.InputFiles
                .FirstOrDefaultAsync(file => file.InputId == inputId && file.Index == imageIndex);
            if (inputFile == null)
                return Results.NotFound();

            httpContext.Response.ContentType = inputFile.MimeType;
            await httpContext.Response.Body.WriteAsync(inputFile.Bytes);
            return Results.Empty;
        }).AllowAnonymous();

    }
}