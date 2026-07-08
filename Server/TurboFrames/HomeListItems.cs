namespace SolidGround;

record InputListItem(int Id, string DisplayName)
{
    public static InputListItem From(int id, string? name, DateTime creationTime) =>
        new(id, name ?? TimeHelper.HowMuchTimeAgo(creationTime));
}

record ExecutionListItem(int Id, string DisplayName);

record OutputListItem(int Id, int InputId, string ExecutionName, ExecutionStatus Status, decimal? Cost);

static class HomeListItemQueries
{
    public static IQueryable<OutputListItem> SelectListItems(this IQueryable<Output> outputs) =>
        outputs.Select(o => new OutputListItem(
            o.Id,
            o.InputId,
            o.Execution.Name ?? "Naamloos",
            o.Status,
            o.Cost));
}
