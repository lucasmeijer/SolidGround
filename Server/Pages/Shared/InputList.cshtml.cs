using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record InputList(int[] InputIds) : TurboFrame("inputlist")
{
    public record Model(Input[] Inputs) : TurboFrameModel;

    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        return new Model(await serviceProvider.GetRequiredService<AppDbContext>()
            .Inputs.Where(i => InputIds.Contains(i.Id))
            .ToArrayAsync());
    }
}
