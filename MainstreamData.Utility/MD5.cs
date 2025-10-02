// <copyright file="MD5.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Utility
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Class to provide MD5 password hashing.
    /// </summary>
    public static class MD5
    {
        /// <summary>
        /// Retrieves the hash value for a given string.
        /// </summary>
        /// <param name="value">The value to hash.</param>
        /// <returns>Hashed value.</returns>
        public static string HashString(string value)
        {
            byte[] data = new MD5CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes(value));

            StringBuilder hashedString = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                hashedString.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return hashedString.ToString();
        }
    }
}
