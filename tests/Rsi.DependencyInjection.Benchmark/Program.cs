using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Rsi.DependencyInjection.Benchmark
{
	class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<TestHostBenchmarks>(
				DefaultConfig
					.Instance
					.AddJob(Job.InProcess.WithGcServer(true))
			);;
		}
	}
}