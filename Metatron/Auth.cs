using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron
{
    /// <summary>
    /// Manage authentication for Metatron.
    /// </summary>
	internal sealed class Auth : IDisposable
    {
        readonly string ERROR_UNKNOWN = "Unknown Error",
            SUCCESSFUL = "Successful",
            ERROR_EMAIL = "Invalid E-Mail Address",
            ERROR_INSTANCES = "Too many PCs running Metatron",
            ERROR_UNPAID = "Auth account has run out of paid access time",
            ERROR_PASSWORD = "Invalid password",
            ERROR_LOCKED = "Account has been locked or frozen",
            ERROR_MYSQL = "MySQL connection reported an error, try again in 5 minutes or contact Snipa",
			ERROR_BAD_RESPONSE = "Received a bad response from the server. Blame Snipa.";

        #region Encryption-related variables
        //Value used in salting pre-encryption by the server; needs removed from the decrypted result
        private readonly string _serverResponseSalt = "pythonIsAWhore";

        //Server public key used for deriving the client public key and the secret key
        private readonly BigInteger _serverPublicKey = new BigInteger(CryptographicExtensions.StringToByteArray("3C60036080F514F1BD50BF0543F2317AB79E5AE641E593345AA1DAF26B1C2440"));

        //Prime # for modulus operations (p)
        private readonly BigInteger _modulus = new BigInteger(CryptographicExtensions.StringToByteArray("FCDE12011C83943A002932EC4E2B3BA289D7207FC3AD1F39863E410FC7D94F61"));

        //Generator used for exponentiation in key calaculation
        private readonly BigInteger _generator = new BigInteger(13);
        #endregion

        #region Factory Singleton

        private static Auth _instance;

        public static Auth CreateAuth(ILogging logging)
        {
            return _instance ?? (_instance = new Auth(logging));
        }

        #endregion

        //List of servers to be used for authentication attempts
        readonly List<string> AUTH_SERVERS = new List<string>
        {
            "https://sbauth.goo.im/auth" 
        };

        //Event for notifying listeners of a completed authentication result
		public event EventHandler<__err_retn> AuthenticationComplete;

		Thread _loginThread = null;

        #region IDisposable members
        bool _isDisposed = false;
        #endregion

        private readonly ILogging _logging;

        private Auth(ILogging logging)
        {
            _logging = logging;
        }

        #region IDisposable implementors
        ~Auth()
		{
            Dispose(false);
		}

        /// <summary>
        /// Cleanup this object.
        /// </summary>
        /// <param name="isDisposing">Whether or not we're called from Dispose.</param>
        public void Dispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            //If disposing instead of finalizing
            if (isDisposing)
            {
                if (_loginThread != null &&
                    (_loginThread.ThreadState == ThreadState.Background))
                {
                    _loginThread.Abort();
                }

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Cleanup this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// Try to login with the auth server.
        /// </summary>
        /// <param name="email">Username</param>
        /// <param name="password">Password</param>
        public void TryLogin(string email, string password)
		{
            //If we haven't already started trying to login...
            if (_loginThread != null && _loginThread.ThreadState != ThreadState.Stopped) return;

            try
            {
                var stateInfo = new AuthStateInfo(this, new AuthParams(email, password));
                _loginThread = new Thread(TryLogin);
                _loginThread.Start(stateInfo);
            }
            catch (Exception e)
            {
                _logging.LogException("Authentication", e, "TryLogin", "Caught exception starting login process:");
            }
		}

        private void TryLogin(object stateInfo)
        {
            try
            {
                var authStateInfo = (AuthStateInfo) stateInfo;
                Login(authStateInfo);
            }
            catch (Exception e)
            {
                _logging.LogException("Authentication", e, "TryLogin", "Caught exception during login process:");
            }
        }

        /// <summary>
        /// Do the Login process.
        /// </summary>
        /// <param name="authStateInfo">Reference to an AuthStateInfo instance containing login info</param>
        private void Login(AuthStateInfo authStateInfo)
        {
            var loginResult = new __err_retn(true, SUCCESSFUL, true);

            if (AuthenticationComplete != null)
                AuthenticationComplete(loginResult, loginResult);
        }

        /// <summary>
        /// Try to authenticate with a given server, using the given key and IV for decrypting the result.
        /// </summary>
        /// <param name="authServerUrl">URL of the server to authenticate with.</param>
        /// <param name="key">Key to be used for decryption.</param>
        /// <param name="iv">IV to be used for decryption.</param>
        /// <returns>Result of the login attempt.</returns>
        private __err_retn Authenticate(string authServerUrl, string key, string iv)
        {
			try
			{
				//First, try to connect to the given server.
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(authServerUrl);

				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				using (var stream = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding(httpWebResponse.CharacterSet)))
				{
					var base64AuthString = "";

					try
					{
						// Try to read from the response stream
						base64AuthString = stream.ReadToEnd();
					}
					catch (Exception e)
					{
						_logging.LogException("Authentication", e, "Authenticate", "Caught exception reading response: \n{0}", e.Message);
						return new __err_retn(false, ERROR_BAD_RESPONSE, false);
					}

					if (!string.IsNullOrEmpty(base64AuthString))
					{
						byte[] encryptedResultBytes = null;
						try
						{
							// Base64-Decode the data from the webserver
							encryptedResultBytes = Convert.FromBase64String(base64AuthString);
						}
						catch (Exception e)
						{
							_logging.LogException("Authentication", e, "Authenticate", "Caught exception during base64 decoding: \n{0}", e.Message);
							return new __err_retn(false, ERROR_BAD_RESPONSE, false);
						}

						//Get the decrypted result
						//_logging.LogMessage("Authentication", "Authenticate", LogSeverityTypes.Debug, "Encrypted result string: {0}", encryptedResultBytes);
						var decryptedResultString = DecryptBytes(encryptedResultBytes, key, iv);
						//_logging.LogMessage("Authentication", "Authenticate", LogSeverityTypes.Debug, "Decrypted result string: {0}", decryptedResultString);

						//Split the decrypted result into its components
						var decryptedResults = decryptedResultString.Split(':');

						//Determine if this is a test-enabled account
						var isTesterInt = -1;
						int.TryParse(decryptedResults[1], out isTesterInt);
						var isTester = (isTesterInt == 1);

						//Get the login result.
						var authResult = y6675636b65647570.x2121;

						//See if our result is defined in the results enum.
						if (Enum.IsDefined(typeof(y6675636b65647570), decryptedResults[0]))
						{
							//It was defined, so parse the result.
							authResult = (y6675636b65647570)Enum.Parse(typeof(y6675636b65647570), decryptedResults[0]);
						}

						var authResultString = ERROR_UNKNOWN;
						switch (authResult)
						{
							case y6675636b65647570.x2121:
								authResultString = ERROR_UNKNOWN;
								break;
							case y6675636b65647570.x6561:
								authResultString = ERROR_EMAIL;
								break;
							case y6675636b65647570.x6674:
								authResultString = ERROR_INSTANCES;
								break;
							case y6675636b65647570.x6862:
								authResultString = ERROR_UNPAID;
								break;
							case y6675636b65647570.x6c74:
								authResultString = ERROR_PASSWORD;
								break;
							case y6675636b65647570.x6f74:
								authResultString = ERROR_LOCKED;
								break;
							case y6675636b65647570.x7374:
								authResultString = SUCCESSFUL;
								break;
							case y6675636b65647570.x7721:
								authResultString = ERROR_MYSQL;
								break;
						}

						//Build the login result and break, done reading
						_logging.LogMessage("Authentication", "Login", LogSeverityTypes.Debug, "Parsed decrypted result {0} as enum value {1}.", decryptedResults[0], authResultString);
						return new __err_retn(true, authResultString, isTester);
					}
				}

			}
			catch (Exception e)
			{
				_logging.LogException("Authentication", e, "Authenticate", "Caught exception reading response:", e.Message);
				return new __err_retn(false, ERROR_BAD_RESPONSE, false);
			}
			// If we fall thru to here, we failed.
			return new __err_retn(false, ERROR_UNKNOWN, false);
        }

        /// <summary>
        /// Encrypt a byte[] using a given key and IV.
        /// </summary>
        /// <param name="bytesToEncrypt">The byte[] to be encrypted.</param>
        /// <param name="key">The key to be used in encryption.</param>
        /// <param name="iv">The IV to be used in encryption.</param>
        /// <returns>Encrypted byte[]</returns>
        private byte[] EncryptBytes(byte[] bytesToEncrypt, string key, string iv)
        {
            //Get an encryptor using the provided key and IV
            var aesEncryptor = GetAesEncryptor(key, iv);
            //Get an ICryptoTransform used for the encryption
            var encryptor = aesEncryptor.CreateEncryptor();

            //byte[] to hold the encrypted result
            byte[] encryptedResultArray = null;

            //Get a memorystream to hold the encrypted byte[] temporarily
            using (var memoryStream = new MemoryStream())
            //Get a cryptostream to transform the byte[] we want encrypted
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                //Write the input byte[] to the cryptoStream
                cryptoStream.Write(bytesToEncrypt, 0, bytesToEncrypt.Length);
                //Flush the final block to ensure transformation
                cryptoStream.FlushFinalBlock();
                //Assign the encryptedResultArray to the contents of the memory stream
                encryptedResultArray = memoryStream.ToArray();
            }

            //return the encrypted results
            return encryptedResultArray;
        }

        /// <summary>
        /// Decrypt a byte[] using a given key and IV.
        /// </summary>
        /// <param name="bytesToDecrypt">Encrypted byte[] to be decrypted</param>
        /// <param name="key">Key to be used for decryption</param>
        /// <param name="iv">IV to be used for decryption</param>
        /// <returns>Decrypted string</returns>
        private string DecryptBytes(byte[] bytesToDecrypt, string key, string iv)
        {
            //Get an encryptor using the provided key and IV
            var aesEncryptor = GetAesEncryptor(key, iv);
            //Get an ICryptoTransform for actual transformation
            var decryptor = aesEncryptor.CreateDecryptor();

            //String to hold the decrypted result
            string decryptedResultString = null;

            //Get a MemoryStream containing the byte[] to be decrypted
            using (var memoryStream = new MemoryStream(bytesToDecrypt))
            //Get a CryptoStream for decryption built off the memory stream
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            //Get a StreamReader with the results of the cryptostream
            using (var streamReader = new StreamReader(cryptoStream))
            {
                //Read the stream to end, thus decrypting it
                decryptedResultString = streamReader.ReadToEnd();
                _logging.LogMessage("Authentication", "DecryptBytes", LogSeverityTypes.Debug, "Raw decrypted result string: {0}", decryptedResultString);

                //Remove all null bytes from the stream
                while (decryptedResultString.Contains('\0'))
                {
                    decryptedResultString = decryptedResultString.Remove(decryptedResultString.IndexOf('\0'), 1);
                }

                //Remove the salt value
                if (decryptedResultString.Contains(_serverResponseSalt))
                {
                    //Trim the crap before the salt - as of the hacky fix for snipa's fucking up the auth server,
                    //we're getting a bunch of crap at the start of the response.
                    decryptedResultString = decryptedResultString.Remove(0, decryptedResultString.IndexOf(_serverResponseSalt));

                    decryptedResultString = decryptedResultString.Remove(
                        decryptedResultString.IndexOf(_serverResponseSalt), _serverResponseSalt.Length);
                }
            }

            //Return the decrypted result
            return decryptedResultString;
        }

        /// <summary>
        /// Instantiate and return an AES encryptor/decryptor
        /// </summary>
        /// <param name="key">The key used for encryption/decryption</param>
        /// <param name="iv">The IV used for encryption/decryption</param>
        /// <returns>Initialized RijndaelManaged object</returns>
        private RijndaelManaged GetAesEncryptor(string key, string iv)
        {
            //Create an instance of the RijndaelManaged object
            var aesEncryptor = new RijndaelManaged();
            //Set the encryptor's key
            aesEncryptor.Key = CryptographicExtensions.StringToByteArray(key);
            //Set the encryptor's IV
            aesEncryptor.IV = CryptographicExtensions.StringToByteArray(iv);
            //Use CBC cipher mode and Zeroes padding
            aesEncryptor.Mode = CipherMode.CBC;
            aesEncryptor.Padding = PaddingMode.Zeros;
            //return the encryptor
            return aesEncryptor;
        }

		private byte[] GenerateHardwareHash()
		{
		    var hashValues = new List<string>();

		    var hddValues = GetHddIDs();
		    hashValues.Add(hddValues);

		    var macAddresses = GetMacAddresses();
		    hashValues.Add(macAddresses);

			//now put all these srings in one big-ass byte[]
			var hashStringBuilder = new StringBuilder();
		    for (var index = 0; index < hashValues.Count; index++)
		    {
                if (index > 0)
                    hashStringBuilder.Append("|");

		        var hashValue = hashValues[index];
		        hashStringBuilder.Append(hashValue);
		    }
		    return Encoding.ASCII.GetBytes(hashStringBuilder.ToString());
		}

		//These courtesy of Lecht @ LavishNet/QuakeNet. What he didn't write, I based off his.
        //Very little of the original code remains, but I'm leaving this here as thanks.

		private string GetHddIDs()
		{
		    var primaryHddID = "PrimaryHddID:";

            using (var managementClass = new ManagementClass("Win32_DiskDrive"))
            using (var wmiDisks = managementClass.GetInstances())
            {
                foreach (ManagementObject wmiDisk in wmiDisks)
                {
                    wmiDisk.Get();
                    primaryHddID = string.Concat(primaryHddID, (string)wmiDisk.Properties["SerialNumber"].Value);
                }
            }

		    return primaryHddID;
		}

        private string GetMacAddresses()
        {
            var returnValue = new StringBuilder();
            returnValue.Append("MacAddresses:");

            var macAddresses = new List<string>();

            using (var managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            using (var networkAdapters = managementClass.GetInstances())
            {
                foreach (ManagementObject networkAdapter in networkAdapters)
                {
                    var macAddress = (string) networkAdapter.Properties["MacAddress"].Value;

                    if (string.IsNullOrEmpty(macAddress)) continue;
                    
                    macAddresses.Add(macAddress);
                }
            }

            for (var index = 0; index < macAddresses.Count; index++)
            {
                if (index > 0)
                    returnValue.Append(",");

                returnValue.Append(macAddresses[index]);
            }

            return returnValue.ToString();
        }

        private static BigInteger GetRandomBigInteger(BigInteger p)
        {
            BigInteger randomNumber = null;

            while ((object)randomNumber == null || randomNumber >= p)
            {
                var random = new Random();
                randomNumber = new BigInteger();
                randomNumber.genRandomBits(256, random);
            }

            return randomNumber;
        }
    }

    internal sealed class SbAllPolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint servicePoint, X509Certificate certificate, WebRequest webRequest, int certificateProblem)
        {
            return true;
        }
    }

    internal sealed class AuthStateInfo
    {
        public Auth Auth;
        public AuthParams AuthParams;

        public AuthStateInfo(Auth auth, AuthParams authParams)
        {
            Auth = auth;
            AuthParams = authParams;
        }
    }

    internal sealed class AuthParams
    {
        public string Username = string.Empty;
        public string Password = string.Empty;

        public AuthParams(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

	public class __err_retn : EventArgs
	{
		public bool DidAuthenticationFail;
		public string AuthenticationResult;
        public bool CanUseTestBuilds;
		public __err_retn(bool connSucceeded, string loginResult, bool isTester)
		{
			DidAuthenticationFail = connSucceeded;
			AuthenticationResult = loginResult;
            CanUseTestBuilds = isTester;
		}
	}

	//"fuckedup" -> hex, just incase the obfuscator fails me a bit
	public enum y6675636b65647570
	{
		//I'm gonna fuck with people and fuck with these. "Metatronftw!!!" -> hex, two byte chunks
        /// <summary>
        /// Successful
        /// </summary>
		x7374,
        /// <summary>
        /// Error_IncorrectEmail
        /// </summary>
		x6561,
        /// <summary>
        /// Error_IncorrectPassword
        /// </summary>
		x6c74,
        /// <summary>
        /// Error_AccountUnpaid
        /// </summary>
		x6862,
        /// <summary>
        /// Error_AccountLocked
        /// </summary>
		x6f74,
        /// <summary>
        /// Error_TooManyInstances
        /// </summary>
		x6674,
        /// <summary>
        /// Error_MySQLConnectionFailed
        /// </summary>
		x7721,
        /// <summary>
        /// Error_Unknown
        /// </summary>
		x2121,
		x7375,	//Fake results just to fuck with people. "suckmyhairydick!" -> hex, two-byte chunks
		x636b,
		x6d79,
		x6861,
		x6972,
		x7964,
		x6963,
		x6b21
	}
}
