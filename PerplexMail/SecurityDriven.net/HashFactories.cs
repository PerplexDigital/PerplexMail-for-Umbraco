using System;
using System.Security.Cryptography;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class HashFactories
	{
		static readonly Func<SHA1> ManagedSHA1 = () => new SHA1Managed();
		static readonly Func<SHA1> FipsSHA1 = () => new SHA1Cng();

		static readonly Func<SHA256> ManagedSHA256 = () => new SHA256Managed();
		static readonly Func<SHA256> FipsSHA256 = () => new SHA256Cng();

		static readonly Func<SHA512> ManagedSHA512 = () => new SHA512Managed();
		static readonly Func<SHA512> FipsSHA512 = () => new SHA512Cng();

		public static readonly Func<SHA1> SHA1 = Util.AllowOnlyFipsAlgorithms ? FipsSHA1 : ManagedSHA1;
		public static readonly Func<SHA256> SHA256 = Util.AllowOnlyFipsAlgorithms ? FipsSHA256 : ManagedSHA256;
		public static readonly Func<SHA512> SHA512 = Util.AllowOnlyFipsAlgorithms ? FipsSHA512 : ManagedSHA512;
	}// HashFactories class
}//ns