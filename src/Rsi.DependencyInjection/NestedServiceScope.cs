using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Rsi.DependencyInjection
{
	internal class NestedServiceScope : IServiceScope
	{
		private static readonly AsyncLocal<Dictionary<IServiceProvider, NestedServiceScope>> _currentByRootServiceProvider = new AsyncLocal<Dictionary<IServiceProvider, NestedServiceScope>>();
		private static readonly AsyncLocal<Dictionary<IServiceCollection, NestedServiceScope>> _currentByRootServices = new AsyncLocal<Dictionary<IServiceCollection, NestedServiceScope>>();

		private bool _disposed;

		private IServiceCollection Services { get; }
		private NestedServiceScope ParentScope { get; }
		private IServiceScope ServiceScope { get; }
		private NestedServiceProvider NestedProvider { get; }

		private class NestedServiceProvider : IServiceProvider
		{
			public NestedServiceProvider(IServiceProvider rootServiceProvider, NestedServiceScope nestedServiceScope)
			{
				RootServiceProvider = rootServiceProvider;
				NestedServiceScope = nestedServiceScope;
			}
			
			public IServiceProvider RootServiceProvider { get; }
			
			public NestedServiceScope NestedServiceScope { get; }

			public object GetService(Type serviceType)
			{
				return NestedServiceScope.ServiceScope.ServiceProvider.GetService(serviceType);
			}
		}

		public NestedServiceScope(IServiceProvider serviceProvider, Action<IServiceCollection> servicesConfiguration)
		{
			if (serviceProvider is NestedServiceProvider nestedParentServiceProvider)
			{
				NestedProvider = new NestedServiceProvider(nestedParentServiceProvider.RootServiceProvider, this);
				ParentScope = nestedParentServiceProvider.NestedServiceScope;
			}
			else
			{
				NestedProvider = new NestedServiceProvider(serviceProvider, this);
				ParentScope = null;
			}

			PushNestedServiceScope();

			Services = new ServiceCollection();
			
			servicesConfiguration(Services);

			ServiceScope = serviceProvider.CreateScope();
		}
		
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				PopNestedServiceScope();
				ServiceScope.Dispose();
			}
		}

		public IServiceProvider ServiceProvider => NestedProvider;

		public ServiceDescriptor GetClosestServiceDefinition(Type serviceType)
		{
			var currentScope = this;
			ServiceDescriptor serviceDescriptor;
			do
			{
				serviceDescriptor = currentScope.Services.LastOrDefault(d => d.ServiceType == serviceType);
				currentScope = currentScope.ParentScope;
			} while (serviceDescriptor == null && currentScope != null);

			return serviceDescriptor;
		}

		public static NestedServiceScope GetCurrentScopeByServiceProvider(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));

			var rootServiceProvider = serviceProvider is NestedServiceProvider nestedServiceProvider
				? nestedServiceProvider.RootServiceProvider
				: serviceProvider;
			
			if (_currentByRootServiceProvider.Value == null || !_currentByRootServiceProvider.Value.TryGetValue(rootServiceProvider, out var currentScope))
				return null;

			return currentScope;
		}
		
		public static NestedServiceScope GetCurrentScopeByRootServices(IServiceCollection rootServices)
		{
			if (rootServices == null)
				throw new ArgumentNullException(nameof(rootServices));
			
			if (_currentByRootServices.Value == null || !_currentByRootServices.Value.TryGetValue(rootServices, out var currentScope))
				return null;

			return currentScope;
		}
		
		private void PushNestedServiceScope()
		{
			_currentByRootServiceProvider.Value ??= new Dictionary<IServiceProvider, NestedServiceScope>();
			_currentByRootServiceProvider.Value[NestedProvider.RootServiceProvider] = this;
			
			var rootServices = NestedProvider.RootServiceProvider.GetService<IServiceCollection>();
			_currentByRootServices.Value ??= new Dictionary<IServiceCollection, NestedServiceScope>();
			_currentByRootServices.Value[rootServices] = this;
		}

		private void PopNestedServiceScope()
		{
			var rootServices = NestedProvider.RootServiceProvider.GetService<IServiceCollection>();
			if (ParentScope == null)
			{
				_currentByRootServiceProvider.Value.Remove(NestedProvider.RootServiceProvider);
				_currentByRootServices.Value.Remove(rootServices);
			}
			else
			{
				_currentByRootServiceProvider.Value[NestedProvider.RootServiceProvider] = ParentScope;
				_currentByRootServices.Value[rootServices] = ParentScope;
			}
		}
	}
}