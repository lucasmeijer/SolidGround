using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

public abstract record InputNameBase(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int InputId) => $"input_{InputId}_name";

    public record Model(Input Input, string ViewName_) : TurboFrameModel
    {
        public override string ViewName => ViewName_;
    }

    protected override Delegate BuildModelDelegate() => async (AppDbContext db) =>
    {
        var input = await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        return new Model(input, GetType().Name);
    };
}

[Route("/input/{InputId:int}/name")]
public record InputNameTurboFrame(int InputId) : InputNameBase(InputId)
{
    public static string RouteFor(int InputId) => $"/input/{InputId}/name";
}

[Route("/input/{InputId:int}/name/edit")]
public record InputNameEditTurboFrame(int InputId) : InputNameBase(InputId)
{
    public static string RouteForEditFor(int InputId) => $"/input/{InputId}/name/edit";
}
