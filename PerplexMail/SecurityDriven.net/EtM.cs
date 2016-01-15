using System;
using System.IO;
using System.Security.Cryptography;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class EtM
	{
		static readonly Func<Aes> aesFactory = AesFactories.Aes;
		static readonly Func<HMAC> hmacFactory = HMACFactories.HMACSHA512;

		static readonly Aes modelAES = aesFactory();
		static readonly HMAC modelHMAC = hmacFactory();

		static readonly int aesIVLength = modelAES.BlockSize / 8;
		static readonly int macLength = Math.Min(128, modelHMAC.HashSize) / 8; // set upper-limit on mac digest length

		static readonly int encKeyLength = modelAES.KeySize / 8;
		static readonly int macKeyLength = Math.Max(256, modelHMAC.HashSize - modelAES.KeySize) / 8; // set lower-limit on mac key length

		static readonly int minCiphertextLength = aesIVLength + macLength;

		public static byte[] Encrypt(byte[] masterKey, byte[] plaintext, byte[] salt = null)
		{
			byte[] encKey, macKey;
			using (var hkdf = new HKDF(hmacFactory, masterKey, salt))
			{
				macKey = hkdf.GetBytes(macKeyLength);
				encKey = hkdf.GetBytes(encKeyLength);
			}

			using (var aes = aesFactory())
			{
				aes.Key = encKey;
				var iv = aes.IV; // generates new IV

				using (var stream = new MemoryStream())
				{
					stream.Write(iv, 0, iv.Length);
					using (var aesEncryptor = aes.CreateEncryptor())
					{
						using (var cryptoStream = new CryptoStream(stream, aesEncryptor, CryptoStreamMode.Write))
						{
							cryptoStream.Write(plaintext, 0, plaintext.Length);
							cryptoStream.FlushFinalBlock();

							using (var hmac = hmacFactory())
							{
								hmac.Key = macKey;
								var fullmac = hmac.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);
								stream.Write(fullmac, 0, macLength);
								return stream.ToArray();
							}// using hmac
						}// using cryptoStream
					}// using aesEncryptor
				}// using stream
			}// using aes
		}// Encrypt()

		public static byte[] Decrypt(byte[] masterKey, byte[] ciphertext, byte[] salt = null)
		{
			int cipherLength = ciphertext.Length - minCiphertextLength;
			if (cipherLength <= 0) return null;
			int ivcipherLength = aesIVLength + cipherLength;

			byte[] encKey, macKey;
			using (var hkdf = new HKDF(hmacFactory, masterKey, salt))
			{
				macKey = hkdf.GetBytes(macKeyLength);
				encKey = hkdf.GetBytes(encKeyLength);
			}

			using (var aes = aesFactory())
			{
				aes.Key = encKey;
				using (var hmac = hmacFactory())
				{
					hmac.Key = macKey;
					var fullmacActual = hmac.ComputeHash(ciphertext, 0, ivcipherLength);
					if (!Util.Xor(fullmacActual, 0, macLength, ciphertext, ivcipherLength, macLength)) return null;

					var iv = new byte[aesIVLength];
					Buffer.BlockCopy(ciphertext, 0, iv, 0, aesIVLength);
					aes.IV = iv;

					using (var stream = new MemoryStream())
					{
						using (var aesDecryptor = aes.CreateDecryptor())
						{
							using (var cryptoStream = new CryptoStream(stream, aesDecryptor, CryptoStreamMode.Write))
							{
								cryptoStream.Write(ciphertext, aesIVLength, cipherLength);
							}// using cryptoStream
						}// using aesDecryptor
						return stream.ToArray();
					}// using stream
				}// using hmac
			}// using aes
		}// Decrypt()

	}//class EtM
}//ns