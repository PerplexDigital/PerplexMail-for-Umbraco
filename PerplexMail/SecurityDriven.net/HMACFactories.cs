using System;
using System.Security.Cryptography;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class HMACFactories
	{
		public static readonly Func<HMAC> HMACSHA1 = () => new HMAC2(HashFactories.SHA1);
		public static readonly Func<HMAC> HMACSHA256 = () => new HMAC2(HashFactories.SHA256);
		public static readonly Func<HMAC> HMACSHA512 = () => new HMAC2(HashFactories.SHA512);
	}// HMACFactories class
}
