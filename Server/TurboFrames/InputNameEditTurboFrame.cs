using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

public record InputNameEditTurboFrame(int InputId) : TurboFrame(InputNameTurboFrame.TurboFrameIdFor(InputId))
{
    protected override async Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        var input = await serviceProvider.GetRequiredService<AppDbContext>().Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        return new($"""
                    <form action="{InputEndPoints.Routes.api_input_id_name.For(InputId)}" method="post">
                        <input type="text" name="name" value="{(input.Name ?? "Naamloos")}" />
                        <button type="submit">Save</button>
                    </form>
                    """);
    }
}

