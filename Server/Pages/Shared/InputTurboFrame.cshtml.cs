namespace SolidGround;

public record InputTurboFrame(Input Input, Tag[] AllTags) : TurboFrame($"input_{Input.Id}");