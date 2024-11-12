namespace SolidGround;

public class ResultException(IResult result) : Exception
{
    public IResult Result { get; } = result;
}
