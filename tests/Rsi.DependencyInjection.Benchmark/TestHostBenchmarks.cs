using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestEase;
using Rsi.DependencyInjection.SampleWebApi;
using Rsi.DependencyInjection.Testing;
using Xunit;

namespace Rsi.DependencyInjection.Benchmark
{
	public class TestHostBenchmarks
	{
		private static readonly WebApplicationFactory<Startup> Factory = new WebApplicationFactory<Startup>();
		
		private static readonly HttpClient Client;
		private static readonly IServiceProvider RootScope;

		static TestHostBenchmarks()
		{
			var testServer = Factory.WithWebHostBuilder(builder =>
			{
				builder.ConfigureServices(services => services.DecorateServicesForTesting(t => t.IsMockable()));
			}).Server;
			testServer.PreserveExecutionContext = true;
			
			Client = testServer.CreateClient();
			RootScope = testServer.Services.CreateScope().ServiceProvider;
		}
		
		[Benchmark]
		public async Task ConfigureTestServices()
		{
			var client = Factory.WithWebHostBuilder(builder =>
				{
					builder.ConfigureTestServices(mockServices =>
					{
						var mockSampleService = Substitute.For<ISampleService>();
						mockSampleService.GetSampleValue().ReturnsForAnyArgs("Mock value");
						mockServices.AddSingleton(_ => mockSampleService);					
					});
				})
				.CreateClient();
			
			var sampleController = RestClient.For<ISampleController>(client);
			
			var sampleValue = await sampleController.GetSampleValue();
			Assert.Equal("Mock value", sampleValue);
		}

		[Benchmark]
		public async Task CreateScope()
		{
			using var mockServiceScope = RootScope.CreateScope(mockServices =>
			{
				var mockSampleService = Substitute.For<ISampleService>();
				mockSampleService.GetSampleValue().ReturnsForAnyArgs("Mock value");
				mockServices.AddSingleton(_ => mockSampleService);
			});
			var sampleController = RestClient.For<ISampleController>(Client);
			
			var sampleValue = await sampleController.GetSampleValue();
			Assert.Equal("Mock value", sampleValue);			
		}
	}
}