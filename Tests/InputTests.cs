using System.Net;
using Xunit;

public class InputTests(CustomWebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetNonExistingInput_Returns_404()
    {
        await using var context = CreateDbContext();
        var response = await _client.GetAsync("/api/input/3");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}