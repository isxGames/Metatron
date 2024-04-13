using System;
using System.Security.Cryptography;
using System.Text;

namespace Metatron.Core.Extensions
{
	public static class CryptographicExtensions
	{
		public static string HexStringToAscii(this string hexString)
		{
			var asciiStringBuilder = new StringBuilder();
			hexString = hexString.Replace(" ", "");
			for (var i = 0; i < hexString.Length; i += 2)
			{
				asciiStringBuilder.Append(Convert.ToChar(Convert.ToByte(hexString.Substring(i, 2), 16)));
			}
			return asciiStringBuilder.ToString();
		}

		/// <summary>
		/// Convert a string of hexadecimal characters to a byte[]
		/// </summary>
		/// <param name="hexString"></param>
		/// <returns></returns>
		public static byte[] StringToByteArray(this string hexString)
		{
		    var bytes = new byte[hexString.Length / 2];
			for (var i = 0; i < hexString.Length; i += 2)
				bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
			return bytes;
		}

		/// <summary>
		/// Convert a byte array to a hexadecimal string
		/// </summary>
		/// <param name="byteArray"></param>
		/// <returns></returns>
		public static string ByteArrayToString(this byte[] byteArray)
		{
			return BitConverter.ToString(byteArray).Replace("-", "");
		}

		/// <summary>
		/// Convert an integer to a byte[]
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] IntToByteArray(this int value)
		{
			return new[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value};
		}

		/// <summary>
		/// Convert a byte[] to an integer.
		/// </summary>
		/// <param name="byteArray"></param>
		/// <returns></returns>
		public static int ByteArrayToInt(this byte[] byteArray)
		{
			return (byteArray[0] << 24)
					+ ((byteArray[1] & 0xFF) << 16)
					+ ((byteArray[2] & 0xFF) << 8)
					+ (byteArray[3] & 0xFF);
		}

        /// <summary>
        /// Get an MD5 hash from a given string.
        /// </summary>
        /// <param name="stringToHash"></param>
        /// <returns></returns>
		public static string ToMd5Hash(this string stringToHash)
		{
			var textBytes = Encoding.Default.GetBytes(stringToHash);
            var hashStringBuilder = new StringBuilder();

			using (var cryptHandler = new MD5CryptoServiceProvider())
			{
			    var hashBytes = cryptHandler.ComputeHash(textBytes);
			    foreach (var hashByte in hashBytes)
			    {
			        hashStringBuilder.Append(hashByte.ToString("x2"));
			    }
			}

            return hashStringBuilder.ToString();
		}
	}
}
