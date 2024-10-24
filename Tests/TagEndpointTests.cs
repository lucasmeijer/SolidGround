using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using Xunit;
using Xunit.Abstractions;

public class TagEndPointTests : IntegrationTestBase
{
    [Fact]
    public async Task PostNewTag_Returns_Created()
    {
        var response = await Client.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "harry"});
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task PostDuplicateTag_Returns_Conflict()
    {
        var createTagDto = new TagEndPoints.CreateTagDto() { Name = "harry"};
        var response1 = await Client.PostAsJsonAsync("/api/tags", createTagDto);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var response2 = await Client.PostAsJsonAsync("/api/tags", createTagDto);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }
    
    [Fact]
    public async Task DeleteTag_Returns_Ok()
    {
        await PostNewTag_Returns_Created();
        var response = await Client.DeleteAsync("/api/tags/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var doc = XDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("refresh", doc.Descendants("turbo-stream").Single().Attribute("action")?.Value);
    }
    
    // [Fact]
    // public async Task DeleteTagThroughPostWithUnderscoreMethod_Returns_NoContent()
    // {
    //     await PostNewTag_Returns_Created();
    //     var response = await Client.PostAsync("/api/tags/1", new FormUrlEncodedContent([new("_method","delete")]));
    //     Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    // }
}