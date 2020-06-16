using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rsi.DependencyInjection.SampleWebApi
{
	public class Startup
	{
		public virtual void ConfigureServices(IServiceCollection services)
		{
			var mvcBuilder = services.AddMvc();
			if (ApplicationPart != null)
			{
				mvcBuilder.PartManager.ApplicationParts.Clear();
				mvcBuilder.AddApplicationPart(ApplicationPart);
			}
			mvcBuilder.AddControllersAsServices();

			services.AddSingleton<ISampleService, SampleService>();
		}
		
		/// <summary>
		/// This is required for correct controllers registration, because test startup will be in a separate assembly
		/// </summary>
		protected virtual Assembly ApplicationPart => null;

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}