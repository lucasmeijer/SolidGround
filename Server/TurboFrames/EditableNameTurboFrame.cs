using TurboFrames;

namespace SolidGround;

abstract record EditableNameTurboFrame(string TurboFrameId, bool EditMode) : TurboFrame(TurboFrameId)
{
    protected abstract string EditRoute { get; }
    protected abstract string ChangeNameEndPoint { get; }
    protected override Delegate RenderFunc => TempMethod;

    async Task<Html> TempMethod(AppDbContext db) => Html(await FindCurrentName(db));

    protected abstract Task<string> FindCurrentName(AppDbContext db);

    Html Html(string currentName)
    {
        return !EditMode 
            ? new Html($"""
                        <h3 class="font-semibold">
                            <a href="{EditRoute}" data-turbo-frame="{TurboFrameId}">
                                {currentName}
                            </a>
                        </h3>
                        """) 
            : new Html($"""
                        <form action="{ChangeNameEndPoint}" method="post" data-controller='formtojson'">
                            <input type="text" name="name" value="{currentName}" />
                            <button type="submit">Save</button>
                            <div data-formtojson-target="errorMessage" class="error-message"></div>
                        </form>
                        """);
    }
}