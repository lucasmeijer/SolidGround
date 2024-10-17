using System.Diagnostics;

namespace SolidGround;

public record InputTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public record Model(Input Input);

    protected override async Task<object> BuildRazorModelAsync(AppDbContext dbContext)
    {
        return new Model(await dbContext.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("Input not found"));
    }

    public override string[] AdditionalAttributes => ["data-turbo-permanent"];

    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";
}