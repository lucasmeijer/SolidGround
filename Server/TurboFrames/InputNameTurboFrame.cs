using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

[Route("/input/{InputId:int}/name")]
public record InputNameTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int InputId) => $"input_{InputId}_name";
    
    protected override async Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        var input = await serviceProvider.GetRequiredService<AppDbContext>().Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        
        return new($"""
                    <h3 class="font-semibold">
                        <a href="{InputNameEditTurboFrame.RouteForEditFor(InputId)}" data-turbo-frame="{TurboFrameIdFor(InputId)}">
                            {input.Name ?? "Naamloos"}
                        </a>
                    </h3>
                    """);
    }
}