using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

public record InputNameEditTurboFrame(int InputId) : TurboFrame(InputNameTurboFrame.TurboFrameIdFor(InputId))
{
    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var input = await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        return new Html($"""
                    <form action="{InputEndPoints.Routes.api_input_id_name.For(InputId)}" method="post">
                        <input type="text" name="name" value="{(input.Name ?? "Naamloos")}" />
                        <button type="submit">Save</button>
                    </form>
                    """);
    };
}

