using System.Text.Json;
using TurboFrames;

namespace SolidGround;

record InputListTurboFrame(InputListItem[] Inputs) : TurboFrame("inputlist")
{
    protected override Delegate RenderFunc => () => new Html($"""
          <div class="flex-col flex gap-4" id="inputlistdiv" data-inputids="{InputIdsAsJson}">
          {WarningElements().Render()}
          {Inputs.Render(input => InputTurboFrame.RenderSummary(input, false))}
          </div>
         """);

    string InputIdsAsJson => JsonSerializer.Serialize(Inputs.Select(i => i.Id).ToArray());

    Html[] WarningElements() => Inputs.Length == 0
        ? [new("""<div class="bg-white shadow-md rounded-lg p-4">No inputs matching this filter.</div>""")]
        : [];
}
