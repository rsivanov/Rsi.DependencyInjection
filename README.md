# Rsi.DependencyInjection

![Build](https://github.com/rsivanov/Rsi.DependencyInjection/workflows/Build%20&%20test%20&%20publish%20Nuget/badge.svg?branch=master)
[![NuGet](https://img.shields.io/nuget/dt/Rsi.DependencyInjection)](https://www.nuget.org/packages/Rsi.DependencyInjection) 
[![NuGet](https://img.shields.io/nuget/v/Rsi.DependencyInjection)](https://www.nuget.org/packages/Rsi.DependencyInjection)

This project implements a couple of extension methods for [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) to allow mocking services inside unit or integration tests without rebuilding the DI container.

Why it matters? Imagine that you have several hundred or even thousands of integration tests where you mock services using [ConfigureTestServices](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1#inject-mock-services). In every such test you'll have to rebuild the container completely and it'll take a significant amount of time.

You can cut the total test running time by an order of magnitude, if you build the DI container only once.

Inspired by
===
I've been using [Autofac](https://autofac.org/) for quite some time before moving to [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) as my DI container of choice. In Autofac they have the ability to [add service registrations on the fly](https://autofaccn.readthedocs.io/en/latest/lifetime/working-with-scopes.html#adding-registrations-to-a-lifetime-scope) without rebuilding the container.
```csharp
using(var scope = container.BeginLifetimeScope(
  builder =>
  {
    builder.RegisterType<Override>().As<IService>();
    builder.RegisterModule<MyModule>();
  }))
{
  // The additional registrations will be available
  // only in this lifetime scope.
}
```
I moved from Autofac to [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) to get [a much faster DI container](https://github.com/danielpalme/IocPerformance), but an outstanding runtime performance of [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) unfortunately leads to slow integration testing due to the static nature of MS DI container - once it's built, you can't easily change it for mocking purposes.

And that's when I thought, what if we could have a similar extension method to change service registrations after the container is built.

How to use
==
Inside your test host builder code add a call to DecorateServicesForTesting as the last one after completing all service registrations.
```csharp
services.DecorateServicesForTesting(t => t.IsMockable());
```
The only parameter to that method is a criteria - whether you going to mock a service type or not. An example from SampleWebApi:
```csharp
public static bool IsMockable(this Type t)
{
    return t.IsFromOurAssemblies() ||
           t.IsOptionType() && t.GenericTypeArguments[0].IsFromOurAssemblies();
}

private static bool IsFromOurAssemblies(this Type t)
{
    return t.Assembly.GetName().Name.StartsWith("Rsi.");
}

private static bool IsOptionType(this Type t)
{
    return t.IsClosedTypeOf(typeof(IConfigureOptions<>)) ||
           t.IsClosedTypeOf(typeof(IPostConfigureOptions<>)) ||
           t.IsClosedTypeOf(typeof(IOptions<>)) ||
           t.IsClosedTypeOf(typeof(IOptionsSnapshot<>)) ||
           t.IsClosedTypeOf(typeof(IOptionsMonitor<>));
}
```
Here we mock only service types from our assemblies and IOptions* closed-generic interfaces where option types come from our assemblies.

Then after building a test host only once for all tests (using XUnit FixtureCollection) you can mock service registrations locally inside your integration tests using another extension method CreateScope:
```csharp
[Fact]
public async Task GetSampleValue_WhenCalledWithMock_ReturnsMockValue()
{
    using var mockServiceScope = _testHostServiceProvider.CreateScope(mockServices =>
    {
        var mockSampleService = Substitute.For<ISampleService>();
        mockSampleService.GetSampleValue().ReturnsForAnyArgs("Mock value");
        mockServices.AddSingleton(_ => mockSampleService);
    });
    var sampleController = RestClient.For<ISampleController>(_testHost.HttpClient);
    
    var sampleValue = await sampleController.GetSampleValue();
    Assert.Equal("Mock value", sampleValue);
}
```
As you can see, it's similar to Autofac [BeginLifetimeScope](https://autofaccn.readthedocs.io/en/latest/lifetime/working-with-scopes.html#adding-registrations-to-a-lifetime-scope), but for [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1).

How it works
===
For all existing service registrations that meet a decoration criteria we change Singleton lifetimes to Scoped and replace service descriptors with a custom factory inside DecorateServicesForTesting. 
When you open a new scope with mock service registrations the factory uses that information when returns a service implementation inside a scope. 
```csharp
var newServices = new ServiceCollection();
foreach (var wrappedServiceDescriptor in services)
{
    Func<IServiceProvider, object> objectFactory = serviceProvider =>
    {
        var currentScope = NestedServiceScope.GetCurrentScopeByRootServices(services);
        if (currentScope == null)
            return serviceProvider.CreateInstance(wrappedServiceDescriptor);

        var serviceDescriptor =
            currentScope.GetClosestServiceDefinition(wrappedServiceDescriptor.ServiceType);
        return serviceDescriptor == null
            ? serviceProvider.CreateInstance(wrappedServiceDescriptor)
            : serviceProvider.CreateInstance(serviceDescriptor);
    };

    if (decorationCriteria(wrappedServiceDescriptor.ServiceType))
    {
        //We can't mock classes and open-generic interfaces
        if (wrappedServiceDescriptor.ServiceType.IsClass ||
            wrappedServiceDescriptor.ServiceType.IsGenericType &&
            wrappedServiceDescriptor.ServiceType.ContainsGenericParameters)
        {
            newServices.Add(wrappedServiceDescriptor);
        }
        else
        {
            //We change decorated services Singleton lifetimes to Scoped to make mocked registrations local
            //to a concrete mock scope
            var serviceLifetime = wrappedServiceDescriptor.Lifetime == ServiceLifetime.Singleton
                ? ServiceLifetime.Scoped
                : wrappedServiceDescriptor.Lifetime;
            newServices.Add(ServiceDescriptor.Describe(wrappedServiceDescriptor.ServiceType, objectFactory, serviceLifetime));
        }
    }
    else
    {
        newServices.Add(wrappedServiceDescriptor);
    }
}
services.Clear();
services.Add(newServices);
```
Known limitations
==
* We can only replace existing service registrations with mock ones and can't add any new service registrations that weren't present in the container before it was built. This is due to the static nature of MS DI container.
* We can't mock open-generic service registrations due to limitations of MS DI factory methods such as:
```csharp 
services.AddSingleton(typeof(IInterface<>), typeof(Implementation<>));
```
Just replace open-generic service registrations with closed-generic versions if you want to mock them.