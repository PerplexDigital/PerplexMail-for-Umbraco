using System;
using System.Security.Cryptography;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class AesFactories
	{
		static readonly Func<Aes> ManagedAes = () => new AesManaged();
		static readonly Func<Aes> FipsAes = () => new AesCryptoServiceProvider();

		public static readonly Func<Aes> Aes = Util.AllowOnlyFipsAlgorithms ? FipsAes : ManagedAes;
	}//class AesFactories
}//ns