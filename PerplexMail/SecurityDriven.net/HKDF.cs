using System;
using System.Security.Cryptography;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal class HKDF : DeriveBytes
	{
		HMAC hmac;
		int hashLength;
		byte[] context;
		static readonly byte[] zeroByteArray = new byte[0];
		byte counter;
		byte[] k;
		int k_unused;

		public HKDF(Func<HMAC> hmacFactory, byte[] ikm, byte[] salt = null, byte[] context = null)
		{
			hmac = hmacFactory();
			hashLength = hmac.OutputBlockSize;
			hmac.Key = salt ?? new byte[hashLength];
			hmac.Key = hmac.ComputeHash(ikm); // re-keying hmac with PRK
			this.context = context;
			Reset();
		}

		public override void Reset()
		{
			k = zeroByteArray;
			k_unused = 0;
			counter = 1;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (hmac != null)
				hmac.Dispose();
		}

		public override byte[] GetBytes(int countBytes)
		{
			using (var okm = new System.IO.MemoryStream())
			{
				do
				{
					if (k_unused > 0)
					{
						var min = Math.Min(k_unused, countBytes);
						okm.Write(k, hashLength - k_unused, min);
						countBytes -= min;
						k_unused -= min;
					}

					if (countBytes == 0) break;
					var n = countBytes / hashLength;
					if (countBytes % hashLength > 0) ++n;
					using (var hmac_msg = new System.IO.MemoryStream())
					{
						for (var i = 1; i <= n; ++i)
						{
							hmac_msg.Write(k, 0, k.Length);
							if (context != null)
								hmac_msg.Write(context, 0, context.Length);

							hmac_msg.WriteByte(counter);
							checked { ++counter; };
							k = hmac.ComputeHash(hmac_msg.GetBuffer(), 0, (int)hmac_msg.Length);
							okm.Write(k, 0, i < n ? hashLength : countBytes);
							countBytes -= hashLength;
							hmac_msg.SetLength(0);
						}
						k_unused = -countBytes;
					}// using hmac_msg
				} while (false);
				return okm.ToArray();
			}// using okm
		}// GetBytes()
	}// HKDF class
}//ns