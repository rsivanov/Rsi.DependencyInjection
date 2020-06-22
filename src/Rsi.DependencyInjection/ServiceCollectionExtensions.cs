using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Rsi.DependencyInjection
{
	/// <summary>
	///  Mock service registration extensions
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Decorate service registrations to allow local mock service registrations inside tests without rebuilding the DI-container
		/// This method should be called the last for the test host after all required service configuration calls.
		/// That's the reason why it returns void and not IServiceCollection, there aren't supposed to be any additional calls after that.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="decorationCriteria">The criteria whether the given service should be decorated for possible local mock registrations</param>
		public static void DecorateServicesForTesting(this IServiceCollection services, Func<Type, bool> decorationCriteria)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			
			if (decorationCriteria == null)
				throw new ArgumentNullException(nameof(decorationCriteria));

			//Register root service collection to be able to determine the current mock scope later on
			services.AddSingleton(services);

			services.Remove(new ServiceDescriptor(typeof(IOptions<>), typeof(OptionsManager<>),
				ServiceLifetime.Singleton));
			services.Remove(new ServiceDescriptor(typeof(IOptionsSnapshot<>), typeof(OptionsManager<>),
				ServiceLifetime.Scoped));
			services.Remove(new ServiceDescriptor(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>),
				ServiceLifetime.Singleton));

			var configureOptions = services.Where(s => s.ServiceType.IsClosedTypeOf(typeof(IConfigureOptions<>)))
				.ToArray();

			var existingOptionsRegistrations = services.Where(s => s.ServiceType.IsClosedTypeOf(typeof(IOptions<>))).ToArray();
			var existingOptionsSnapshotRegistrations = services.Where(s => s.ServiceType.IsClosedTypeOf(typeof(IOptionsSnapshot<>))).ToArray();
			var existingOptionsMonitorRegistrations = services.Where(s => s.ServiceType.IsClosedTypeOf(typeof(IOptionsMonitor<>))).ToArray();
			foreach (var configureOption in configureOptions)
			{
				var optionsType = configureOption.ServiceType.GenericTypeArguments[0];

				var optionsClosedGenericType = typeof(IOptions<>).MakeGenericType(optionsType);
				var optionsSnapshotClosedGenericType = typeof(IOptionsSnapshot<>).MakeGenericType(optionsType);
				var optionsMonitorClosedGenericType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
				
				if (existingOptionsRegistrations.All(s => s.ServiceType != optionsClosedGenericType))
					services.AddSingleton(optionsClosedGenericType,
						typeof(OptionsManager<>).MakeGenericType(optionsType));
				
				if (existingOptionsSnapshotRegistrations.All(s => s.ServiceType != optionsSnapshotClosedGenericType))
					services.AddScoped(optionsSnapshotClosedGenericType,
					typeof(OptionsManager<>).MakeGenericType(optionsType));
				
				if (existingOptionsMonitorRegistrations.All(s => s.ServiceType != optionsMonitorClosedGenericType))
					services.AddSingleton(optionsMonitorClosedGenericType,
						typeof(OptionsMonitor<>).MakeGenericType(optionsType));
			}

			var newServices = new ServiceCollection();
			foreach (var wrappedServiceDescriptor in services)
			{
				Func<IServiceProvider, object> objectFactory = serviceProvider =>
				{
					var currentScope = NestedServiceScope.GetCurrentScopeByRootServices(services);
					if (currentScope == null)
						return serviceProvider.CreateInstance(wrappedServiceDescriptor);

					var serviceDescriptor =
						currentScope.GetClosestServiceDefinition(wrappedServiceDescriptor.ServiceType);
					return serviceDescriptor == null
						? serviceProvider.CreateInstance(wrappedServiceDescriptor)
						: serviceProvider.CreateInstance(serviceDescriptor);
				};

				if (decorationCriteria(wrappedServiceDescriptor.ServiceType))
				{
					var serviceLifetime = wrappedServiceDescriptor.Lifetime == ServiceLifetime.Singleton
						? ServiceLifetime.Scoped
						: wrappedServiceDescriptor.Lifetime;
					
					if (wrappedServiceDescriptor.ServiceType.IsClass ||
					    wrappedServiceDescriptor.ServiceType.IsGenericType &&
					    wrappedServiceDescriptor.ServiceType.ContainsGenericParameters)
					{
						if (wrappedServiceDescriptor.ImplementationInstance != null)
							newServices.Add(new ServiceDescriptor(wrappedServiceDescriptor.ServiceType, wrappedServiceDescriptor.ImplementationInstance));
						else if (wrappedServiceDescriptor.ImplementationFactory != null)
							newServices.Add(ServiceDescriptor.Describe(wrappedServiceDescriptor.ServiceType,
								wrappedServiceDescriptor.ImplementationFactory, serviceLifetime));
						else
							newServices.Add(ServiceDescriptor.Describe(wrappedServiceDescriptor.ServiceType,
								wrappedServiceDescriptor.ImplementationType, serviceLifetime));						
					}
					else
					{
						newServices.Add(ServiceDescriptor.Describe(wrappedServiceDescriptor.ServiceType, objectFactory, serviceLifetime));
					}
				}
				else
				{
					newServices.Add(wrappedServiceDescriptor);
				}
			}
			services.Clear();
			services.Add(newServices);
		}
	}
}