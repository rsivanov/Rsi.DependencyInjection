namespace Rsi.DependencyInjection.Tests
{
	public class TestService<T> : ITestService<T>
	{
		public string GetName() => typeof(T).FullName;
	}
}