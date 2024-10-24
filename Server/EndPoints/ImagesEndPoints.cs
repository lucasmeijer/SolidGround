using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class ImagesEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate images_inputid_imageindex = RouteTemplate.Create("/images/{inputId:int}/{imageIndex:int}");
    }

    public static void MapImagesEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.images_inputid_imageindex, async (int inputId, int imageIndex, AppDbContext db, HttpContext httpContext) =>
        {
            var inputFile = await db.InputFiles
                .Include(f => f.Input)
                .FirstOrDefaultAsync(file => file.InputId == inputId && file.Index == imageIndex);
            if (inputFile == null)
                return Results.NotFound();
    
            httpContext.Response.ContentType = inputFile.MimeType;
            await httpContext.Response.Body.WriteAsync(inputFile.Bytes);
            return Results.Empty;
        });

    }
}