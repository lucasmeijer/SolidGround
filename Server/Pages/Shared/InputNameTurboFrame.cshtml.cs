using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;
//
// public abstract record InputNameBase(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
// {
//     public static string TurboFrameIdFor(int InputId) => $"input_{InputId}_name";
//
//     public record Model(Input Input, string ViewName_) : TurboFrameModel
//     {
//         public override string ViewName => ViewName_;
//     }
//
//     protected override Delegate BuildModelDelegate() => async (AppDbContext db) =>
//     {
//         var input = await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
//         return new Model(input, GetType().Name);
//     };
// }
//
// [Route("/input/{InputId:int}/name")]
// public record InputNameTurboFrame(int InputId) : InputNameBase(InputId)
// {
//     public static string RouteFor(int InputId) => $"/input/{InputId}/name";
// }
//
// [Route("/input/{InputId:int}/name/edit")]
// public record InputNameEditTurboFrame(int InputId) : InputNameBase(InputId)
// {
//     public static string RouteForEditFor(int InputId) => $"/input/{InputId}/name/edit";
// }



[Route("/input/{InputId:int}/name")]
public record InputNameTurboFrame2(int InputId) : TurboFrame2(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int InputId) => $"input_{InputId}_name";
    public static string RouteFor(int InputId) => $"/input/{InputId}/name";

    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var input = await serviceProvider.GetRequiredService<AppDbContext>().Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        
        return new($"""
                <h3 class="font-semibold">
                    <a href="{InputNameEditTurboFrame2.RouteForEditFor(InputId)}" data-turbo-frame="{TurboFrameIdFor(InputId)}">
                        {input.Name ?? "Naamloos"}
                    </a>
                </h3>
                """);
    }
}

[Route("/input/{InputId:int}/name/edit")]
public record InputNameEditTurboFrame2(int InputId) : TurboFrame2(InputNameTurboFrame2.TurboFrameIdFor(InputId))
{
    public static string RouteForEditFor(int InputId) => $"/input/{InputId}/name/edit";
    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var input = await serviceProvider.GetRequiredService<AppDbContext>().Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        return new($"""
                    <form action="{InputController.ModifyInputRouteFor(InputId)}" method="post">
                        <input type="text" name="name" value="{input.Name ?? "Naamloos"}" />
                        <button type="submit">Save</button>
                    </form>
                    """);
    }
}

