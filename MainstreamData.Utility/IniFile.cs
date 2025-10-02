// <copyright file="IniFile.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// Based on code snipped found at http://forums.techpowerup.com/showthread.php?t=74722 orignally posted by FordGT90Concept.

namespace MainstreamData.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using MainstreamData.ExceptionHandling;

    /// <summary>
    /// Provides easy access to ini files.
    /// </summary>
    public sealed class IniFile : Dictionary<string, IniSection>
    {
        // TODO: Update this class to use GetValue(section, key) instead of dictionary so is more CLR compliant and so can throw more specific errors (like section "xyz" doesn't exist or key "abc" not found).

        /// <summary>
        /// Path to ini file.
        /// </summary>
        private string filePath;

        /// <summary>
        /// Initializes a new instance of the IniFile class.
        /// </summary>
        /// <param name="filePath">Path to the ini file.</param>
        public IniFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ConfigurationException("\"" + filePath + "\" not found.");
            }

            this.filePath = filePath;
        }

        /// <summary>
        /// Initializes a new instance of the IniFile class.
        /// Both the local app folder, and the c:\msd\config0 folder will be check for
        /// an ini file with the same name as the app.
        /// </summary>
        public IniFile()
        {
            // Check local and msd file paths if no path was given
            string appPath = ApplicationInfo.Path + "\\"
                + ApplicationInfo.Name + ".ini";
            string msdPartPath = "\\msd\\config0\\" + ApplicationInfo.Name + ".ini";
            string msdPathC = "c:" + msdPartPath;
            string msdPathD = "d:" + msdPartPath;
            string msdPathE = "e:" + msdPartPath;

            if (File.Exists(appPath))
            {
                this.filePath = appPath;
            }
            else if (File.Exists(msdPathC))
            {
                this.filePath = msdPathC;
            }
            else if (File.Exists(msdPathD))
            {
                this.filePath = msdPathD;
            }
            else if (File.Exists(msdPathE))
            {
                this.filePath = msdPathE;
            }
            else
            {
                throw new ConfigurationException(
                    "\"" + appPath + "\" and \"" + msdPartPath +
                        "\" (on drive c, d, or e) were not found.");
            }
        }

        /// <summary>
        /// Gets path to ini file.
        /// </summary>
        public string FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        /// <summary>
        /// Adds a section or key/value pair to the ini file.
        /// </summary>
        /// <param name="line">The line of text to add to the ini file</param>
        /// <returns>The line with any brackets removed.</returns>
        public string Add(string line)
        {
            if (line.StartsWith("[", StringComparison.Ordinal))
            {
                line = line.TrimStart('[');
            }

            if (line.EndsWith("]", StringComparison.Ordinal))
            {
                line = line.TrimEnd(']');
            }

            this.Add(line, new IniSection());

            return line;
        }

        /// <summary>
        /// Loads the ini file into memory.
        /// </summary>
        public void Load()
        {
            using (StreamReader sr = new StreamReader(this.filePath))
            {
                string section = string.Empty;
                while (sr.Peek() != -1)
                {
                    string read = sr.ReadLine();
                    if (read.StartsWith("[", StringComparison.Ordinal) && read.EndsWith("]", StringComparison.Ordinal))
                    {
                        section = this.Add(read);
                    }
                    else
                    {
                        if (section.Length != 0)
                        {
                            this[section].Add(read);
                        }
                        else
                        {
                            throw new ConfigurationException("Ini file must start with a section.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes the ini file to disk.
        /// </summary>
        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(this.filePath))
            {
                foreach (string section in this.Keys)
                {
                    sw.WriteLine("[" + section + "]");
                    foreach (string key in this[section].Keys)
                    {
                        // Check from comments and blank lines (denoted by keys starting with double underscore (__)).
                        if (key.Substring(0, 2) == "__")
                        {
                            sw.WriteLine(this[section][key]);
                        }
                        else
                        {
                            sw.WriteLine(key + "=" + this[section][key]);
                        }
                    }

                    sw.Flush();
                }
            }
        }

        /// <summary>
        /// Tells you whether or not this specific INI file exists or not.
        /// </summary>
        /// <returns>True if it is found, false if it is not.</returns>
        public bool Exists()
        {
            if (File.Exists(this.filePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes the INI file.
        /// </summary>
        public void Delete()
        {
            File.Delete(this.filePath);
            this.Clear();
        }

        /// <summary>
        /// Moves the INI file to a different location and updates all internal references to it.
        /// </summary>
        /// <param name="path">The place to move it to.</param>
        public void Move(string path)
        {
            File.Move(this.filePath, path);
            this.filePath = path;
        }

        /// <summary>
        /// Returns all of the sections contained in the ini file.
        /// </summary>
        /// <returns>Sections contained in the ini file.</returns>
        public string[] GetSections()
        {
            string[] output = new string[this.Count];
            byte i = 0;
            foreach (KeyValuePair<string, IniSection> item in this)
            {
                output[i] = item.Key;
                i++;
            }

            return output;
        }

        /// <summary>
        /// Checks to see if a section exists.
        /// </summary>
        /// <param name="section">The section to check for.</param>
        /// <returns>True if the section exists.</returns>
        public bool HasSection(string section)
        {
            // TODO: Decide if this provides any value over this.ContainsKey.
            foreach (KeyValuePair<string, IniSection> item in this)
            {
                if (item.Key == section)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
