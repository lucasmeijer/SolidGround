namespace SolidGround;

public record OutputTurboFrame(Output Output) : TurboFrame($"output_{Output.Id}");