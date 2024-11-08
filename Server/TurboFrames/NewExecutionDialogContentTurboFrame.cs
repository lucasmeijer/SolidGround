using Microsoft.EntityFrameworkCore;
using SolidGround;
using TurboFrames;

record NewExecutionDialogContentTurboFrame() : TurboFrame(TurboFrameId)
{
    public new static string TurboFrameId => "new_execution";
    
    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var executions = await db.Executions.ToArrayAsync();

        return new Html($"""
                        Start off from:
                        
                        {executions.Render(RenderExecution)}
                        <a href="{ExecutionsEndPoints.Routes.api_executions_new_production.For()}" data-turbo-frame="_self">Production Variables</a>
                        """);

        Html RenderExecution(Execution execution)
        {
            return $"""<a href="{ExecutionsEndPoints.Routes.api_executions_new_executionid.For(execution.Id)}" data-turbo-frame="_self">{execution.Name}</a>""";
        }
    };
    
    
}