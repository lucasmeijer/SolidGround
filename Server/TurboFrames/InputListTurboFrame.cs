using System.Text.Json;
using TurboFrames;

namespace SolidGround;

public record InputListTurboFrame(int[] InputIds) : TurboFrame("inputlist")
{
    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider) => new Html($"""
          <div class="flex-col flex gap-4" id="inputlistdiv" data-inputids="{InputIdsAsJson}">
          {WarningElements().Render()}
          {await InputIds.Select(i => new InputTurboFrame(i)).RenderAsync(serviceProvider)}
          </div>
         """);

    string InputIdsAsJson => JsonSerializer.Serialize(InputIds);

    Html[] WarningElements() => InputIds.Length == 0
        ? [new("""<div class="bg-white shadow-md rounded-lg p-4">No inputs matching this filter.</div>""")]
        : [];
}