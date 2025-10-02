// <copyright file="ExtensionMethods.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Data
{
    using System;
    using System.Data.SqlTypes;

    /// <summary>
    /// Contains extension methods for this namespace.
    /// </summary>
    public static class ExtensionMethods
    {
        // TODO: Consider adding GetNullableValue method for MySqlDataReader - see Newscom SellMedia GetWebData application for example.

        /// <summary>
        /// Gets a nullable or default value depending on type specified.
        /// </summary>
        /// <typeparam name="T">Type of value to return. Specify a nullable value if want nulls returned, otherwise a null value will return as the default value for that type.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <returns>If null then returns null or default depending on type, otherwise simply returns the value.</returns>
        public static T ToNullable<T>(this object value)
        {
            return value == null || value == DBNull.Value ? default(T) : (T)value;
        }

        /// <summary>
        /// Returns a nullable DateTime from an object.
        /// </summary>
        /// <param name="dateTimeValue">The object to get the DateTime from.</param>
        /// <returns>Nullable DateTime.</returns>
        public static DateTime? ToNullableDateTime(this object dateTimeValue)
        {
            return dateTimeValue.ToNullable<DateTime?>();
        }

        /// <summary>
        /// Returns a nullable Int from an object.
        /// </summary>
        /// <param name="intValue">The object to get the Int from.</param>
        /// <returns>Nullable Int.</returns>
        public static int? ToNullableInt(this object intValue)
        {
            return intValue.ToNullable<int?>();
        }
    }
}
