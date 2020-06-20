using System.Threading.Tasks;
using RestEase;

namespace Rsi.DependencyInjection.Benchmark
{
	public interface ISampleController
	{
		[Get("sample")]
		Task<string> GetSampleValue();
	}
}