using System.Reflection;
using SolidGroundClient;
using Xunit;

namespace Tests;


public class SolidGroundVariablesTests
{
    class OneString : SolidGroundVariables
    {
        public string One { get; set; } = "yeah";
    }
    
    
    [Fact]
    public void SetPropertyAsString()
    {
        var oneString = new OneString();
        var oneStringProperties = oneString.Properties.ToArray();
        oneString.SetPropertyAsString(oneStringProperties.Single(),"baby");
        Assert.Equal("baby", oneString.One);
    }
    
    [Fact]
    public void GetPropertyAsString()
    {
        Assert.Equal("yeah", new OneString().GetPropertyAsString(new OneString().Properties.Single()));
    }

    class OneBool : SolidGroundVariables
    {
        public bool Maybe { get; set; } = true;
    }
    
    [Fact]
    public void SetPropertyAsString_Bool()
    {
        var oneBool = new OneBool();
        oneBool.SetPropertyAsString(oneBool.Properties.Single(),"false");
        Assert.False(oneBool.Maybe);
    }
    
    [Fact]
    public void GetPropertyAsString_Bool()
    {
        Assert.Equal("true", new OneBool().GetPropertyAsString(new OneBool().Properties.Single()));
    }

    public enum MyEnum
    {
        One,
        Two,
        Three
    }
    
    public class OneEnum : SolidGroundVariables
    {
        public MyEnum MyEnum { get; set; } = MyEnum.Two;
    }
    
    [Fact]
    public void SetPropertyAsString_Enum()
    {
        var oneEnum = new OneEnum();
        oneEnum.SetPropertyAsString(oneEnum.Properties.Single(),"three");
        Assert.Equal(MyEnum.Three, oneEnum.MyEnum);
    }
    
    [Fact]
    public void GetPropertyAsString_Enum()
    {
        Assert.Equal("Two", new OneEnum().GetPropertyAsString(new OneEnum().Properties.Single()));
    }
}