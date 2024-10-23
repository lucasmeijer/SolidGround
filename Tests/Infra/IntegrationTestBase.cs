using Microsoft.EntityFrameworkCore;
using SolidGround;
using Xunit;

public abstract class IntegrationTestBase(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client = factory.CreateClient();
    
    protected AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        return new AppDbContext(optionsBuilder.Options);
    }
}