using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using PerplexMail.SecurityDrivenDotNet;

namespace PerplexMail
{
    /// <summary>
    /// Available SHA hashing algorithms (SHA)
    /// </summary>
    public enum EnmHashingAlgorithm
    {
        /// <summary>
        /// Weak
        /// </summary>
        SHA1,
        /// <summary>
        /// Avarage
        /// </summary>
        SHA256,
        /// <summary>
        /// Strong
        /// </summary>
        SHA512,
    }

    /// <summary>
    /// Available keyed hashing algorithms (HMAC)
    /// </summary>
    public enum EnmSecureHashingAlgorithm
    {
        /// <summary>
        /// Weak
        /// </summary>
        HMACSHA1,
        /// <summary>
        /// Avarage
        /// </summary>
        HMACSHA256,
        /// <summary>
        /// Strong
        /// </summary>
        HMACSHA512,
    }

    /// <summary>
    /// This class manages all security related concerns for the PerplexMail package, such as encryption/decryption, hashing and password generation.
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// The minimum length of the secret key
        /// </summary>
        const int MIN_SECRETKEY_LENGTH = 16;

        /// <summary>
        /// The optimal (and minimum) salt lenght
        /// </summary>
        const int SALT_LENGTH = 16;

        /// <summary>
        /// The number of hashing iterations that should be made to secure a given password. The higher the better.
        /// </summary>
        const int HASH_ITERATIONS = 10000;

        static readonly char[] alphabetUpper = new[] {
            // Uppercase characters
            'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'P', 'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z', 'X', 'C', 'V', 'B', 'N', 'M',
        };
        static readonly char[] alphabetLower = new[] {
            // Lowercase characters
            'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'p', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'z', 'x', 'c', 'v', 'b', 'n', 'm',
        };
        static readonly char[] alphabetDigit = new[] {
            // Digits
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
        };
        static readonly char[] alphabetSpecial = new[] {
            // Special characters
             '@', '#', '$', '_', '-', '=', '^', '~'
        };

        enum EnmCharType
        {
            Lower = 0,
            Upper = 1,
            Digit = 2,
            Special = 3,
        }

        /// <summary>
        /// Generate a random password. The "flavour" of the generated password can be altered by changing the password length and minimum character count per type in the method's parameters.
        /// Keep in mind that the 'length' parameter may not be smaller then the sum of all the minimum character counts from the other parameters.
        /// If the length value exceeds the sum of all the minimum character counts, the miscelanious characters will have be of a random type.
        /// If the minimum character count of a type is set to 0, the generated password will not contain any characters of this type.
        /// </summary>
        /// <param name="length">The exact length of the password that is to be generated</param>
        /// <param name="minLowerCharCount">The minimum number of lowercase characters that should be present in the output</param>
        /// <param name="minUpperCharCount">The minimum number of uppsercase characters that should be present in the output</param>
        /// <param name="minDigitCount">The minimum number of digits that should be present in the output</param>
        /// <param name="minSpecialCharCount">The minimum number of special characters that should be present in the output</param>
        /// <returns>The generated password as a string</returns>
        public static string GeneratePassword(int length = 12,
                                              int minLowerCharCount = 1,
                                              int minUpperCharCount = 1,
                                              int minDigitCount = 1,
                                              int minSpecialCharCount = 1)
        {
            #region Input validation
            if (minLowerCharCount < 0)
                throw new ArgumentException("Value cannot be less than 0", "minLowerCharCount");
            if (minUpperCharCount < 0)
                throw new ArgumentException("Value cannot be less than 0", "minUpperCharCount");
            if (minDigitCount < 0)
                throw new ArgumentException("Value cannot be less than 0", "minNumberCount");
            if (minSpecialCharCount < 0)
                throw new ArgumentException("Value cannot be less than 0", "minSpecialCharCount");
            if (length < 1)
                throw new ArgumentException("Value must be greater than 0", "minSpecialCharCount");
            int totalMinChars = minLowerCharCount + minUpperCharCount + minDigitCount + minSpecialCharCount;
            if (totalMinChars == 0)
                throw new InvalidOperationException("Atleast one of the following parameters must have a value greater than 0: 'minLowerCharCount', 'minUpperCharCount', 'minDigitCount', 'minSpecialCharCount'");
            int remainingChars = length - totalMinChars;
            if (remainingChars < 0)
                throw new ArgumentException("Cannot be less than the sum of parameters 'minLowerCharCount', 'minUpperCharCount', 'minNumberCount' and 'minSpecialCharCount'.", "length");
            #endregion

            // If the "Length" parameter exceeds the sum of all combined "min chars", increment each of the minimum char counts by 1 untill the length is equal to the sum.
            // This will greatly simplify the password generation proces
            while (remainingChars > 0)
            {
                if (remainingChars > 0 && minLowerCharCount > 0)
                {
                    minLowerCharCount++; remainingChars--;
                }
                if (remainingChars > 0 && minUpperCharCount > 0)
                {
                    minUpperCharCount++; remainingChars--;
                }
                if (remainingChars > 0 && minDigitCount > 0)
                {
                    minDigitCount++; remainingChars--;
                }
                // Don't use special characters as filler, else the password will be too hardcore!
                //if (remainingChars > 0 && minSpecialCharCount > 0)
                //{
                //    minSpecialCharCount++; remainingChars--;
                //}
            }

            // Prepare our RNG (random number generator)
            var r = new CryptoRandom();

            // The variable that will contain our password that will be generated
            var password = new Char[length];

            // We will generate the password per-character. So keep incrementing our position index by 1 untill the password has been generated completely.
            int index = 0;
            while (index < password.Length)
            {
                // Randomly pick which character type we will generate next. Each character type has a 25% chance to be picked.
                // We could also pick a random character over the entire available character set, but that would lead to more predictable passwords
                EnmCharType ct = (EnmCharType)r.Next(4);

                switch (ct)
                {
                    case EnmCharType.Lower:
                        if (minLowerCharCount > 0)
                        {
                            // Pick a random lowercase character from the set and set it at the index
                            password[index] = alphabetLower[r.Next(alphabetLower.Length)];
                            minLowerCharCount--; // Consume a character of this type
                        }
                        else
                            continue;
                        break;
                    case EnmCharType.Upper:
                        if (minUpperCharCount > 0)
                        {
                            // Pick a random uppsercase character from the set and set it at the index
                            password[index] = alphabetUpper[r.Next(alphabetUpper.Length)];
                            minUpperCharCount--; // Consume a character of this type
                        }
                        else
                            continue;
                        break;
                    case EnmCharType.Digit:
                        if (minDigitCount > 0)
                        {
                            // Pick a random digit from the set and set it at the index
                            password[index] = alphabetDigit[r.Next(alphabetDigit.Length)];
                            minDigitCount--; // Consume a character of this type
                        }
                        else
                            continue;
                        break;
                    case EnmCharType.Special:
                        if (minSpecialCharCount > 0 && index > 0) // Exception: the first character of the password should not be a special character
                        {
                            // Pick a random special character from the set and set it at the index
                            password[index] = alphabetSpecial[r.Next(alphabetSpecial.Length)];
                            minSpecialCharCount--; // Consume a character of this type
                        }
                        else
                            continue;
                        break;
                    default:
                        continue; // ???
                }

                // A valid character has been generated, proceed to the next character
                index++;
            }
            // Return our newly generated password
            return new String(password);
        }

        /// <summary>
        /// Generate a simple (SHA512) hash from an arbitrary string.
        /// NOTE: This method generates a random salt every time the method is called. Calling the method twice will result in two different hash strings.
        ///       If the Hash is to be used for simple verification purposes, please call the overloaded Hash method.
        /// </summary>
        /// <param name="text">The text to generate the hash for</param>
        /// <returns>A simple hash string</returns>
        public static string Hash(string text)
        {
            if (String.IsNullOrEmpty(text))
                return null;
            var textBytes = ASCIIEncoding.ASCII.GetBytes(text);
            var saltBytes = Guid.NewGuid().ToByteArray();
            return Hash(textBytes, saltBytes, HashFactories.SHA512()).ToString();
        }

        /// <summary>
        /// Transform any arbitrary input string into a secure and unintelligible hash (string).
        /// - Simple and quick
        /// - Low level security
        /// - NOT to be used for sensitive data such as passwords. Instead use the method method HashPassword.
        /// </summary>
        /// <param name="text">The input string to hash</param>
        /// <param name="salt">A string containing 16 or more random characters. The salt should be saved together with the hash. Only use each salt once (read: generate a new salt every time you hash)</param>
        /// <param name="algorithm">The algorithm that should perform the hashing operation</param>
        /// <returns>A HashResult object that contains the results of the hashing proces. Call .ToString() to convert the object into a single hash string</returns>
        /// <remarks>Not to be used for sensitive data such as passwords</remarks>
        public static HashResult Hash(string text, string salt, EnmHashingAlgorithm algorithm = EnmHashingAlgorithm.SHA512)
        {
            if (String.IsNullOrEmpty(text))
                return null;
            if (salt == null || salt.Length < SALT_LENGTH)
                throw new ArgumentException("Must be atleast " + SALT_LENGTH.ToString() + " characters in length", "salt");

            var textBytes = ASCIIEncoding.ASCII.GetBytes(text);
            var saltBytes = ASCIIEncoding.ASCII.GetBytes(salt);
            return Hash(textBytes, saltBytes, algorithm.SHA());
        }

        /// <summary>
        /// Determines if the contents of the hash string 'hashedText' matches the raw input string 'verificationText'.
        /// </summary>
        /// <param name="originalText">The original text to validate against the hash</param>
        /// <param name="hashedText">The complete secure hash string as produced by the other hashing methods</param>
        /// <returns>True if there is a match, otherwise false</returns>
        public static bool ValidateHash(string originalText, string hashedText)
        {
            try
            {
                if (String.IsNullOrEmpty(originalText) || String.IsNullOrEmpty(hashedText) || hashedText.Count(x => x == DATA_DELIMETER) != 2)
                    return false; // Invalid input parameters
                var data = hashedText.Split(DATA_DELIMETER);
                // data[0] = Algorithm
                // data[1] = Salt
                // data[2] = Hash

                EnmHashingAlgorithm algorithm = EnmHashingAlgorithm.SHA512;
                if (!Enum.TryParse<EnmHashingAlgorithm>(data[0], out algorithm))
                    return false; // Unknown hashing algorithm

                var saltBytes = Convert.FromBase64String(data[1]);
                var textBytes = ASCIIEncoding.ASCII.GetBytes(originalText);

                return Hash(textBytes, saltBytes, algorithm.SHA()).Hash == data[2];
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a more secure hash, using a secret key generated from the secret masterkey (specified in the web.config).
        /// </summary>
        /// <param name="text">The input string to hash</param>
        /// <returns>A secure hash string</returns>
        public static string HashWithAuthentication(string text)
        {
            if (String.IsNullOrEmpty(text))
                return null;
            var textBytes = ASCIIEncoding.ASCII.GetBytes(text);
            var saltBytes = Guid.NewGuid().ToByteArray();
            return HashWithAuthentication(textBytes, saltBytes, MasterKey, EnmHashingAlgorithm.SHA512).ToString();
        }

        /// <summary>
        /// Determines if the contents of the hash string 'hashedText' matches the raw input string 'verificationText'.
        /// </summary>
        /// <param name="verificationText">The raw text to validate</param>
        /// <param name="hashedText">The hashed string to validate against</param>
        /// <param name="key">The secret key used to generate the hash</param>
        /// <returns>True if there is a match, otherwise false</returns>
        public static bool ValidateAuthenticationHash(string verificationText, string hashedText, string key)
        {
            try
            {
                if (String.IsNullOrEmpty(verificationText) || String.IsNullOrEmpty(hashedText) || hashedText.Count(x => x == DATA_DELIMETER) != 2 || key == null || key.Length < MIN_SECRETKEY_LENGTH)
                    return false; // Invalid input parameters
                var data = hashedText.Split(DATA_DELIMETER);

                EnmHashingAlgorithm algorithm = EnmHashingAlgorithm.SHA512;
                if (!Enum.TryParse<EnmHashingAlgorithm>(data[0], out algorithm))
                    return false; // Unknown hashing algorithm

                var saltBytes = Convert.FromBase64String(data[1]);
                var textBytes = Convert.FromBase64String(data[2]);
                var keyBytes = Encoding.ASCII.GetBytes(key);

                return HashWithAuthentication(textBytes, saltBytes, keyBytes, algorithm).ToString() == hashedText;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Transform any arbitrary input string into a secure and unintelligible hash (string). 
        /// This method is a keyed hashing method which is a lot more secure then the regular hashing methods
        /// - The generated keyed hash can be used to verify the authenticity of a message
        /// - NOT to be used for securing passwords, instead use the provided method HashPassword.
        /// </summary>
        /// <param name="text">The input string to hash</param>
        /// <param name="salt">A string containing 16 or more random characters. The salt should be saved together with the hash. Only use each salt once (read: generate a new salt every time you hash)</param>
        /// <param name="algorithm">The hashing algorithm to use for the hashing proces</param>
        /// <param name="key">The secret key to be used to generate the hash. T</param>
        /// <returns>A HashResult object that contains the results of the hashing proces. Call .ToString() to convert the object into a single hash string</returns>
        /// <remarks>Not to be used for sensitive data such as passwords</remarks>
        public static HashResult HashWithAuthentication(string text, string salt, EnmHashingAlgorithm algorithm, string key)
        {
            if (text == null || text.Length == 0)
                return null;
            if (salt == null || salt.Length < SALT_LENGTH)
                throw new ArgumentException("Must be atleast " + SALT_LENGTH.ToString() + " characters in length", "salt");
            if (key == null || key.Length < MIN_SECRETKEY_LENGTH)
                throw new ArgumentException("Must be atleast " + MIN_SECRETKEY_LENGTH.ToString() + " characters in length", "key");

            var textBytes = Encoding.ASCII.GetBytes(text);
            var saltBytes = Encoding.ASCII.GetBytes(salt);
            var keyBytes = Encoding.ASCII.GetBytes(key);
            return HashWithAuthentication(textBytes, saltBytes, keyBytes, algorithm);
        }

        /// <summary>
        /// Genereer een veilige(re) hash voor een string die belangrijke data bevat, zoals een wachtwoord. 
        /// Deze methode is CPU intensiever dan de methode 'HashString', welke op zijn beurt beter gebruikt kan worden voor snelle en simpele authenticatie hashes.
        /// </summary>
        /// <param name="password">De veilige string welke gehashed moet worden</param>
        /// <returns>De gehashde string welke in zijn volledigheid opgeslagen dient te worden, in het formaat: ALGORITME:ITERATIES:SALT:HASH</returns>
        public static string HashPassword(string password)
        {
            // Controleer parameters
            if (String.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            var passwordBytes = Encoding.ASCII.GetBytes(password);
            var saltBytes = GenerateSalt();
            var generatedHash = new PBKDF2(EnmSecureHashingAlgorithm.HMACSHA512.Factory(), passwordBytes, saltBytes, HASH_ITERATIONS).GetBytes(64);
            return new PasswordResult(EnmSecureHashingAlgorithm.HMACSHA512.ToString(),
                                      HASH_ITERATIONS,
                                      Convert.ToBase64String(saltBytes), // Base-64 salt
                                      Convert.ToBase64String(generatedHash)) // Base-64 hash
                                      .ToString(); // Retourneer de secure string. Het object gaan we niet teruggeven
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="algorithm"></param>
        /// <param name="salt">De te gebruiken salt, welke minimaal uit 16 karakters dient te bestaan</param>
        /// <param name="key">De primaire (master) sleutel waarmee de hash gegenereerd dient te worden</param>
        /// <returns></returns>
        public static PasswordResult HashPassword(string password, EnmSecureHashingAlgorithm algorithm, string salt, string key)
        {
            // Controleer parameters
            if (String.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (salt == null || salt.Length < SALT_LENGTH)
                throw new ArgumentException("Must be atleast " + SALT_LENGTH.ToString() + " characters in length", "salt");
            if (key == null || key.Length < MIN_SECRETKEY_LENGTH)
                throw new ArgumentException("Must be atleast " + MIN_SECRETKEY_LENGTH.ToString() + " characters in length", "masterKey");
            // Genereer een salt en converteer het wachtwoord naar bytes
            var factory = HMACFactories.HMACSHA512;
            var saltBytes = Encoding.ASCII.GetBytes(salt);
            var passwordBytes = Encoding.ASCII.GetBytes(password);
            var generatedHash = new PBKDF2(factory, passwordBytes, saltBytes, HASH_ITERATIONS).GetBytes(64);
            return new PasswordResult(algorithm.ToString(), HASH_ITERATIONS, Convert.ToBase64String(saltBytes), Convert.ToBase64String(generatedHash));
        }

        /// <summary>
        /// Valideer of een wachtwoord geldig is door deze met de beveiligde hash te vergelijken.
        /// </summary>
        /// <param name="password">Het te valideren wachtwoord</param>
        /// <param name="passwordHash">De secure string (gegenereerd met de methode HashPassword)</param>
        /// <returns>True indien het wachtwoord overeen komt met de secure hash</returns>
        public static bool ValidatePasswordHash(string password, string passwordHash)
        {
            if (String.IsNullOrEmpty(password))
                throw new ArgumentException("password");
            if (String.IsNullOrEmpty(passwordHash))
                throw new ArgumentException("passwordHash");

            var data = new PasswordResult(passwordHash);

            // Bereid de parameters voor op gebruik
            var passwordBytes = Encoding.ASCII.GetBytes(password); // Het te valideren wachtwoor als bytes
            var saltBytes = Convert.FromBase64String(data.Salt); // Het te valideren wachtwoor als bytes
            var generatedHash = new PBKDF2(data.Factory, passwordBytes, saltBytes, data.Iterations).GetBytes(64);

            var hashedBytes = Convert.FromBase64String(data.Hash);
            return SlowEquals(generatedHash, hashedBytes);
        }

        /// <summary>
        /// Valideer of een wachtwoord geldig is door deze met de ingevoerde parameters te vergelijken.
        /// </summary>
        /// <param name="password">Het plain text wachtwoodr dat vergeleken moet worden met de secure hash</param>
        /// <param name="method">Het te gebruiken hashing algorithme</param>
        /// <param name="salt">Base-64 Encoded salt (gegenereerd met de methode HashPassword)</param>
        /// <param name="iteraties">Het aantal hashing iteraties</param>
        /// <param name="passwordHash">Base-64 Encoded password hash(Zoals gegenereerd uit de methode HashPassword)</param>
        /// <returns>True indien het wachtwoord overeen komt met de secure hash</returns>
        public static bool ValidatePasswordHash(string password, EnmSecureHashingAlgorithm method, string salt, int iteraties, string passwordHash)
        {
            // Valideer input
            if (String.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (iteraties < 1)
                throw new ArgumentNullException("Value must be greater then 0", "iteraties");
            if (String.IsNullOrEmpty(salt))
                throw new ArgumentNullException("salt");
            if (String.IsNullOrEmpty(passwordHash))
                throw new ArgumentNullException("passwordHash");

            // Bereid de parameters voor op gebruik
            var factory = method.Factory(); // De te gebruiken HMACSHA algoritme
            var passwordBytes = Convert.FromBase64String(password); // Het te valideren wachtwoor als bytes
            var saltBytes = Convert.FromBase64String(salt); // Het te valideren wachtwoor als bytes
            var generatedHash = new PBKDF2(factory, passwordBytes, saltBytes, iteraties).GetBytes(64);

            // Slow compare
            return SlowEquals(generatedHash, Convert.FromBase64String(passwordHash));
        }

        /// <summary>
        /// Versleutel een willekeurige string naar een onleesbare variant welke moeilijk is om te kraken. 
        /// Deze methode dient alleen gebruikt te worden indien de versleutelde tekst ook weer ontsleuteld moet kunnen woren (anders moet je een hash gebruiken).
        /// </summary>
        /// <param name="text">De tekst welke versleuteld dient te worden</param>
        /// <returns>De versleutelde tekst</returns>
        public static string Encrypt(string text)
        {
            #region Input validation
            if (text == null || text.Length == 0)
                return null;
            #endregion

            var textBytes = Util.SafeUTF8.GetBytes(text);
            return Encrypt(textBytes, MasterKey).ToString();
        }

        /// <summary>
        /// Versleutel een willekeurige string naar een onleesbare variant welke moeilijk is om te kraken. 
        /// Deze methode dient alleen gebruikt te worden indien de versleutelde tekst ook weer ontsleuteld moet kunnen woren (anders moet je een hash gebruiken).
        /// </summary>
        /// <param name="text">De tekst welke versleuteld dient te worden</param>
        /// <param name="key">De geheime sleutel waarmee de tekst versleuteld dient te worden</param>
        /// <returns></returns>
        public static EncryptionResult Encrypt(string text, string key)
        {
            #region Input validation
            if (text == null || text.Length == 0)
                return null;
            if (key == null || key.Length < MIN_SECRETKEY_LENGTH)
                throw new ArgumentException("Must be atleast " + MIN_SECRETKEY_LENGTH.ToString() + " characters in length", "key");
            #endregion

            var textBytes = Util.SafeUTF8.GetBytes(text);
            var keyBytes = ASCIIEncoding.ASCII.GetBytes(key);
            return Encrypt(textBytes, keyBytes);
        }

        /// <summary>
        /// Ontsleutel een versleutelde string welke is versleuteld met de Encrypt methode.
        /// </summary>
        /// <param name="secureString">De volledige secure string zoals deze gegenereerd wordt door Encrypt()</param>
        /// <returns>De originele (ontsleutelde) tekststring</returns>
        public static string Decrypt(string secureString)
        {
            if (String.IsNullOrEmpty(secureString))
                return secureString;
            var data = new EncryptionResult(secureString);
            var saltBytes = Convert.FromBase64String(data.Salt);
            var cipherBytes = Convert.FromBase64String(data.CipherText);
            return Decrypt(cipherBytes, saltBytes, MasterKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Decrypt(EncryptionResult data, string key)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            var cipherBytes = Convert.FromBase64String(data.CipherText);
            if (key == null || key.Length == 0)
                throw new ArgumentNullException("key");
            var saltBytes = Convert.FromBase64String(data.Salt);
            var keyBytes = ASCIIEncoding.ASCII.GetBytes(key);
            return Decrypt(cipherBytes, saltBytes, keyBytes);
        }

        #region PerplexLib-Only (Internal) Fields, Properties & Methods

        /// <summary>
        /// Het scheidingsteken waarmee informatie over de hashingmethode wordt bewaard
        /// </summary>
        internal const char DATA_DELIMETER = ':';

        internal static byte[] MasterKey
        {
            get
            {
                string key = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY];
                if (String.IsNullOrEmpty(key))
                    throw new Exception("A valid master key must be provided. Place the key in the AppSettings section of the web.config with the name '" + Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY + "'");
                if (key.Length < MIN_SECRETKEY_LENGTH)
                    throw new Exception("The masterkey must be atleast " + MIN_SECRETKEY_LENGTH + " characters in length");
                return ASCIIEncoding.ASCII.GetBytes(key);
            }
        }

        static string Decrypt(byte[] cipherText, byte[] salt, byte[] key)
        {
            // Decrypt
            var textBytes = EtM.Decrypt(key, cipherText, salt);
            // Return as string
            return Util.SafeUTF8.GetString(textBytes);
        }

        static EncryptionResult Encrypt(byte[] text, byte[] key)
        {
            #region Input validation
            if (text == null || text.Length == 0)
                return null;
            if (key == null || key.Length < MIN_SECRETKEY_LENGTH)
                throw new ArgumentException("Must be atleast " + MIN_SECRETKEY_LENGTH.ToString() + " characters in length", "key");
            #endregion

            // Generate salt
            var salt = GenerateSalt();
            // Encrypt 
            var encryptedBytes = EtM.Encrypt(key, text, salt); // Let op: De resulterende byte array bestaat stiekem uit drie componenten: IV (Initialization Vector), HMAC (authentication) en de ciphertext (encrypted text)
            // Return the encrypted data and the salt as a Base-64 string in an object
            return new EncryptionResult(Convert.ToBase64String(salt), Convert.ToBase64String(encryptedBytes));
        }

        static HashResult Hash(byte[] text, byte[] salt, HashAlgorithm algorithm)
        {
            if (text == null || text.Length == 0)
                return null;
            if (salt == null || salt.Length < SALT_LENGTH)
                throw new ArgumentException("Must be atleast " + SALT_LENGTH.ToString() + " characters in length", "salt");

            // Het hashingalgoritme verwacht bytes. Converteer onze string naar bytes
            var bytesUnhashed = text.Concat(salt).ToArray();

            // Magic time
            var hash = algorithm.ComputeHash(bytesUnhashed);

            // Transformeer de salt en de hash naar een Base-64 encoded string zodat je deze gemakkelijk kan opslaan en opsturen
            return new HashResult(algorithm.GetType().Name, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        static HashResult HashWithAuthentication(byte[] text, byte[] salt, byte[] key, EnmHashingAlgorithm algorithm)
        {
            if (text == null || text.Length == 0)
                return null;
            if (salt == null || salt.Length < SALT_LENGTH)
                throw new ArgumentException("Must be atleast " + SALT_LENGTH.ToString() + " characters in length", "salt");
            if (key == null || key.Length < MIN_SECRETKEY_LENGTH)
                throw new ArgumentException("Must be atleast " + MIN_SECRETKEY_LENGTH.ToString() + " characters in length", "masterKey");

            // Tussenstap: Genereer nu o.b.v. de (master)key en de salt een nieuwe afgeleide sleutel waarmee we gaan hashen: de zogenaamde hashKey.
            // Dit doen we zodat je de key niet kan herleiden uit de hash die we straks gaan genereren.
            byte[] hashkeyBytes; // De hashkey. Hiermee gaan we zometeen hashen
            using (var hkdf = new HKDF(SecurityDrivenDotNet.HMACFactories.HMACSHA512, key, salt))
                hashkeyBytes = hkdf.GetBytes(64);

            // Magic: Transformeer de data, met behulp van de sleutel, naar een onleesbare maar voorspelbare hash.
            var hash = new HMAC2(algorithm.Factory(), hashkeyBytes).ComputeHash(text);

            // Retourneer het resultaat als een BASE-64 encoded string
            return new HashResult(algorithm.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        static bool SlowEquals(byte[] a, byte[] b)
        {
            var diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        static byte[] GenerateSalt()
        {
            return Guid.NewGuid().ToByteArray();
        }

        internal static Func<HMAC> Factory(this EnmSecureHashingAlgorithm enm)
        {
            switch (enm)
            {
                case EnmSecureHashingAlgorithm.HMACSHA1:
                    return HMACFactories.HMACSHA1;
                case EnmSecureHashingAlgorithm.HMACSHA256:
                    return HMACFactories.HMACSHA256;
                case EnmSecureHashingAlgorithm.HMACSHA512:
                    return HMACFactories.HMACSHA512;
                default:
                    throw new InvalidOperationException("Unknown algorithm: '" + enm.ToString() + "'");
            }
        }

        static HMAC HMAC(this EnmSecureHashingAlgorithm enm)
        {
            return enm.Factory()();
        }

        static Func<HashAlgorithm> Factory(this EnmHashingAlgorithm enm)
        {

            switch (enm)
            {
                case EnmHashingAlgorithm.SHA1:
                    return HashFactories.SHA1;
                case EnmHashingAlgorithm.SHA256:
                    return HashFactories.SHA256;
                case EnmHashingAlgorithm.SHA512:
                    return HashFactories.SHA512;
                default:
                    throw new InvalidOperationException("Unknown algorithm: '" + enm.ToString() + "'");
            }
        }

        static HashAlgorithm SHA(this EnmHashingAlgorithm enm)
        {
            return enm.Factory()();
        }

        #endregion
    }

    /// <summary>
    /// The results of the hashing operation. Consult the properties to read each part of the hash string.
    /// To combine the HashResult object into a single hash string, simply call .ToString().
    /// </summary>
    public class HashResult
    {
        /// <summary>
        /// The algorithm that was used for the hashing proces
        /// </summary>
        public string Algorithm { get; private set; }

        /// <summary>
        /// The salt that was used to generate the hash
        /// </summary>
        public string Salt { get; private set; }

        /// <summary>
        /// The generated hash as a Base-64 encoded string.
        /// </summary>
        public string Hash { get; private set; }

        internal HashResult(string algorithm, string salt, string hash)
        {
            switch (algorithm)
            {
                case "SHA":
                case "SHA1":
                case "SHACng":
                case "SHA1Cng":
                    Algorithm = "SHA1"; break;
                case "SHA256":
                case "SHA256Cng":
                    Algorithm = "SHA256"; break;
                default:
                    Algorithm = "SHA512"; break;
            }
            Salt = salt;
            Hash = hash;
        }

        /// <summary>
        /// Generates a single hash string that contains the method and the salt to generate the hash.
        /// </summary>
        public override string ToString()
        {
            return Algorithm + Security.DATA_DELIMETER + Salt + Security.DATA_DELIMETER + Hash;
        }
    }

    /// <summary>
    /// The results of the encryption operation. Consult the properties to read each part of the encrypted string.
    /// To combine the EncryptionResult object into a single hash string, simply call .ToString().
    /// </summary>
    public class EncryptionResult
    {
        /// <summary>
        /// De Base-64 encoded salt.
        /// </summary>
        public string Salt { get; private set; }

        /// <summary>
        /// De Base-64 encoded encryption data.
        /// </summary>
        public string CipherText { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secureString"></param>
        public EncryptionResult(string secureString)
        {
            if (String.IsNullOrEmpty(secureString))
                throw new ArgumentNullException("secureString");
            else if (secureString.Count(c => c == Security.DATA_DELIMETER) != 1)
                throw new ArgumentException("Not a secure string", "secureString");

            var data = secureString.Split(Security.DATA_DELIMETER);
            Salt = data[0];
            CipherText = data[1];
            if (Salt == null || Salt.Length == 0 ||
                CipherText == null || CipherText.Length == 0)
                throw new ArgumentException("Not a secure string", "secureString");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="salt"></param>
        /// <param name="cipherText"></param>
        public EncryptionResult(string salt, string cipherText)
        {
            Salt = salt;
            CipherText = cipherText;
        }

        /// <summary>
        /// Generates a single hash string that contains the method and the salt to generate the hash.
        /// </summary>
        public override string ToString()
        {
            return
                String.Join(Security.DATA_DELIMETER.ToString(), Salt, CipherText);
        }
    }

    /// <summary>
    /// The results of the password generation operation. Consult the properties to read each part of the encrypted string.
    /// To combine the EncryptionResult object into a single hash string, simply call .ToString().
    /// </summary>
    public class PasswordResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string Algorithm { get; private set; }

        internal Func<HMAC> Factory { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Iterations { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Salt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="passwordHash"></param>
        public PasswordResult(string passwordHash)
        {
            if (String.IsNullOrEmpty(passwordHash))
                throw new ArgumentException("passwordHash");
            // De te verifieren data bestaat uit 4 componenten, gescheiden door een speciaal teken
            var passwordData = passwordHash.Split(Security.DATA_DELIMETER); // Het wachtwoord is op een bepaalde manier opgeslagen => Weer ontleden en met het nieuwe wachtwoord hergenereren
            if (passwordData.Length != 4)
                throw new ArgumentException("Onverwacht formaat: 4 velden verwacht maar het zijn er " + passwordData.Length.ToString(), "storedPassword");
            // 0: Algoritme
            EnmSecureHashingAlgorithm tmp;
            if (Enum.TryParse(passwordData[0], out tmp))
                Factory = tmp.Factory();
            else
                throw new ArgumentException("Kan het hashing algoritme niet bepalen", "securePassword");
            // 1: Iteraties
            int iterations;
            if (int.TryParse(passwordData[1], out iterations))
                Iterations = iterations;
            else
                throw new ArgumentException("Kan het aantal iteraties niet bepalen", "securePassword");
            // 2: Salt
            if (!String.IsNullOrEmpty(passwordData[2]))
                Salt = passwordData[2];
            else
                throw new ArgumentException("Kan de salt niet bepalen", "securePassword");

            // 3: Hash
            if (!String.IsNullOrEmpty(passwordData[3]))
                Hash = passwordData[3];
            else
                throw new ArgumentException("Kan de hash niet bepalen", "securePassword");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="iterations"></param>
        /// <param name="salt"></param>
        /// <param name="hash"></param>
        public PasswordResult(string algorithm, int iterations, string salt, string hash)
        {
            Algorithm = algorithm;
            Iterations = iterations;
            Salt = salt;
            Hash = hash;
        }

        /// <summary>
        /// Generates a single hash string that contains the method and the salt to generate the hash.
        /// </summary>
        public override string ToString()
        {
            // bouw een string op als ==> ALGORITME:ITERATIES:SALT:HASH
            return
                String.Join(Security.DATA_DELIMETER.ToString(), Algorithm, Iterations, Salt, Hash);
        }
    }
}

//namespace PerplexMail
//{
//    public static class Security
//    {
//        public enum EnmStrength
//        {
//            Numbers,
//            Chars,
//            CharsAndNumbers,
//            CharsAndNumbersAndSpecialChars
//        }

//        /// <summary>
//        /// Determines the keysize to be used when encrypting texts.
//        /// </summary>
//        const int Keysize = 256;

//        /// <summary>
//        /// This encryption key is used as the default when no key is sent with the encrypt/decrypt function calls
//        /// </summary>
//        static string DefaultEncryptionKey
//        {
//            get
//            {
//                string defaultKey = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_ENCRYPTION_PRIVATEKEY];
//                if (String.IsNullOrEmpty(defaultKey))
//                    defaultKey = "Nk1uST8Qa3i5hHzw1ZHYe";
//                return defaultKey;
//            }
//        }

//        /// <summary>
//        /// This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
//        /// This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
//        /// 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
//        ///     
//        /// This salt is used in combination with a private key to encrypt/decrypt sensitive data.
//        /// 
//        /// THE SALT SHOULD BE EXACTLY 32 CHARACTERS LONG!
//        /// </summary>
//        static byte[] InitVectorBytes
//        {
//            get
//            {
//                // Specified salt must be 32 characters long!
//                var data = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_ENCRYPTION_VECTORBYTES];
//                if (String.IsNullOrEmpty(data))
//                    data = "PleaseConfigureSaltInTheWebConfi"; // This default should NOT be used! Configure your own web.config key
//                if (data.Length != 32)
//                    throw new InvalidOperationException("Web.config error (appSettings key '" + Constants.WEBCONFIG_SETTING_ENCRYPTION_VECTORBYTES + "'): Provided vector bytes must be exactly 32 characters long!");
//                return Encoding.ASCII.GetBytes(data);
//            }
//        }

//        /// <summary>
//        /// Encrypt a string with a password. With PerplexLib.Security.Decrypt() you can decrypt the same string with the right password
//        /// </summary>
//        /// <param name="plaintext">The to encrypt text</param>
//        /// <param name="key">The password, if null the default 'EncryptionKey' web.config key is used</param>
//        /// <returns>The encrypted string</returns>
//        public static string Encrypt(string plaintext, string key = null, string salt = null)
//        {
//            // Validate the input

//            // Use a default key if none is present
//            key = key ?? DefaultEncryptionKey;

//            // This throws an exception if anything went wrong...
//            ValidateEncryptDecryptInput(key);

//            // Add the salt to our string
//            if (!String.IsNullOrEmpty(plaintext) && !String.IsNullOrEmpty(salt))
//                plaintext += salt;

//            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plaintext);
//            using (PasswordDeriveBytes password = new PasswordDeriveBytes(key, null))
//            {
//                byte[] keyBytes = password.GetBytes(Keysize / 8);
//                using (RijndaelManaged symmetricKey = new RijndaelManaged())
//                {
//                    symmetricKey.Mode = CipherMode.CBC;
//                    symmetricKey.Padding = PaddingMode.PKCS7;
//                    symmetricKey.BlockSize = 256; // might take longer to encrypt
//                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, InitVectorBytes))
//                    {
//                        using (MemoryStream memoryStream = new MemoryStream())
//                        {
//                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
//                            {
//                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
//                                cryptoStream.FlushFinalBlock();
//                                byte[] cipherTextBytes = memoryStream.ToArray();
//                                return Convert.ToBase64String(cipherTextBytes);
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Decode an encrypted string which was encoded with the method PerplexLib.Security.Encrypt
//        /// </summary>
//        /// <param name="ciphertext">De encrypted tekst</param>
//        /// <param name="key">The password, if null the default 'EncryptionKey' web.config key is used</param>
//        /// <returns>The original encrypted string</returns>
//        public static string Decrypt(string ciphertext, string key = null, string salt = null)
//        {
//            // Just return the ciphertext if it doens't contain anything
//            if (String.IsNullOrEmpty(ciphertext)) 
//                return ciphertext;

//            try
//            {
//            // Use a default key if none is present
//            key = key ?? DefaultEncryptionKey;

//            // This throws an exception if anything went wrong...
//            ValidateEncryptDecryptInput(key);

//            byte[] cipherTextBytes = Convert.FromBase64String(ciphertext);
//            using (PasswordDeriveBytes password = new PasswordDeriveBytes(key, null))
//            {
//                byte[] keyBytes = password.GetBytes(Keysize / 8);
//                using (RijndaelManaged symmetricKey = new RijndaelManaged())
//                {
//                    symmetricKey.Mode = CipherMode.CBC;
//                    symmetricKey.Padding = PaddingMode.PKCS7;
//                    symmetricKey.BlockSize = 256; // might take longer to encrypt
//                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, InitVectorBytes))
//                    {
//                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
//                        {
//                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
//                            {
//                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
//                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
//                                string result = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);

//                                // Remove the salt from our string
//                                if (!String.IsNullOrEmpty(result) && !String.IsNullOrEmpty(salt))
//                                    result = result.Replace(salt, "");

//                                return result;
//                            }
//                        }
//                    }
//                }
//            }
//            }
//            catch (Exception)
//            {
//                return "Unable to decrypt: " + ciphertext;
//            }
//        }

//        static void ValidateEncryptDecryptInput(string key)
//        {
//            // Check for a key
//            if (String.IsNullOrEmpty(key))
//                throw new ArgumentNullException("No encryption key passed and not set in the web.config, use EncryptionKey as the web.config key");

//            // Check for minlength
//            if (key.Length < 12)
//                throw new ArgumentException("The encryption key is too short, minlength is 12");

//            // Check the initvectorbytes
//            var data = InitVectorBytes;
//            if (data == null)
//                throw new NullReferenceException("Vectorbytes are not specified!");
//            if (data.Length != 32)
//                throw new ArgumentException("The initVectorBytes should be 32 bytes long, no more no less");
//        }

//        /// <summary>
//        /// Generates a strong password, minimal 1 digit, 1 capital, 1 small char and 1 special character 
//        /// </summary>
//        /// <param name="length">Length can be set</param>
//        /// <returns>String</returns>
//        /// <remarks></remarks>
//        public static string GeneratePassword(int length, EnmStrength strength = EnmStrength.CharsAndNumbersAndSpecialChars)
//        {
//            //create constant strings for each type of characters
//            string alphaCaps = "QWERTYUIOPASDFGHJKLZXCVBNM";
//            string alphaLow = "qwertyuiopasdfghjklzxcvbnm";
//            string numerics = "1234567890";
//            string special = "@#$_-=^~";

//            //create another string which is a concatenation of all above
//            string allChars = string.Empty;

//            switch (strength)
//            {
//                case EnmStrength.Chars:
//                    allChars = alphaCaps + alphaLow;
//                    break;
//                case EnmStrength.CharsAndNumbers:
//                    allChars = alphaCaps + alphaLow + numerics;
//                    break;
//                case EnmStrength.CharsAndNumbersAndSpecialChars:
//                    allChars = alphaCaps + alphaLow + numerics + special;
//                    break;
//                case EnmStrength.Numbers:
//                    allChars = numerics;
//                    break;
//            }

//            var r = new RNGCryptoServiceProvider();
//            String generatedPassword = "";
//            for (int i = 0; i <= length - 1; i++)
//            {
//                byte[] result = new byte[8];
//                r.GetBytes(result);
//                double rand = (double)BitConverter.ToUInt64(result, 0) / ulong.MaxValue;

//                if (i == 0 & (strength == EnmStrength.Chars | strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
//                {
//                    //First character is an upper case alphabet
//                    generatedPassword += alphaCaps.ToCharArray()[Convert.ToInt32(Math.Floor(rand * alphaCaps.Length))];
//                }
//                else if (i == 1 & (strength == EnmStrength.Chars | strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
//                {
//                    //Second character is a lower case alphabet
//                    generatedPassword += alphaLow.ToCharArray()[Convert.ToInt32(Math.Floor(rand * alphaLow.Length))];
//                }
//                else if (i == 2 & (strength == EnmStrength.CharsAndNumbersAndSpecialChars))
//                {
//                    //Third character is a special 
//                    generatedPassword += special.ToCharArray()[Convert.ToInt32(Math.Floor(rand * special.Length))];
//                }
//                else if (i == 3 & (strength == EnmStrength.CharsAndNumbers | strength == EnmStrength.CharsAndNumbersAndSpecialChars))
//                {
//                    //Fourth character is a number 
//                    generatedPassword += numerics.ToCharArray()[Convert.ToInt32(Math.Floor(rand * numerics.Length))];
//                }
//                else
//                {
//                    // rest is random
//                    generatedPassword += allChars.ToCharArray()[Convert.ToInt32(Math.Floor(rand * allChars.Length))];
//                }
//            }
//            return generatedPassword;
//        }

//        /// <summary>
//        /// Performs a one-way transformation on a string, rendering the contents unreadable (forever!) but with a consistent output.
//        /// Generally used for authentication purposes.
//        /// </summary>
//        /// <param name="stringToEncode">The string to encode</param>
//        /// <returns>The transformed string</returns>
//        public static string GenerateHash(string stringToEncode)
//        {
//            string hashKey = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_HASH_PRIVATEKEY];
//            if (String.IsNullOrEmpty(hashKey))
//                // Note: please specify your own key, or enter your key in the web.config under appSettings: <add key="PerplexMailHashKey" value"..." />
//                hashKey = "Wz80pB0YKXSkxdK4"; 

//            var sha = new System.Security.Cryptography.SHA512Managed();
//            var e = new System.Text.ASCIIEncoding();
//            var data = e.GetBytes(stringToEncode + hashKey);
//            return Convert.ToBase64String(sha.ComputeHash(data));
//        }

//    }
//}