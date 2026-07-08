using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

record FilterBarExecutionsList(int[] SelectedExecutions) : TurboFrame(TurboFrameId)
{
    public new static string TurboFrameId => "filter_bar_executions_list";

    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var executions = await db.Executions
            .AsNoTracking()
            .Where(e => e.SolidGroundInitiated)
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, e.Name, e.StartTime })
            .ToArrayAsync();

        ExecutionListItem[] allExecutions =
        [
            new(-1, "Original"),
            ..executions.Select(e => new ExecutionListItem(e.Id, e.Name ?? TimeHelper.HowMuchTimeAgo(e.StartTime)))
        ];
        
        return new Html($"""
                          <div class="grid grid-cols-4 gap-2">
                              {allExecutions.Render(execution => ExecutionTurboFrame.RenderSummary(execution, SelectedExecutions.Contains(execution.Id)))}
                             <button onclick="document.getElementById('new_execution_dialog').showModal()" class="flex rounded-md bg-purple-100 items-center justify-between p-2 h-14">
                                 New execution
                             </button>
                             <dialog id="new_execution_dialog" class="w-11/12 max-w-4xl rounded-xl p-0 backdrop:bg-gray-900/50 backdrop:backdrop-blur-sm">
                                 {
                                     RenderLazy(NewExecutionDialogContentTurboFrame.TurboFrameId, ExecutionsEndPoints.Routes.api_executions_new.For())
                                 }                   
                             </dialog>
                         </div>
                         """);
    };
}
