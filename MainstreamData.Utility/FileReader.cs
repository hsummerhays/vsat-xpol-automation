// <copyright file="FileReader.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Utility
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Helps with reading files (especially text based log files).
    /// </summary>
    public static class FileReader
    {
        /// <summary>
        /// Gets all of the text from the specified file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>Text from file.</returns>
        public static string GetFileText(string path)
        {
            // Note: Error handling is done by calling method
            return FileReader.GetFileText(path, 0);
        }

        /// <summary>
        /// Gets text from the specified file starting at the specified location.
        /// Note: Error handling is done by calling method.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="startPos">Position in the file to start reading the text from.</param>
        /// <returns>Text from file starting at startPos through to the end of the file.</returns>
        public static string GetFileText(string path, long startPos)
        {
            string text;
            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader streamReader = new StreamReader(file))
                {
                    streamReader.SetPosition(startPos);
                    text = streamReader.ReadToEnd();
                }
            }

            return text;
        }

        /// <summary>
        /// Gets text from the specified file starting at the specified location,
        /// up to the last line feed.
        /// Note: Error handling is done by calling method.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="startPos">Position in the file to start reading the text from.</param>
        /// <returns>Text from file starting at startPos through to the last line feed.</returns>
        public static string GetFileTextToLastLF(string path, int startPos)
        {
            string text = GetFileText(path, startPos);

            int lastLfPos = text.LastIndexOf("\n", 0, StringComparison.Ordinal);
            if (lastLfPos == -1)
            {
                return string.Empty;
            }
            else
            {
                return text.Substring(0, lastLfPos + 1);
            }
        }

        /// <summary>
        /// Gets the first line of the log file and splits it into columns which are returned as a TextSplitter with one row.
        /// </summary>
        /// <param name="path">Path to the log file.</param>
        /// <param name="delimiter">The character separating the columns.</param>
        /// <returns>TextSplitter with one row of data.</returns>
        public static TextSplitter GetFileTextFirstLineSplit(string path, char delimiter)
        {
            StringBuilder text = new StringBuilder();
            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    text.Append(sr.ReadLine());
                }
            }

            TextSplitter splitText = new TextSplitter(text, "\r\n", delimiter);
            return splitText;
        }

        /// <summary>
        /// Checks the specified path to see if a file exists.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>True if file exists, otherwise false.</returns>
        public static bool FileExists(string path)
        {
            FileInfo file = new FileInfo(path);
            return file.Exists;
        }

        /// <summary>
        /// Gets the file length from the specified file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>Length of file in bytes.</returns>
        public static long GetFileLength(string path)
        {
            FileInfo file = new FileInfo(path);
            return file.Length;
        }
    }
}
