using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rsi.DependencyInjection
{
	/// <summary>
	/// Mock service registration extensions
	/// </summary>
	public static class ServiceProviderExtensions
	{
		/// <summary>
		/// Creates a new scope with mock service registrations
		/// </summary>
		/// <param name="serviceProvider">Current scope</param>
		/// <param name="servicesConfiguration">Mock services configuration delegate</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceScope CreateScope(this IServiceProvider serviceProvider, Action<IServiceCollection> servicesConfiguration)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));
			
			return new NestedServiceScope(serviceProvider, servicesConfiguration);
		}

		/// <summary>
		/// Returns the current service provider with mock service registrations for the specified test host service provider
		/// </summary>
		/// <param name="serviceProvider">Test host service provider</param>
		/// <returns></returns>
		public static IServiceProvider GetCurrentScopeServiceProvider(this IServiceProvider serviceProvider)
		{
			var currentScope = NestedServiceScope.GetCurrentScopeByServiceProvider(serviceProvider);
			return currentScope == null ? serviceProvider : currentScope.ServiceProvider;
		}
		
		internal static object CreateInstance(this IServiceProvider serviceProvider, ServiceDescriptor descriptor)
		{
			if (descriptor.ImplementationInstance != null)
				return descriptor.ImplementationInstance;

			if (descriptor.ImplementationFactory != null)
				return descriptor.ImplementationFactory(serviceProvider);

			return ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, descriptor.ImplementationType);
		}
	}
}