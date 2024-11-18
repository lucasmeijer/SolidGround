namespace SolidGround;

record ExecutionNameTurboFrame(int ExecutionId, bool EditMode) : EditableNameTurboFrame($"execution_{ExecutionId}_name", EditMode)
{
    protected override async Task<string> FindCurrentName(AppDbContext db)
    {
        var execution = await db.Executions.FindAsync(ExecutionId) ?? throw new BadHttpRequestException("ExecutionId not found");
        return execution.Name ?? TimeHelper.HowMuchTimeAgo(execution.StartTime);
    }

    protected override string EditRoute => ExecutionsEndPoints.Routes.api_executions_id_name_edit.For(ExecutionId);
    protected override string ChangeNameEndPoint => ExecutionsEndPoints.Routes.api_executions_id_name.For(ExecutionId);
}