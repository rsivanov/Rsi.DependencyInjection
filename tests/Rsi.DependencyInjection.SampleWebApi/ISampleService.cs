using System.Threading.Tasks;

namespace Rsi.DependencyInjection.SampleWebApi
{
	public interface ISampleService
	{
		Task<string> GetSampleValue();
	}
}