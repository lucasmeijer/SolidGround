using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

[Route("/api/input/{InputId}/details")]
public record InputDetailsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_details")
{
    public record Model(Input Input) : TurboFrameModel;

    public LazyFrame Lazy => new(TurboFrameId, $"/api/input/{InputId}/details");

    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var input = await dbContext.Inputs
            .Include(i => i.Tags)
            .Include(i => i.Outputs)
            .Include(i => i.Files)
            .Include(i => i.Strings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == InputId) ?? throw new BadHttpRequestException("input not found");

        return new Model(input);
    }
}