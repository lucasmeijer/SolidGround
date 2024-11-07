using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using Xunit;

public class InputEndPointTests : IntegrationTestBase
{
    [Fact]
    public async Task PostNewInput_BadRequest_Returns_BadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/input", new {});
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task PostNewInput_Returns_Created()
    {
        var response = await Client.PostAsJsonAsync("/api/input", SimpleDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/input/1", response.Headers.Location?.OriginalString);
    }
    
    [Fact]
    public async Task PostNewInputWithFormWithImages_ImagesGetStored()
    {
        var multipartContent = new MultipartFormDataContent("----WebKitFormBoundaryABC123")
        {
            { 
                new ByteArrayContent([1, 2, 3, 4, 5]) { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/octet-stream") } },
                "file1",
                "file1.txt"
            },
            {
                new StringContent("someValue"),
                "myString"
            }    
        };
        using var ms = new MemoryStream();
        await multipartContent.CopyToAsync(ms);
        
        var response = await Client.PostAsJsonAsync("/api/input", (InputDto)new()
        {
            Output = new()
            {
                OutputComponents = [],
                StringVariables = []
            },
            Request = new()
            {
                BasePath = "asd",
                BodyBase64 = Convert.ToBase64String(ms.ToArray()),
                ContentType = multipartContent.Headers.ContentType?.ToString() ?? throw new Exception(),
                Route = "/api/hello"
            }
        });
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/input/1", response.Headers.Location?.OriginalString);

        var input = await DbContext.Inputs
            .Include(i=>i.Files)
            .Include(i=>i.Strings)
            .SingleAsync();
        
        var file = input.Files.Single();
        Assert.Equivalent((byte[])[1,2,3,4,5], file.Bytes);

        var inputString = input.Strings.Single();
        Assert.Equal("myString", inputString.Name);
        Assert.Equal("someValue", inputString.Value);
    }
    
    [Fact]
    public async Task UpdateName_Updates_Name()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/input", SimpleDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdId = int.Parse(createResponse.Headers.Location!.ToString().Last().ToString());
        Assert.Equal(1, createdId);

        var input = await DbContext.Inputs.FindAsync(createdId);
        Assert.NotNull(input);
        Assert.Equal(null, input.Name);

        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/input/{createdId}/name", new InputEndPoints.NameUpdateDto()
        {
            Name = "hallo"
        })).StatusCode);

        await DbContext.Entry(input).ReloadAsync();
        Assert.Equal("hallo", input.Name);
    }

    static InputDto SimpleDto => new()
    {
        Output = new()
        {
            OutputComponents = [],
            StringVariables = []
        },
        Request = new()
        {
            BasePath = "asd",
            BodyBase64 = "asd",
            ContentType = "text/html",
            Route = "/api/hello"
        }
    };

    [Fact]
    public async Task PostToTags_Returns_Created()
    {
        var inputResponse = await Client.PostAsJsonAsync("/api/input", SimpleDto);
        inputResponse.EnsureSuccessStatusCode();
        var input = inputResponse.Headers.Location;
        var tagResponse = await Client.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "mytag"});
        tagResponse.EnsureSuccessStatusCode();
        var tag = tagResponse.Headers.Location;
        var response = await Client.PostAsJsonAsync($"{input}/tags", new InputEndPoints.AddTagToInputDto() { TagId = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/input/1/tags/1", response.Headers.Location?.OriginalString);
    }
    
    [Fact]
    public async Task PostNonExistingTag_Returns_NotFound()
    {
        var input = (await Client.PostAsJsonAsync("/api/input", SimpleDto)).Headers.Location;
        var response = await Client.PostAsJsonAsync($"{input}/tags", new InputEndPoints.AddTagToInputDto() { TagId = 123 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PostToNonExistingInput_Returns_NotFound()
    {
        await Client.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "mytag"});
        var response = await Client.PostAsJsonAsync($"/api/input/2/tags", new InputEndPoints.AddTagToInputDto() { TagId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteTagFromInput_Returns_Ok()
    {
        var httpClient = Client;
        
        var asJsonAsync = await httpClient.PostAsJsonAsync("/api/input", SimpleDto);
        var input = asJsonAsync.Headers.Location;
        var httpResponseMessage = await httpClient.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "mytag"});
        var tag = httpResponseMessage.Headers.Location;
        var postAsJsonAsync = await httpClient.PostAsJsonAsync($"{input}/tags", new InputEndPoints.AddTagToInputDto() { TagId = 1 });
        var tagOnInput = postAsJsonAsync.Headers.Location;
        
        var response = await httpClient.DeleteAsync(tagOnInput);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var readAsStringAsync = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(readAsStringAsync);
        Assert.Equal("input_1_tags", doc.Descendants("turbo-frame").Single().Attribute("id")?.Value);
    }
    
    [Fact]
    public async Task DeleteTagFromNonExistingInput_Returns_NotFound()
    {
        var tag = (await Client.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "mytag"})).Headers.Location;
        var response = await Client.DeleteAsync("/api/input/1/tags/1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteNonExistingTagFromInput_Returns_NotFound()
    {
        var input = (await Client.PostAsJsonAsync("/api/input", SimpleDto)).Headers.Location;
        var tag = (await Client.PostAsJsonAsync("/api/tags", new TagEndPoints.CreateTagDto() { Name = "mytag"})).Headers.Location;
        var response = await Client.DeleteAsync("/api/input/1/tags/1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}