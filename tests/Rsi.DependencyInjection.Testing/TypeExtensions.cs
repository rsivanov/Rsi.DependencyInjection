using System;
using Microsoft.Extensions.Options;

namespace Rsi.DependencyInjection.Testing
{
	public static class TypeExtensions
	{
		public static bool IsMockable(this Type t)
		{
			return t.IsFromOurAssemblies() ||
			       t.IsOptionType() && t.GenericTypeArguments[0].IsFromOurAssemblies();
		}

		private static bool IsFromOurAssemblies(this Type t)
		{
			return t.Assembly.GetName().Name.StartsWith("Rsi.");
		}

		private static bool IsOptionType(this Type t)
		{
			return t.IsClosedTypeOf(typeof(IConfigureOptions<>)) ||
			       t.IsClosedTypeOf(typeof(IPostConfigureOptions<>)) ||
			       t.IsClosedTypeOf(typeof(IOptions<>)) ||
			       t.IsClosedTypeOf(typeof(IOptionsSnapshot<>)) ||
			       t.IsClosedTypeOf(typeof(IOptionsMonitor<>));
		}		
	}
}