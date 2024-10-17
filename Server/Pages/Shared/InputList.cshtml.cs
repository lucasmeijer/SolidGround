namespace SolidGround;

public record InputList(Input[] Inputs) : TurboFrame("inputlist")
{
    public override string? DataTurboAction => "morph";
}
