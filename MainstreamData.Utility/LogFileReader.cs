// <copyright file="LogFileReader.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Utility
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// An id to indicate which log file the data came from.
    /// See tSegment table in the reuwne database for more info.
    /// </summary>
    public enum EventSourceId
    {
        /// <summary>
        /// Provide a zero value id.
        /// </summary>
        None,

        /// <summary>
        /// ID for Reuters WNE FileDirect log.
        /// </summary>
        ReutersWneFileDirectLog,

        /// <summary>
        /// ID for Reuters WNE Player log.
        /// </summary>
        ReutersWnePlayerLog,

        /// <summary>
        /// ID for Reuters WNE z-Band log.
        /// </summary>
        ReutersWneZbandLog,

        /// <summary>
        /// ID for Medias server Input Controller log.
        /// </summary>
        MediasServerInputControllerLog,

        /// <summary>
        /// ID for Medias server FTP Manager log.
        /// </summary>
        MediasServerFtpManagerLog,

        /// <summary>
        /// ID for Reuters WNE playout history
        /// </summary>
        ReutersWnePlayoutHistory,

        /// <summary>
        /// ID for events for any type of hardware - not currently in use.
        /// </summary>
        Hardware,

        /// <summary>
        /// ID for Reuters FTP Suite software.
        /// </summary>
        ReutersFtpSuite
    }

    /// <summary>
    /// Provides an easy way to retrieve data from log files and put it into a DataTable.
    /// </summary>
    public class LogFileReader : IDisposable
    {
        /// <summary>
        /// Contains a list of log files within a given path.
        /// </summary>
        private DataTable logFileTable = new DataTable();

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the LogFileReader class.
        /// </summary>
        public LogFileReader()
        {
            // TODO: See about combining efforts here with TextSplitter and ComputerStats.GetFileText(string path, int startPos).
            // Get new data from log file by checking last location for matching data then getting rest.
            this.logFileTable.Locale = CultureInfo.InvariantCulture;
            this.logFileTable.Columns.Add("FileName", typeof(string));
            this.logFileTable.Columns.Add("FileDate", typeof(DateTime));
        }

        /// <summary>
        /// Finalizes an instance of the LogFileReader class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~LogFileReader()
        {
            this.Dispose(false);
        }
        
        /// <summary>
        /// Updates the DataTable from a log file.
        /// </summary>
        /// <param name="folderPath">Full or relative path to the log file.</param>
        /// <param name="fileName">Name of the log file including extension.</param>
        /// <param name="lastEventTime">DateTime of the last event that was retrieved from the log file.</param>
        /// <param name="serverId">The serverID of the current PC.</param>
        /// <param name="eventSourceIDEnum">The value from the EventSourceID enum that matches the type of the current log file.</param>
        /// <param name="eventTable">The DataTable that the log file data is loaded into.</param>
        /// <returns>The DateTime of the last event in the log file.</returns>
        public DateTime UpdateTable(string folderPath, string fileName, DateTime lastEventTime, int serverId, EventSourceId eventSourceIDEnum, ref DataTable eventTable)
        {
            this.logFileTable.Clear();

            // TODO: Improve this class by making properties and contructor overloads
            // TODO: remember log file size and only read new entries next time around

            // Get names and dates of log files
            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
            FileInfo[] logFiles = dirInfo.GetFiles(fileName);
            foreach (FileInfo logFile in logFiles)
            {
                DateTime fileTime = logFile.LastWriteTimeUtc;
                if (fileTime.Date >= lastEventTime.Date)
                {
                    this.logFileTable.Rows.Add(new object[] { logFile.FullName, fileTime });
                }
            }

            // iterate through each file
            this.logFileTable.DefaultView.Sort = "[FileDate] asc";
            DateTime maxEventTime = new DateTime();
            foreach (DataRowView row in this.logFileTable.DefaultView)
            {
                StreamReader sr = new StreamReader(row["FileName"].ToString());
                string text = sr.ReadToEnd();
                sr.Close();

                // iterate through each row
                string[] textRows = text.Split('\n');
                foreach (string textRow in textRows)
                {
                    string[] fields = textRow.Split('\t');

                    // TODO: catch errors so can keep going even after a bad entry
                    //// try
                    {
                        // ignore blank rows
                        if (fields.GetUpperBound(0) == 4)
                        {
                            DateTime eventTime = Convert.ToDateTime(fields[0] + " " + fields[1], new CultureInfo("en-US"));
                            if (eventTime >= lastEventTime)
                            {
                                DataRow eventRow = eventTable.NewRow();
                                eventRow.BeginEdit();
                                eventRow["iServerID"] = serverId;
                                eventRow["zModule"] = fields[2] + " " + fields[3];
                                eventRow["zDescription"] = fields[4];
                                eventRow["oEventTime"] = eventTime;
                                eventRow["iEventSourceID"] = (int)eventSourceIDEnum;
                                eventTable.Rows.Add(eventRow);
                                //// _settings.LastPlayerTime = eventTime;
                                if (maxEventTime < eventTime)
                                {
                                    maxEventTime = eventTime;
                                }
                            }
                        }
                    }
                    //// catch
                    {
                        // TODO: write error to log
                    }
                }
            }

            return maxEventTime;
        }

        /// <summary>
        /// Cleans up resources to avoid having to wait for garbage collection
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Prevent garbage collection from calling finalize again since we already cleaned up.
            GC.SuppressFinalize(this);
        } 

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check disposed field so Dispose does not get done more than once
            ////try
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        // Clean up managed resources
                        this.logFileTable.Dispose();
                    }

                    // Clean up unmanaged resources (if any)
                }

                this.disposed = true;
            }
            ////finally
            {
                ////base.dispose(disposing);
            }
        }
    }
}
