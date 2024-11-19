using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using Xunit;

[UsedImplicitly]
class InMemoryDatabaseForTenant : IDatabaseConfigurationForTenant
{
    string _databaseName = Guid.NewGuid().ToString();
    
    public void Configure(DbContextOptionsBuilder options, Tenant? tenant)
    {
        options.UseInMemoryDatabase(databaseName: _databaseName);
    }
}

public abstract class IntegrationTestBase : IAsyncLifetime
{
    SolidGroundApplicationUnderTest WebApplicationUnderTest { get; set; } = null!;
    protected HttpClient Client => WebApplicationUnderTest.HttpClient;
    internal AppDbContext DbContext => WebApplicationUnderTest.DbContext;
    protected Tenant Tenant { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Tenant = new FlashCardsTenant();
        var webApplication = Program.CreateWebApplication<InMemoryDatabaseForTenant>([], Tenant);
        WebApplicationUnderTest = await SolidGroundApplicationUnderTest.StartAsync(webApplication, Tenant);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WebApplicationUnderTest.DisposeAsync();
    }
}