
//Implement both the testsuite and implementation.
//
// I want to do new string PromptBuilder(mystring).  the string I pass in is a prompt template.
//it will have strings like **COMMMAND** or **TRANSCRIPT**.  the constructor should find all these variables
//that have to be filled in.  there should be a .Set("TRANSCRIPT", "the docter said blab lab blaa") method where
//the user can provide values. there should be a .Build() that returns the final replaced stirng, that throws
//an exception if there was a variable not yet provided.  it's also important that we only replace variables that
//were present in the original template,  not ones that might accidentally be present in an injected value through Set()

using SolidGroundClient;
using Xunit;

namespace Tests;

public class PromptBuilderTests
{
    [Fact]
    public void BasicReplacement_Works()
    {
        var builder = new PromptBuilder("Hello **NAME**!");
        var result = builder.Add("NAME", "World").Build();
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void MultipleVariables_WorkCorrectly()
    {
        var builder = new PromptBuilder("**GREETING** **NAME**! How is **LOCATION**?");
        var result = builder
            .Add("GREETING", "Hello")
            .Add("NAME", "John")
            .Add("LOCATION", "London")
            .Build();
            
        Assert.Equal("Hello John! How is London?", result);
    }

    [Fact]
    public void MissingVariable_ThrowsException()
    {
        var builder = new PromptBuilder("Hello **NAME** from **LOCATION**!");
        builder.Add("NAME", "John");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("LOCATION", exception.Message);
    }

    [Fact]
    public void UnknownVariable_ThrowsException()
    {
        var builder = new PromptBuilder("Hello **NAME**!");

        var exception = Assert.Throws<ArgumentException>(() => 
            builder.Add("UNKNOWN", "Value"));
        Assert.Contains("UNKNOWN", exception.Message);
    }

    [Fact]
    public void NestedVariablesInValues_AreNotReplaced()
    {
        var builder = new PromptBuilder("Hello **NAME**!");
        var result = builder.Add("NAME", "**OTHER**").Build();
            
        Assert.Equal("Hello **OTHER**!", result);
    }

    [Fact]
    public void EmptyTemplate_WorksCorrectly()
    {
        var builder = new PromptBuilder("");
        var result = builder.Build();
        Assert.Equal("", result);
    }
}