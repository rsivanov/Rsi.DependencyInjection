using System.Threading.Tasks;
using RestEase;

namespace Rsi.DependencyInjection.SampleWebApi.Tests
{
	public interface ISampleController
	{
		[Get("sample")]
		Task<string> GetSampleValue();
	}
}