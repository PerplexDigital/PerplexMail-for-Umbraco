using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class Util
	{
		// cache the FIPS flag
		public static readonly bool AllowOnlyFipsAlgorithms = true; //performance is better with "true" in all scenarios. //System.Security.Cryptography.CryptoConfig.AllowOnlyFipsAlgorithms;

		// static reflection setter for fields
		public static Action<T, V> CreateSetter<T, V>(this FieldInfo field)
		{
			var targetExp = Expression.Parameter(typeof(T));
			var valueExp = Expression.Parameter(typeof(V));

			// Expression.Property can be used here as well
			var fieldExp = Expression.Field(targetExp, field);
			var assignExp = Expression.Assign(fieldExp, valueExp);

			var setter = Expression.Lambda<Action<T, V>>(assignExp, targetExp, valueExp).Compile();
			return setter;
		}

		public static readonly UTF8Encoding SafeUTF8 = new UTF8Encoding(false, true);

		public static bool Xor(byte[] a, byte[] b)
		{
			return Xor(a, 0, a.Length, b, 0, b.Length);
		}// Xor()

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static bool Xor(byte[] a, int aStart, int aCount, byte[] b, int bStart, int bCount)
		{
			int x = aCount ^ bCount;
			for (int i = 0; i < aCount; ++i)
			{
				x |= a[aStart + i] ^ b[bStart + i % bCount];
			}
			return x == 0;
		}// Xor()

	}// Util class
}//ns