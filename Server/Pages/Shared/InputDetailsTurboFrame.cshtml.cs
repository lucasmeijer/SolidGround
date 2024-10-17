using Microsoft.EntityFrameworkCore;

namespace SolidGround;

public record InputDetailsTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public record Model(Input Input, Tag[] AllTags);

    protected override async Task<object> BuildRazorModelAsync(AppDbContext dbContext)
    {
        var input = await dbContext.Inputs
            .Include(i => i.Tags)
            .Include(i => i.Outputs)
            .Include(i => i.Files)
            .Include(i => i.Strings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == InputId) ?? throw new BadHttpRequestException("input not found");

        return new Model(input, await dbContext.Tags.ToArrayAsync());
    }

    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}_details";
}