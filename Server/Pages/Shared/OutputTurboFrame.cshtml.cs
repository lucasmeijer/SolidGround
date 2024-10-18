using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

[Route("/output{OutputId}")]
public record OutputTurboFrame(int OutputId) : TurboFrame($"output_{OutputId}")
{
    public record Model(Output Output) : TurboFrameModel;

    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        return new Model(await dbContext.Outputs
                             .Include(o => o.Components)
                             .Include(o => o.Execution)
                             .Include(o=>o.StringVariables)
                             .AsSplitQuery()
                             .FirstOrDefaultAsync(o => o.Id == OutputId)
                         ?? throw new BadHttpRequestException("No output found"));
    }
}