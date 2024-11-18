namespace SolidGround;

record InputNameTurboFrame(int InputId, bool EditMode) : EditableNameTurboFrame($"input_{InputId}_name", EditMode)
{
    protected override async Task<string> FindCurrentName(AppDbContext db)
    {
        var input = await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("input not found");
        return input.Name ?? TimeHelper.HowMuchTimeAgo(input.CreationTime);
    }

    protected override string EditRoute => InputEndPoints.Routes.api_input_id_name_edit.For(InputId);
    protected override string ChangeNameEndPoint => InputEndPoints.Routes.api_input_id_name.For(InputId);
}