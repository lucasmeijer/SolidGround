using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;


[Route("/api/input/{InputId}")]
public record InputTurboFrame(int InputId, AppDbContext Db) : TurboFrame(TurboFrameIdFor(InputId))
{
    public record Model(Input Input) : TurboFrameModel;

    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
       return new Model(await Db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("Input not found"));
    }

    public override string[] AdditionalAttributes => ["data-turbo-permanent"];

    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";
}