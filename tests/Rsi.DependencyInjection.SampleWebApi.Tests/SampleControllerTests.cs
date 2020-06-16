using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestEase;
using Rsi.DependencyInjection.Testing;
using Xunit;

namespace Rsi.DependencyInjection.SampleWebApi.Tests
{
	[Collection(nameof(FixtureCollection))]
	public class SampleControllerTests
	{
		private readonly TestHost<TestStartup> _testHost;

		public SampleControllerTests(TestHost<TestStartup> testHost)
		{
			_testHost = testHost;
		}

		[Fact]
		public async Task GetSampleValue_WhenCalledWithoutMocks_ReturnsInitialValue()
		{
			var sampleController = RestClient.For<ISampleController>(_testHost.HttpClient);
			
			var sampleValue = await sampleController.GetSampleValue();
			Assert.Equal("To get this value we had to do some really hard work", sampleValue);
		}

		[Fact]
		public async Task GetSampleValue_WhenCalledWithMock_ReturnsMockValue()
		{
			using var mockServiceScope = _testHost.RootScope.CreateScope(mockServices =>
			{
				var mockSampleService = Substitute.For<ISampleService>();
				mockSampleService.GetSampleValue().ReturnsForAnyArgs("Mock value");
				mockServices.AddSingleton(_ => mockSampleService);
			});
			var sampleController = RestClient.For<ISampleController>(_testHost.HttpClient);
			
			var sampleValue = await sampleController.GetSampleValue();
			Assert.Equal("Mock value", sampleValue);
		}
	}
}