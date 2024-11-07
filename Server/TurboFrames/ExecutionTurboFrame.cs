using TurboFrames;

namespace SolidGround;

public record ExecutionTurboFrame(int ExecutionId, bool Checked) : TurboFrame(TurboFrameIdFor(ExecutionId))
{
    public static string TurboFrameIdFor(int executionId) => $"execution_{executionId}";
    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider) => new Html($"""
          <div class="flex rounded-md bg-purple-100 items-center justify-between p-2 h-14">
             <div class="flex gap-2 items-center">
             <input {(Checked ? "checked" : "")} value="{ExecutionId}" data-action="click->filterbar#executionCheckboxClicked" type="checkbox" class="execution_checkbox w-5 h-5 rounded-full text-purple-600 focus:ring-purple-500 cursor-pointer">
             {(ExecutionId == -1 ? "Original" : await new ExecutionNameTurboFrame(ExecutionId, EditMode: false).RenderAsync(serviceProvider))}
             </div>
             {(ExecutionId != -1 ? CloseButton() : "")}         
         </div>
         """);

    Html CloseButton() => new($"""
                              <a href="{ExecutionsEndPoints.Routes.api_executions_id.For(ExecutionId)}" data-turbo-method="delete" data-turbo-confirm="Delete Execution?" class="text-pink-500 hover:text-pink-700 transition-colors duration-200 p-2 rounded-full hover:bg-pink-50">
                                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                  </svg>
                              </a>
                              """);
}