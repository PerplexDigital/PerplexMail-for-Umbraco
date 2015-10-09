using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PerplexMail
{
    public static class Security
    {
        public enum EnmStrength
        {
            Numbers,
            Chars,
            CharsAndNumbers,
            CharsAndNumbersAndSpecialChars
        }

        /// <summary>
        /// Determines the keysize to be used when encrypting texts.
        /// </summary>
        const int Keysize = 256;

        /// <summary>
        /// This encryption key is used as the default when no key is sent with the encrypt/decrypt function calls
        /// </summary>
        static string DefaultEncryptionKey
        {
            get
            {
                string defaultKey = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_ENCRYPTION_PRIVATEKEY];
                if (String.IsNullOrEmpty(defaultKey))
                    defaultKey = "Nk1uST8Qa3i5hHzw1ZHYe";
                return defaultKey;
            }
        }

        /// <summary>
        /// This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        /// This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        /// 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        ///     
        /// This salt is used in combination with a private key to encrypt/decrypt sensitive data.
        /// 
        /// THE SALT SHOULD BE EXACTLY 32 CHARACTERS LONG!
        /// </summary>
        static byte[] InitVectorBytes
        {
            get
            {
                // Specified salt must be 32 characters long!
                var data = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_ENCRYPTION_VECTORBYTES];
                if (String.IsNullOrEmpty(data))
                    data = "PleaseConfigureSaltInTheWebConfi"; // This default should NOT be used! Configure your own web.config key
                if (data.Length != 32)
                    throw new InvalidOperationException("Web.config error (appSettings key '" + Constants.WEBCONFIG_SETTING_ENCRYPTION_VECTORBYTES + "'): Provided vector bytes must be exactly 32 characters long!");
                return Encoding.ASCII.GetBytes(data);
            }
        }

        /// <summary>
        /// Encrypt a string with a password. With PerplexLib.Security.Decrypt() you can decrypt the same string with the right password
        /// </summary>
        /// <param name="plaintext">The to encrypt text</param>
        /// <param name="key">The password, if null the default 'EncryptionKey' web.config key is used</param>
        /// <returns>The encrypted string</returns>
        public static string Encrypt(string plaintext, string key = null, string salt = null)
        {
            // Validate the input

            // Use a default key if none is present
            key = key ?? DefaultEncryptionKey;

            // This throws an exception if anything went wrong...
            ValidateEncryptDecryptInput(key);

            // Add the salt to our string
            if (!String.IsNullOrEmpty(plaintext) && !String.IsNullOrEmpty(salt))
                plaintext += salt;

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plaintext);
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(key, null))
            {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    symmetricKey.BlockSize = 256; // might take longer to encrypt
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, InitVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decode an encrypted string which was encoded with the method PerplexLib.Security.Encrypt
        /// </summary>
        /// <param name="ciphertext">De encrypted tekst</param>
        /// <param name="key">The password, if null the default 'EncryptionKey' web.config key is used</param>
        /// <returns>The original encrypted string</returns>
        public static string Decrypt(string ciphertext, string key = null, string salt = null)
        {
            // Just return the ciphertext if it doens't contain anything
            if (String.IsNullOrEmpty(ciphertext)) 
                return ciphertext;

            try
            {
            // Use a default key if none is present
            key = key ?? DefaultEncryptionKey;

            // This throws an exception if anything went wrong...
            ValidateEncryptDecryptInput(key);

            byte[] cipherTextBytes = Convert.FromBase64String(ciphertext);
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(key, null))
            {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    symmetricKey.BlockSize = 256; // might take longer to encrypt
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, InitVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                string result = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);

                                // Remove the salt from our string
                                if (!String.IsNullOrEmpty(result) && !String.IsNullOrEmpty(salt))
                                    result = result.Replace(salt, "");

                                return result;
                            }
                        }
                    }
                }
            }
            }
            catch (Exception)
            {
                return "Unable to decrypt: " + ciphertext;
            }
        }

        static void ValidateEncryptDecryptInput(string key)
        {
            // Check for a key
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("No encryption key passed and not set in the web.config, use EncryptionKey as the web.config key");

            // Check for minlength
            if (key.Length < 12)
                throw new ArgumentException("The encryption key is too short, minlength is 12");

            // Check the initvectorbytes
            var data = InitVectorBytes;
            if (data == null)
                throw new NullReferenceException("Vectorbytes are not specified!");
            if (data.Length != 32)
                throw new ArgumentException("The initVectorBytes should be 32 bytes long, no more no less");
        }

        /// <summary>
        /// Generates a strong password, minimal 1 digit, 1 capital, 1 small char and 1 special character 
        /// </summary>
        /// <param name="length">Length can be set</param>
        /// <returns>String</returns>
        /// <remarks></remarks>
        public static string GeneratePassword(int length, EnmStrength strength = EnmStrength.CharsAndNumbersAndSpecialChars)
        {
            //create constant strings for each type of characters
            string alphaCaps = "QWERTYUIOPASDFGHJKLZXCVBNM";
            string alphaLow = "qwertyuiopasdfghjklzxcvbnm";
            string numerics = "1234567890";
            string special = "@#$_-=^~";

            //create another string which is a concatenation of all above
            string allChars = string.Empty;

            switch (strength)
            {
                case EnmStrength.Chars:
                    allChars = alphaCaps + alphaLow;
                    break;
                case EnmStrength.CharsAndNumbers:
                    allChars = alphaCaps + alphaLow + numerics;
                    break;
                case EnmStrength.CharsAndNumbersAndSpecialChars:
                    allChars = alphaCaps + alphaLow + numerics + special;
                    break;
                case EnmStrength.Numbers:
                    allChars = numerics;
                    break;
            }

            var r = new RNGCryptoServiceProvider();
            String generatedPassword = "";
            for (int i = 0; i <= length - 1; i++)
            {
                byte[] result = new byte[8];
                r.GetBytes(result);
                double rand = (double)BitConverter.ToUInt64(result, 0) / ulong.MaxValue;

                if (i == 0 & (strength == EnmStrength.Chars | strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
                {
                    //First character is an upper case alphabet
                    generatedPassword += alphaCaps.ToCharArray()[Convert.ToInt32(Math.Floor(rand * alphaCaps.Length))];
                }
                else if (i == 1 & (strength == EnmStrength.Chars | strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
                {
                    //Second character is a lower case alphabet
                    generatedPassword += alphaLow.ToCharArray()[Convert.ToInt32(Math.Floor(rand * alphaLow.Length))];
                }
                else if (i == 2 & (strength == EnmStrength.CharsAndNumbersAndSpecialChars))
                {
                    //Third character is a special 
                    generatedPassword += special.ToCharArray()[Convert.ToInt32(Math.Floor(rand * special.Length))];
                }
                else if (i == 3 & (strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
                {
                    //Fourth character is a number 
                    generatedPassword += numerics.ToCharArray()[Convert.ToInt32(Math.Floor(rand * numerics.Length))];
                }
                else
                {
                    // rest is random
                    generatedPassword += allChars.ToCharArray()[Convert.ToInt32(Math.Floor(rand * allChars.Length))];
                }
            }
            return generatedPassword;
        }

        /// <summary>
        /// Performs a one-way transformation on a string, rendering the contents unreadable (forever!) but with a consistent output.
        /// Generally used for authentication purposes.
        /// </summary>
        /// <param name="stringToEncode">The string to encode</param>
        /// <returns>The transformed string</returns>
        public static string GenerateHash(string stringToEncode)
        {
            string hashKey = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_HASH_PRIVATEKEY];
            if (String.IsNullOrEmpty(hashKey))
                // Note: please specify your own key, or enter your key in the web.config under appSettings: <add key="PerplexMailHashKey" value"..." />
                hashKey = "Wz80pB0YKXSkxdK4"; 

            var sha = new System.Security.Cryptography.SHA512Managed();
            var e = new System.Text.ASCIIEncoding();
            var data = e.GetBytes(stringToEncode + hashKey);
            return Convert.ToBase64String(sha.ComputeHash(data));
        }

    }
}