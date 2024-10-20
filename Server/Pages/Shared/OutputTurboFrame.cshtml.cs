using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record OutputTurboFrame(int OutputId) : TurboFrame(TurboFrameIdFor(OutputId))
{
    public static string TurboFrameIdFor(int outputId) => $"output_{outputId}";

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