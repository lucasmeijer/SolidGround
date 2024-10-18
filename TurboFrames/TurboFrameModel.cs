namespace TurboFrames;

public abstract record TurboFrameModel
{
    public virtual string ViewName => GetType().DeclaringType?.Name
                                      ?? GetType().Name;
}