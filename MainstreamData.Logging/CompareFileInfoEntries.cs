// <copyright file="CompareFileInfoEntries.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Logging
{
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>
    /// Enum list of ways to compare files.
    /// </summary>
    internal enum CompareByOptions
    {
        /// <summary>
        /// Compare by filename.
        /// </summary>
        FileName,

        /// <summary>
        /// Compare by last write time.
        /// </summary>
        LastWriteTime,

        /// <summary>
        /// Compare by length.
        /// </summary>
        Length,
    }

    /// <summary>
    /// IComparer for FileInfo entries for Array.Sort.
    /// </summary>
    internal class CompareFileInfoEntries : IComparer
    {
        /// <summary>
        /// The selected option to compare by.
        /// </summary>
        private CompareByOptions compareBy = CompareByOptions.FileName;
        
        /// <summary>
        /// Initializes a new instance of the CompareFileInfoEntries class.
        /// </summary>
        /// <param name="compareBy">The option to compare by.</param>
        public CompareFileInfoEntries(CompareByOptions compareBy)
        {
            this.compareBy = compareBy;
        }
   
        /// <summary>
        /// Compares two files by the selected compareBy option.
        /// </summary>
        /// <param name="file1">The first file.</param>
        /// <param name="file2">The second file.</param>
        /// <returns>An integer that indicates their relationship to each other in the sort order.</returns>
        int IComparer.Compare(object file1, object file2)
        {
            // Convert file1 and file2 to FileInfo entries
            FileInfo f1 = (FileInfo)file1;
            FileInfo f2 = (FileInfo)file2;

            // Compare the file names
            if (this.compareBy.Equals(CompareByOptions.FileName))
            {
                return string.Compare(f1.Name, f2.Name, StringComparison.OrdinalIgnoreCase);
            }
            else if (this.compareBy.Equals(CompareByOptions.LastWriteTime))
            {
                return DateTime.Compare(f1.LastWriteTime, f2.LastWriteTime);
            }
            else if (this.compareBy.Equals(CompareByOptions.Length))
            {
                return (int)(f1.Length - f2.Length);
            }

            return -1;
        }
    }
}
