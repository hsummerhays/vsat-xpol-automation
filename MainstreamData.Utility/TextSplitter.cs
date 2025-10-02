// <copyright file="TextSplitter.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    
    /// <summary>
    /// Splits delimited text into rows and columns.
    /// </summary>
    public class TextSplitter
    {
        /// <summary>
        /// Stores a list of columns within a list of rows.
        /// </summary>
        private List<List<string>> rows = new List<List<string>>();

        /// <summary>
        /// Initializes a new instance of the TextSplitter class.
        /// </summary>
        /// <param name="text">The delimited text to split.</param>
        /// <param name="rowDelimiter">The delimiter used to separate rows.</param>
        /// <param name="colDelimiter">The delimiter used to separate columns.</param>
        public TextSplitter(StringBuilder text, string rowDelimiter, char colDelimiter)
        {
            // Convert row delimiter to an array so that it is compatible with Split.
            string[] rowDelimArray = new string[] { rowDelimiter };

            // Load the text into row and column lists
            List<string> rows = new List<string>(text.ToString().Split(rowDelimArray, StringSplitOptions.None));
            foreach (string row in rows)
            {
                List<string> cols = new List<string>(row.Split(colDelimiter));
                this.rows.Add(cols);
            }
        }

        /// <summary>
        /// Gets the rows property.
        /// </summary>
        public List<List<string>> Rows
        {
            get
            {
                return this.rows;
            }
        }
    }
}
