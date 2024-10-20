using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;


[Route("/api/input/{InputId}")]
public record InputTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public record Model(Input Input) : TurboFrameModel;
    
    protected override Delegate BuildModelDelegate() => async (AppDbContext db) =>
    {
        return new Model(await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("Input not found"));
    };

    protected override string[] AdditionalAttributes => ["data-turbo-permanent"];

    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";
}