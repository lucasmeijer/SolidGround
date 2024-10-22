using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

[Route("/input/{InputId:int}/name/edit")]
public record InputNameEditTurboFrame(int InputId) : TurboFrame(InputNameTurboFrame.TurboFrameIdFor(InputId))
{
    public static string RouteForEditFor(int InputId) => $"/input/{InputId}/name/edit";
    protected override async Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
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

