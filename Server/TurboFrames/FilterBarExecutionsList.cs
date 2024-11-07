using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record FilterBarExecutionsList(int[] SelectedExecutions) : TurboFrame(TurboFrameId)
{
    public new static string TurboFrameId => "filter_bar_executions_list";

    protected override Delegate RenderFunc => async (AppDbContext db, IServiceProvider sp) =>
    {
        int[] allExecutionIds = [-1,..await db.Executions.Select(e=>e.Id).ToArrayAsync()];
        
        return new Html($"""
                          <div class="grid grid-cols-4 gap-2">
                              {await allExecutionIds.Select(id => new ExecutionTurboFrame(id, SelectedExecutions.Contains(id))).RenderAsync(sp)}
                             <button onclick="document.getElementById('new_execution_dialog').showModal()" class="flex rounded-md bg-purple-100 items-center justify-between p-2">
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