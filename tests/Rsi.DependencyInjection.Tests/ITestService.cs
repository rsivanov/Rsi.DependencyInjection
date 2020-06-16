﻿namespace Rsi.DependencyInjection.Tests
{
	public interface ITestService<T>
	{
		string GetName();
	}
}