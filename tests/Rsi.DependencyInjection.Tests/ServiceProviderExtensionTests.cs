using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.Extensions;
using Rsi.DependencyInjection.Testing;
using Xunit;

namespace Rsi.DependencyInjection.Tests
{
	public class ServiceProviderExtensionTests
	{
		[Fact]
		public void CreateScope_WhenServiceReplaced_ReturnsMockService()
		{
			var services = new ServiceCollection();

			var rootService = new TestService<MarkerType>();

			services.AddTransient<ITestService<MarkerType>>(sp => rootService);
			services.AddTransient<ITestService<string>, TestService<string>>();

			services.DecorateServicesForTesting(t => t.IsMockable());
			
			var rootServiceProvider = services.BuildServiceProvider();

			var currentScopeServiceProvider = rootServiceProvider.GetCurrentScopeServiceProvider();

			Assert.Same(currentScopeServiceProvider, rootServiceProvider);

			var nestedService = new TestService<MarkerType>();
			using (var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
				mockServices.AddTransient<ITestService<MarkerType>>(sp => nestedService);
			}))
			{
				var resolvedService = nestedScope.ServiceProvider.GetService<ITestService<MarkerType>>();
				
				Assert.Same(nestedService, resolvedService);
				
				currentScopeServiceProvider = rootServiceProvider.GetCurrentScopeServiceProvider();
				
				Assert.NotSame(rootServiceProvider, nestedScope.ServiceProvider);
				
				Assert.Same(currentScopeServiceProvider, nestedScope.ServiceProvider);
			}
			
			currentScopeServiceProvider = rootServiceProvider.GetCurrentScopeServiceProvider();
			Assert.Same(currentScopeServiceProvider, rootServiceProvider);
		}

		[Fact]
		public void CreateScope_WhenServiceNotReplaced_ReturnsInitialService()
		{
			var services = new ServiceCollection();

			var initialService = new TestService<MarkerType>();

			services.AddTransient<ITestService<MarkerType>>(sp => initialService);
			services.AddTransient<ITestService<string>, TestService<string>>();

			services.DecorateServicesForTesting(t => t.IsMockable());
			var rootServiceProvider = services.BuildServiceProvider();

			using var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
			});
			var resolvedService = nestedScope.ServiceProvider.GetService<ITestService<MarkerType>>();
			Assert.Same(initialService, resolvedService);
		}

		[Fact]
		public void CreateScope_WhenOptionsReplaced_ReturnsMockOptions()
		{
			var services = new ServiceCollection();

			services.Configure<TestOptions>(options => options.Name = "Before");
			
			services.DecorateServicesForTesting(t => t.IsMockable());
			var rootServiceProvider = services.BuildServiceProvider();

			using var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
				var testOptionsMock = Substitute.For<IOptions<TestOptions>>();
				testOptionsMock.ReturnsForAll(new TestOptions()
				{
					Name = "After"
				});

				mockServices.AddScoped(_ => testOptionsMock);
			});
			var testOptions = nestedScope.ServiceProvider.GetService<IOptions<TestOptions>>();
			Assert.Equal("After", testOptions.Value.Name);
		}

		[Fact] 
		public void CreateScope_WhenOptionsNotReplaced_ReturnsInitialOptions()
		{
			var services = new ServiceCollection();

			services.Configure<TestOptions>(options => options.Name = "Before");
			
			services.DecorateServicesForTesting(t => t.IsMockable());
			var rootServiceProvider = services.BuildServiceProvider();

			using var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
			});
			var testOptions = nestedScope.ServiceProvider.GetService<IOptions<TestOptions>>();
			Assert.Equal("Before", testOptions.Value.Name);
		}

		[Fact]
		public void CreateScope_WhenServiceReplacedOnTheSecondLevel_ReturnsMockServiceFromTheSecondLevel()
		{
			var services = new ServiceCollection();

			var rootService = new TestService<MarkerType>();

			services.AddTransient<ITestService<MarkerType>>(sp => rootService);
			services.AddTransient<ITestService<string>, TestService<string>>();

			services.DecorateServicesForTesting(t => t.IsMockable());
			
			var rootServiceProvider = services.BuildServiceProvider();

			var nestedService = new TestService<MarkerType>();
			var secondLevelNestedService = new TestService<MarkerType>();
			using var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
				mockServices.AddTransient<ITestService<MarkerType>>(_ => nestedService);
			});
			using var secondLevelScope = nestedScope.ServiceProvider.CreateScope(secondLevelMockServices =>
			{
				secondLevelMockServices.AddTransient<ITestService<MarkerType>>(_ => secondLevelNestedService);
			});
			var resolvedService = secondLevelScope.ServiceProvider.GetService<ITestService<MarkerType>>();
				
			Assert.Same(secondLevelNestedService, resolvedService);
		}

		[Fact]
		public void CreateScope_WhenServiceNotReplacedOnTheSecondLevel_ReturnsMockServiceFromTheFirstLevel()
		{
			var services = new ServiceCollection();

			var rootService = new TestService<MarkerType>();

			services.AddTransient<ITestService<MarkerType>>(sp => rootService);
			services.AddTransient<ITestService<string>, TestService<string>>();

			services.DecorateServicesForTesting(t => t.IsMockable());
			
			var rootServiceProvider = services.BuildServiceProvider();

			var nestedService = new TestService<MarkerType>();
			using var nestedScope = rootServiceProvider.CreateScope(mockServices =>
			{
				mockServices.AddTransient<ITestService<MarkerType>>(_ => nestedService);
			});
			using var secondLevelScope = nestedScope.ServiceProvider.CreateScope(secondLevelMockServices =>
			{
			});
			var resolvedService = secondLevelScope.ServiceProvider.GetService<ITestService<MarkerType>>();
				
			Assert.Same(nestedService, resolvedService);
		}
	}
}