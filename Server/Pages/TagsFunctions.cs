namespace SolidGround.Pages;

public static class TagsFunctions
{
    public static string ColorFor(Tag tag)
    {
        //bool hasTag = tag.Id % 2 == 1;
        return (tag.Id % 3) switch
        {
            0 => "red",
            1 => "green",
            2 => "blue",
            _ => "gray" // Default case
        };
    }
}