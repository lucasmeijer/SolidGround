using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ServiceProviderExtensionsTests
{
    [Fact]
    public async Task InvokeInjected_WithSynchronousDelegate_ReturnsCorrectResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();

        var invoker = ServiceProviderHelper.CompileInjectionFor<int>((ITestService service) => service.GetValue());

        // Act
        var result = await invoker(serviceProvider);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task InvokeInjected_WithAsynchronousDelegate_ReturnsCorrectResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();

        var invoker = ServiceProviderHelper.CompileInjectionFor<int>((ITestService service) => service.GetValueAsync());

        // Act
        var result = await invoker(serviceProvider);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void InvokeInjected_WithNonMathcingReturnValue_Throws()
    {
        Delegate voidReturningDelegate = () => { return "123"; };
        Assert.Throws<ArgumentException>(() => ServiceProviderHelper.CompileInjectionFor<int>(voidReturningDelegate));
    }

    [Fact]
    public async Task InvokeInjected_WithMultipleParameters_InjectsAllCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<IAnotherService, AnotherService>();
        var serviceProvider = services.BuildServiceProvider();

        Delegate d = (ITestService service1, IAnotherService service2) => service1.GetValue() + service2.GetValue();
        var result = await ServiceProviderHelper.CompileInjectionFor<int>(d).Invoke(serviceProvider);

        // Assert
        Assert.Equal(52, result);
    }
    
    
    
    public interface ITestService
    {
        int GetValue();
        Task<int> GetValueAsync();
        void DoSomething();
    }

    public class TestService : ITestService
    {
        public int GetValue() => 42;
        public Task<int> GetValueAsync() => Task.FromResult(42);
        public void DoSomething() { }
    }

    public interface IAnotherService
    {
        int GetValue();
    }

    public class AnotherService : IAnotherService
    {
        public int GetValue() => 10;
    }

}
