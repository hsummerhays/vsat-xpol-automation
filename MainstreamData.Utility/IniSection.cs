// <copyright file="IniSection.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// Based on code snipped found at http://forums.techpowerup.com/showthread.php?t=74722 orignally posted by FordGT90Concept.

namespace MainstreamData.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using MainstreamData.ExceptionHandling;
    
    /// <summary>
    /// Supports the IniFile class
    /// </summary>
    public sealed class IniSection : Dictionary<string, string>
    {
        /// <summary>
        /// Adds an ini file line.
        /// </summary>
        /// <param name="line">The line to add.</param>
        public void Add(string line)
        {
            if (line.Length != 0)
            {
                int index = line.IndexOf('=');
                if (index != -1)
                {
                    base.Add(line.Substring(0, index), line.Substring(index + 1, line.Length - index - 1));
                }
                else if (line.Substring(0, 1) == "#")
                {
                    base.Add("__comment" + this.Count.ToString(CultureInfo.InvariantCulture), line);
                }
                else
                {
                    throw new ConfigurationException("Keys must have an equal sign.");
                }
            }
            else
            {
                base.Add("__blank" + this.Count.ToString(CultureInfo.InvariantCulture), string.Empty);
            }
        }

        /// <summary>
        /// Converts a key to a string.
        /// </summary>
        /// <param name="key">The key to convert.</param>
        /// <returns>The resulting key/value pair.</returns>
        public string ToString(string key)
        {
            return key + "=" + this[key];
        }

        /// <summary>
        /// Returns all of the keys available.
        /// </summary>
        /// <returns>Keys available.</returns>
        public string[] GetKeys()
        {
            string[] output = new string[this.Count];
            byte i = 0;
            foreach (KeyValuePair<string, string> item in this)
            {
                output[i] = item.Key;
                i++;
            }

            return output;
        }

        /// <summary>
        /// Checks to see if the key exists.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the key exists.</returns>
        public bool HasKey(string key)
        {
            foreach (KeyValuePair<string, string> item in this)
            {
                if (item.Key == key)
                {
                    return true;
                }
            }

            return false;
        }
    }
}