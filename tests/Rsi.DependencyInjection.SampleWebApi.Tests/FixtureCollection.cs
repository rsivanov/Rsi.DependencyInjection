using Rsi.DependencyInjection.Testing;
using Xunit;

namespace Rsi.DependencyInjection.SampleWebApi.Tests
{
	[CollectionDefinition(nameof(FixtureCollection))]
	public class FixtureCollection :
		ICollectionFixture<TestHost<TestStartup>>
	{
	}
}