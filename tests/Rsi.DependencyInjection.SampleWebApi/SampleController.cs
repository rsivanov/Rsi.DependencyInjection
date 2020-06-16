using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Rsi.DependencyInjection.SampleWebApi
{
	[Route("[controller]")]
	public class SampleController : Controller
	{
		private readonly ISampleService _sampleService;

		public SampleController(ISampleService sampleService)
		{
			_sampleService = sampleService;
		}
		
		[HttpGet]
		public Task<string> GetSampleValue()
		{
			return _sampleService.GetSampleValue();
		}
	}
}