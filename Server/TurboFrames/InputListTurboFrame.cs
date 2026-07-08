using System.Text.Json;
using TurboFrames;

namespace SolidGround;

record InputListTurboFrame(InputListItem[] Inputs, OutputListItem[] Outputs) : TurboFrame("inputlist")
{
    protected override Delegate RenderFunc => () =>
    {
        var outputsByInput = Outputs
            .GroupBy(o => o.InputId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return new Html($"""
          <div class="flex-col flex gap-4" id="inputlistdiv" data-inputids="{InputIdsAsJson}">
          {WarningElements().Render()}
          {Inputs.Render(input => InputTurboFrame.RenderSummary(
              input,
              outputsByInput.GetValueOrDefault(input.Id) ?? [],
              false))}
          </div>
         """);
    };

    string InputIdsAsJson => JsonSerializer.Serialize(Inputs.Select(i => i.Id).ToArray());

    Html[] WarningElements() => Inputs.Length == 0
        ? [new("""<div class="bg-white shadow-md rounded-lg p-4">No inputs matching this filter.</div>""")]
        : [];
}
