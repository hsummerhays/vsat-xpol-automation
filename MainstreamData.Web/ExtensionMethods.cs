// <copyright file="ExtensionMethods.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Web
{
    using System.Collections.Specialized;

    /// <summary>
    /// Contains extension methods for this namespace.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Determines whether the collection contains a specified key.
        /// </summary>
        /// <param name="collection">The collection object.</param>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the key is found.</returns>
        public static bool ContainsKey(this NameValueCollection collection, string key)
        {
            return !string.IsNullOrEmpty(collection[key]);
        }

        /// <summary>
        /// Retrieves a value based on the specified key.
        /// </summary>
        /// <param name="collection">The collection object.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value for the specified key or empty string if not found.</returns>
        public static string GetValue(this NameValueCollection collection, string key)
        {
            if (collection.ContainsKey(key))
            {
                return collection[key];
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
