using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace SolidGround;

public record OutputTurboFrame(int OutputId) : TurboFrame($"output_{OutputId}")
{
    public record Model(Output Output);

    protected override async Task<object> BuildRazorModelAsync(AppDbContext dbContext)
    {
        return new Model(await dbContext.Outputs
                             .Include(o => o.Components)
                             .Include(o => o.Execution)
                             .Include(o=>o.StringVariables)
                             .AsSplitQuery()
                             .FirstOrDefaultAsync(o => o.Id == OutputId)
                         ?? throw new BadHttpRequestException("No output found"));
    }
}