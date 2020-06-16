using System;
using System.Linq;

namespace Rsi.DependencyInjection
{
	/// <summary>
	/// Reflection extensions for generic service registrations
	/// </summary>
	public static class TypeExtensions
	{
		/// <summary>
		/// Returns true in case when:
		/// - the <paramref name="openGeneric"/> is an open-generic interface and the <paramref name="type"/> implements its closed-generic version
		/// - the <paramref name="openGeneric"/> is an open-generic class and the <paramref name="type"/> is its closed-generic version 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="openGeneric"></param>
		/// <returns></returns>
		public static bool IsClosedTypeOf(this Type type, Type openGeneric)
		{
			if (openGeneric.IsInterface)
			{
				return type.GetInterfaces()
					.Union(type.IsInterface ? new [] { type } : Array.Empty<Type>())
					.Any(t => t.IsGenericType && !t.ContainsGenericParameters && t.GetGenericTypeDefinition() == openGeneric);
			}

			return type.IsGenericType && !type.ContainsGenericParameters &&
			       type.GetGenericTypeDefinition() == openGeneric;
		}		
	}
}