using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Xunit;


public class RouteTemplateTests
{
    [Fact]
    public void ConstructorValidatesRouteTemplate()
    {
        var validTemplate = RouteTemplate.Create("api/input/{id:int}/{name:alpha}");
        Assert.NotNull(validTemplate);

        Assert.Throws<ArgumentException>(() => RouteTemplate.Create("api/input/{id:float}"));
    }

    [Fact]
    public void ForMethodCreatesRouteTemplateCorrectly()
    {
        var template = RouteTemplate.Create("api/input/{id:int}");
        Assert.Equal("api/input/{id:int}", template.Value);
    }

    [Fact]
    public void ToStringReturnsValue()
    {
        var template = RouteTemplate.Create("api/input/{id:int}");
        Assert.Equal("api/input/{id:int}", template.ToString());
    }

    [Fact]
    public void ImplicitConversionToStringWorks()
    {
        var template = RouteTemplate.Create("api/input/{id:int}");
        string value = template;
        Assert.Equal("api/input/{id:int}", value);
    }

    [Fact]
    public void UrlForOneParameterWorksCorrectly()
    {
        var template = RouteTemplate.Create("api/input/{id:int}");
        var url = template.For(42);
        Assert.Equal("api/input/42", url);
    }

    [Fact]
    public void UrlForTwoParametersWorksCorrectly()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}");
        var url = template.For(42, "John Doe");
        Assert.Equal("api/input/42/John%20Doe", url);
    }

    [Fact]
    public void UrlForThreeParametersWorksCorrectly()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}/{active:bool}");
        var url = template.For(42, "John Doe", true);
        Assert.Equal("api/input/42/John%20Doe/True", url);
    }

    [Fact]
    public void UrlForThrowsWhenIncorrectNumberOfParameters()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}");
        Assert.Throws<ArgumentException>(() => template.For(42));
        Assert.Throws<ArgumentException>(() => template.For(42, "John", "Extra"));
    }

    [Fact]
    public void UrlForThrowsWhenIncorrectTypeProvided()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}");
        Assert.Throws<ArgumentException>(() => template.For("42", "John"));
        Assert.Throws<ArgumentException>(() => template.For(42, 42));
    }

    [Fact]
    public void UrlForWorksWithMultipleParameterTypes()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}/{active:bool}");
        var url = template.For(42, "John Doe", false);
        Assert.Equal("api/input/42/John%20Doe/False", url);
    }

    [Fact]
    public void ConstructorThrowsOnInvalidRouteTemplate()
    {
        Assert.Throws<ArgumentException>(() => RouteTemplate.Create("api/input/{id}"));
        Assert.Throws<ArgumentException>(() => RouteTemplate.Create("api/input/{id:float}"));
    }

    [Fact]
    public void UrlForHandlesSpecialCharactersInParameters()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{name:alpha}");
        var url = template.For(42, "John & Jane");
        Assert.Equal("api/input/42/John%20%26%20Jane", url);
    }

    [Fact]
    public void UrlForHandlesBooleanParameters()
    {
        var template = RouteTemplate.Create("api/input/{id:int}/{active:bool}");
        var url = template.For(42, true);
        Assert.Equal("api/input/42/True", url);
    }
}
