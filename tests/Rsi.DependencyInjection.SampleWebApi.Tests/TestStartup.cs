using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Rsi.DependencyInjection.SampleWebApi.Tests
{
	public class TestStartup : Startup
	{
		protected override Assembly ApplicationPart => typeof(Startup).Assembly;

		public override void ConfigureServices(IServiceCollection services)
		{
			base.ConfigureServices(services);
			//We could add some global mocks or registrations here to be available for all tests
		}
	}
}