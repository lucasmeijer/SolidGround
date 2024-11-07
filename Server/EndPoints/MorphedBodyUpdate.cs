using SolidGround.Pages;
using TurboFrames;

public static class MorphedBodyUpdate
{
    public static IResult For(AppState appState)
    {
        return new TurboStream("update", "body", TurboFrameContent: new IndexPageBodyContent(appState), Method: "morph");
    }
}