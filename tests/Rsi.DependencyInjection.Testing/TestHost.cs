using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rsi.DependencyInjection.Testing
{
	public class TestHost<TStartup> : IDisposable
		where TStartup: class
	{
		private readonly TestServer _testServer;
		private readonly HttpClient _testClient;
		private readonly IServiceScope _rootServiceScope;
		private readonly IServiceScope _hostServiceScope;
		private bool _disposed;
		
		public TestHost()
		{
			var host = Host.CreateDefaultBuilder()
				.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); })
				.ConfigureWebHost(webHostBuilder => webHostBuilder.UseTestServer())
				.ConfigureServices(ConfigureTestServices)
				.Build();
			host.Start();
			
			_hostServiceScope = host.Services.CreateScope();
			_testServer = (TestServer)_hostServiceScope.ServiceProvider.GetRequiredService<IServer>();
			//This is important for saving async context
			_testServer.PreserveExecutionContext = true;
			_testClient = _testServer.CreateClient();
			_rootServiceScope = _testServer.Services.CreateScope();
		}

		private void ConfigureTestServices(IServiceCollection services)
		{
			services.DecorateServicesForTesting(t => t.IsMockable());
		}
		
		public IServiceProvider RootScope => _rootServiceScope.ServiceProvider;

		public HttpClient HttpClient => _testClient;
		
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				_rootServiceScope?.Dispose();
				_testClient?.Dispose();
				_testServer?.Dispose();
				_hostServiceScope?.Dispose();
			}
		}
	}
}