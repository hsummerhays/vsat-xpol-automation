// <copyright file="LogFileReader.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using MainstreamData.Utility;

    /// <summary>
    /// Provides an easy way to retrieve data from log files and put it into a DataTable.
    /// Currently only supports tab delimited log files that have a single entry per line with the date first.
    /// </summary>
    public class LogFileReader : IDisposable
    {
        // TODO: See about combining efforts here with TextSplitter and FileReader.

        /// <summary>
        /// Contains a list of log files found in FolderPath matching filePattern.
        /// </summary>
        private DataTable logFileList = new DataTable();

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Name of last file that was being read.
        /// </summary>
        private string lastFileReadFullName = string.Empty;

        /// <summary>
        /// Text of last line read from current file.
        /// </summary>
        private string lastLineReadText = string.Empty;

        /// <summary>
        /// Position of last line read from current file.
        /// </summary>
        private long lastFileReadPosition = 0;

        /// <summary>
        /// Full name of last file that had too many lines.
        /// </summary>
        private string lastFileWithTooManyLinesFullName = string.Empty;

        /// <summary>
        /// Length of last file that had too many lines.
        /// </summary>
        private long lastFileWithTooManyLinesLength = 0;

        /// <summary>
        /// DateTime of the last event that was retrieved from the log file.
        /// </summary>
        private DateTime? lastReadEventTime = null;

        /// <summary>
        /// A value indicating whether the last CopyLogsToTable method call resulted in all files being parsed completely.
        /// </summary>
        private bool allFilesRead = false;

        /// <summary>
        /// A value indicating whether ReceivedFileRegex is set and thus events should be checked for received files.
        /// </summary>
        private bool checkReceivedFileEvents = false;

        /// <summary>
        /// RegEx string to watch for in Event messages that indicate received files (e.g. "saving file|placed in directory").
        /// </summary>
        private string receivedFileRegex;

        /// <summary>
        /// Initializes a new instance of the LogFileReader class.
        /// </summary>
        public LogFileReader()
            : this((int)EventSourceId.None, string.Empty, string.Empty, null, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LogFileReader class.
        /// </summary>
        /// <param name="eventSourceId">The value from the EventSourceID enum that matches the type of the current log file.</param>
        /// <param name="folderPath">Full or relative path to the log file.</param>
        /// <param name="filePattern">Name pattern of the log file including extension (e.g. MSvrInputController*.log).</param>
        /// <param name="lastReadEventTime">DateTime of the last event that was retrieved from the log file.</param>
        /// <param name="serverId">The serverID of the current PC.</param>
        public LogFileReader(int eventSourceId, string folderPath, string filePattern, DateTime? lastReadEventTime, int serverId)
            : this(eventSourceId, folderPath, filePattern, lastReadEventTime, 24, serverId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LogFileReader class.
        /// </summary>
        /// <param name="eventSourceId">The value from the EventSourceID enum that matches the type of the current log file.</param>
        /// <param name="folderPath">Full or relative path to the log file.</param>
        /// <param name="filePattern">Name pattern of the log file including extension (e.g. MSvrInputController*.log).</param>
        /// <param name="lastReadEventTime">DateTime of the last event that was retrieved from the log file.</param>
        /// <param name="oldestLogEntryHours">Number of hours to go back in the log files if the time field is null.</param>
        /// <param name="serverId">The serverID of the current PC.</param>
        public LogFileReader(int eventSourceId, string folderPath, string filePattern, DateTime? lastReadEventTime, int oldestLogEntryHours, int serverId)
        {
            this.EventSourceIdValue = eventSourceId;

            // Setup DataTable to store log file names.
            this.logFileList.Locale = CultureInfo.InvariantCulture;
            this.logFileList.Columns.Add("FileName", typeof(string));
            this.logFileList.Columns.Add("FileDate", typeof(DateTime));

            // Call Reset method to set some defaults.
            this.Reset(folderPath, filePattern, lastReadEventTime, oldestLogEntryHours, serverId);

            // Set remaining defaults.
            this.MaxBytes = 1024 * 1024 * 1;
            this.MaxFileLinesToRead = 0;
            this.TimeFieldName = string.Empty;
            this.ReadReceivedFileEventsOnly = false;
            this.ReceivedFileRegex = string.Empty;
        }

        /// <summary>
        /// Finalizes an instance of the LogFileReader class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~LogFileReader()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Fires whenever MaxBytes have been read.
        /// </summary>
        public event EventHandler<CancelEventArgs> BufferFull;

        /// <summary>
        /// Fires when MaxFileReadLines has been reached for a given file.
        /// </summary>
        public event EventHandler<CancelEventArgs> MaxLinesRead;

        /// <summary>
        /// Fires when all files have been read. Allows for additional data handling even though buffer isn't full.
        /// </summary>
        public event EventHandler<EventArgs> OnAllFilesRead;

        /// <summary>
        /// Gets or sets the value from the EventSourceID that matches the type of the current log file.
        /// </summary>
        public int EventSourceIdValue { get; set; }

        /// <summary>
        /// Gets or sets the full or relative path to the log file.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// Gets or sets the name pattern of the log file including extension (e.g. MSvrInputController*.log).
        /// </summary>
        public string FilePattern { get; set; }

        /// <summary>
        /// Gets dateTime of the last event that was retrieved from the log file.
        /// </summary>
        public DateTime? LastReadEventTime
        {
            get
            {
                return this.lastReadEventTime;
            }
        }

        /// <summary>
        /// Gets or sets dateTime of the next event to retrieve from the log file.
        /// </summary>
        public DateTime NextEventTimeToRead { get; set; }

        /// <summary>
        /// Gets or sets number of hours to go back in the log files if the time field is null.
        /// </summary>
        public int OldestLogEntryHours { get; set; }

        /// <summary>
        /// Gets or sets the serverID of the current PC.
        /// </summary>
        public int ServerId { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of bytes to read from the log files at one time.
        /// </summary>
        public int MaxBytes { get; set; }

        /// <summary>
        /// Gets or sets the value indicating when the MaxLinesRead event fires. Zero indicates no max.
        /// </summary>
        public int MaxFileLinesToRead { get; set; }

        /// <summary>
        /// Gets or sets the name of the time field from the DB. Is provided simply for storage only.
        /// </summary>
        public string TimeFieldName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the whole log file is read, or just events matching the ReceivedFileRegex.
        /// </summary>
        public bool ReadReceivedFileEventsOnly { get; set; }

        /// <summary>
        /// Gets or sets RegEx string to watch for in Event messages that indicate received files (e.g. "saving file|placed in directory").
        /// </summary>
        public string ReceivedFileRegex
        {
            get
            {
                return this.receivedFileRegex;
            }

            set
            {
                this.receivedFileRegex = value;
                this.checkReceivedFileEvents = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the last UpdateTable method call resulted in all files being parsed completely.
        /// </summary>
        public bool AllFilesRead
        {
            get
            {
                return this.allFilesRead;
            }
        }

        /// <summary>
        /// Gets full path and file name of last file that was read.
        /// </summary>
        public string LastFileReadFullName
        {
            get
            {
                return this.lastFileReadFullName;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether log file reading is paused or not.
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// Resets the object back to its orginal state when it was instatiated.
        /// </summary>
        public void Reset()
        {
            this.allFilesRead = false;
            this.logFileList.Clear();
            this.lastFileReadFullName = string.Empty;
            this.lastLineReadText = string.Empty;
            this.lastFileReadPosition = 0;
        }

        /// <summary>
        /// Resets the object back to its original state, then sets new values based on parameters. Note: CalculateNextEventTimeToRead should be called after calling this method.
        /// </summary>
        /// <param name="folderPath">Full or relative path to the log file.</param>
        /// <param name="filePattern">Name pattern of the log file including extension (e.g. MSvrInputController*.log).</param>
        /// <param name="lastReadEventTime">DateTime of the last event that was retrieved from the log file.</param>
        /// <param name="oldestLogEntryHours">Number of hours to go back in the log files if the time field is null.</param>
        /// <param name="serverId">The serverID of the current PC.</param>
        public void Reset(string folderPath, string filePattern, DateTime? lastReadEventTime, int oldestLogEntryHours, int serverId)
        {
            this.Reset();
            this.ServerId = serverId;
            this.FolderPath = folderPath;
            this.FilePattern = filePattern;

            // Setup read times.
            this.OldestLogEntryHours = oldestLogEntryHours;
            this.lastReadEventTime = lastReadEventTime;
            if (lastReadEventTime == null)
            {
                // Calling with null lastSendTime will always use OldestLogEntryHours regardless of NextEventTimeToRead.
                this.NextEventTimeToRead = this.CalculateNextEventTimeToRead(null, 0);
            }
            else
            {
                this.NextEventTimeToRead = lastReadEventTime.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Updates LastEventTime property based on whether or not log data was sent recently factoring in the daysHistoryToSave.
        /// Allows headend to get older log data by setting LastEventTime to an older date, but won't be older than curTime - daysHistoryToSave.
        /// </summary>
        /// <param name="lastSendTime">Last time that events were sent back to host.</param>
        /// <param name="daysHistoryToSave">Number of days worth of history to save on host.</param>
        /// <returns>The time to start pulling events from the log file for.</returns>
        public DateTime CalculateNextEventTimeToRead(DateTime? lastSendTime, int daysHistoryToSave)
        {
            // TODO: Dig in and figure out when to use UTC time vs. not. Are local log file entry times UTC? Using logFile.CreationTime UTC creates an offset, but copying the files doesn't.
            // Calculate times to use for pulling log entries.
            // Note: Time conversion to/from UTC is handled automatically by c# and mssql.
            DateTime curTime = DateTime.Now;
            DateTime minTime = curTime.AddHours(-this.OldestLogEntryHours);

            // Don't go back more than minTime unless have sent recently and someone turned back the time.
            if (lastSendTime != null && lastSendTime > minTime)
            {
                // Don't go back more days than specified even if time is older.
                DateTime daysToSaveTime = curTime.AddDays(-daysHistoryToSave);
                minTime = this.NextEventTimeToRead < daysToSaveTime ? daysToSaveTime : this.NextEventTimeToRead;
            }

            this.NextEventTimeToRead = minTime;
            return minTime;
        }

        /// <summary>
        /// Must be called when using CopyLogsToTable with exitOnBufferFull if not allFilesRead to ensure that the same records aren't read more than once.
        /// </summary>
        public void RollForwardNextEventTimeToRead()
        {
            if (this.lastReadEventTime != null && this.NextEventTimeToRead < this.lastReadEventTime)
            {
                this.NextEventTimeToRead = this.lastReadEventTime.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Updates the DataTable from a log file. Will keep going until all files have been read. Will fire BufferFull event as appropriate.
        /// </summary>
        /// <param name="eventTable">The DataTable that the log file data is loaded into.</param>
        /// <returns>True if log files were successfully read.</returns>
        public bool CopyLogsToTable(ref DataTable eventTable)
        {
            return this.CopyLogsToTable(ref eventTable, false);
        }

        /// <summary>
        /// Updates the DataTable from a log file. Will keep going until all files have been read or the reader is paused. Will fire BufferFull event as appropriate.
        /// </summary>
        /// <param name="eventTable">The DataTable that the log file data is loaded into. Matches table design of tEvent in MediasRemoteMon DB.</param>
        /// <param name="exitOnFullBuffer">If true, then log file processing stops once buffer is full.
        /// IMPORTANT: You must call RollForwardNextEventTimeToRead if you want to ensure that the same data is not reread again later.</param>
        /// <exception cref="IOException">Path doesn't exist, can't open file, etc.</exception>
        /// <exception cref="DataException">Not able to write data to eventTable.</exception>
        /// <exception cref="FormatException">Data isn't in expected format (e.g. DateTime)</exception>
        /// <exception cref="ArgumentException">Field names from eventTable don't match this code.</exception>
        /// <returns>True if log files were successfully read.
        /// IMPORTANT: If true or paused, you must call RollForwardNextEventTimeToRead if you want to ensure last bit of data is not reread.</returns>
        public bool CopyLogsToTable(ref DataTable eventTable, bool exitOnFullBuffer)
        {
            // lastEventTime defined below will remain the same until this method exits.
            DateTime lastEventTime = this.NextEventTimeToRead;
            this.allFilesRead = false;
            this.logFileList.Clear();

            // Get names and dates of log files. Put them in a DataTable so that they can easily be sorted.
            bool foundPreviousFileInList = false;
            DirectoryInfo dirInfo = new DirectoryInfo(this.FolderPath);
            FileInfo[] logFiles = dirInfo.GetFiles(this.FilePattern);
            foreach (FileInfo logFile in logFiles)
            {
                // Note: Creation time on Medias server log files appears to be same as modified time.
                DateTime fileTime = logFile.CreationTime;

                // TODO: Make sure don't miss some entries if log file rolls over into next day and date changes
                if (fileTime.Date >= lastEventTime.Date)
                {
                    // Don't add files that had too many lines.
                    if (logFile.FullName == this.lastFileWithTooManyLinesFullName && logFile.Length >= this.lastFileWithTooManyLinesLength)
                    {
                        this.lastFileWithTooManyLinesFullName = string.Empty;
                        this.lastFileWithTooManyLinesLength = 0;
                    }
                    else
                    {
                        this.logFileList.Rows.Add(new object[] { logFile.FullName, fileTime });
                        if (logFile.FullName == this.lastFileReadFullName)
                        {
                            foundPreviousFileInList = true;
                        }
                    }
                }
            }

            // Sort the log files and see if the current one is in the list.
            this.logFileList.DefaultView.Sort = "[FileDate] asc";

            // Read each log file.
            bool cancel = this.logFileList.Rows.Count == 0;
            bool foundPreviousFileInOrder = false;
            int bytesRead = 0;
            bool dataWasRead = false;
            DateTime maxEventTime = this.NextEventTimeToRead;
            foreach (DataRowView row in this.logFileList.DefaultView)
            {
                // Don't read any files that come before the last one that was read.
                string fileFullName = row["FileName"].ToString();
                bool jumpToLastPosition = false;
                if (fileFullName == this.lastFileReadFullName)
                {
                    foundPreviousFileInOrder = true;
                    jumpToLastPosition = true;
                }

                if (foundPreviousFileInList && !foundPreviousFileInOrder)
                {
                    break;
                }
                else if (!jumpToLastPosition)
                {
                    this.lastFileReadFullName = fileFullName;
                    this.lastFileReadPosition = 0;
                }

                // Read one line at a time until reach max number of bytes or end of file. Also, open file in read mode without locking.
                using (FileStream file = File.Open(fileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(file))
                    {
                        int linesRead = 0;
                        while (!streamReader.EndOfStream)
                        {
                            if (this.Paused)
                            {
                                cancel = true;
                                break;
                            }
                            else
                            {
                                // Try to jump to last position
                                if (jumpToLastPosition && streamReader.BaseStream.Length > this.lastFileReadPosition)
                                {
                                    streamReader.SetPosition(this.lastFileReadPosition);
                                    string lastLine = streamReader.ReadLine();
                                    jumpToLastPosition = false;
                                    if (lastLine != this.lastLineReadText)
                                    {
                                        streamReader.SetPosition(0);
                                    }
                                }

                                if (!streamReader.EndOfStream)
                                {
                                    // Read the current line
                                    long position = streamReader.GetPosition();
                                    string line = streamReader.ReadLine();
                                    linesRead += 1;

                                    // See if MaxLinesRead event needs to fire.
                                    if (this.MaxFileLinesToRead != 0 && linesRead >= this.MaxFileLinesToRead)
                                    {
                                        this.lastFileWithTooManyLinesFullName = fileFullName;
                                        this.lastFileWithTooManyLinesLength = streamReader.BaseStream.Length;
                                        cancel = this.MaxLinesRead.SafeInvoke(this);
                                        break;
                                    }

                                    // TODO: Allow different delimiting characters or strings. Should even allow multiple lines with field name as first item (maybe colon is fieldname delimiter).
                                    string[] fields = line.Split('\t');

                                    // TODO: catch errors so can keep going even after a bad entry
                                    //// try
                                    {
                                        //// TODO: Allow varying number of fields. Could even pass a field map to get correct array items into correct fields.
                                        // ignore blank rows
                                        if (fields.GetUpperBound(0) == 4)
                                        {
                                            DateTime eventTime = Convert.ToDateTime(fields[0] + " " + fields[1], new CultureInfo("en-US"));
                                            string description = fields[4];
                                            bool readEvent = !(this.checkReceivedFileEvents && this.ReadReceivedFileEventsOnly);   // Always read unless only reading recevied file events.
                                            bool isReceivedFile = false;
                                            if (this.checkReceivedFileEvents)
                                            {
                                                isReceivedFile = Regex.IsMatch(description, this.receivedFileRegex, RegexOptions.IgnoreCase);   // This value will be written to the DB.
                                                readEvent = isReceivedFile || !this.ReadReceivedFileEventsOnly;    // Event gets read whether isReceivedFile or we are reading all events.
                                            }

                                            if (readEvent && eventTime >= lastEventTime)
                                            {
                                                bytesRead += line.Length;
                                                if (bytesRead < this.MaxBytes)
                                                {
                                                    DataRow eventRow = eventTable.NewRow();
                                                    eventRow.BeginEdit();
                                                    eventRow.SetField<int>("iServerID", this.ServerId);
                                                    eventRow.SetField<string>("zModule", fields[2] + " " + fields[3]);
                                                    eventRow.SetField<string>("zDescription", description);
                                                    eventRow.SetField<DateTime>("oEventTime", eventTime);
                                                    eventRow.SetField<int>("iEventSourceID", (int)this.EventSourceIdValue);
                                                    eventRow.SetField<string>("zDescChecksum", MD5.HashString(description));
                                                    eventRow.SetField<int>("bIsReceivedFile", isReceivedFile ? 1 : 0);
                                                    eventRow.EndEdit();
                                                    eventTable.Rows.Add(eventRow);
                                                    dataWasRead = true;
                                                    if (maxEventTime < eventTime)
                                                    {
                                                        maxEventTime = eventTime;
                                                    }

                                                    // TODO: Move this code elsewhere so can jump to last line read instead of last line processed.
                                                    this.lastFileReadPosition = position;
                                                    this.lastLineReadText = line;
                                                }
                                                else
                                                {
                                                    // Buffer is full - deal with it.
                                                    if (exitOnFullBuffer)
                                                    {
                                                        // Since not setting read times, it is up to the calling program to call RollForwardNextEventTimeToRead.
                                                        cancel = true;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        cancel = this.BufferFull.SafeInvoke(this);
                                                        if (cancel)
                                                        {
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            // Event handler finished successfully, so roll forward time.
                                                            this.lastReadEventTime = maxEventTime;
                                                            this.RollForwardNextEventTimeToRead();

                                                            // Reset number of bytes read and continue reading.
                                                            bytesRead = 0;
                                                            dataWasRead = false;
                                                        }
                                                    }
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
                        }
                    }

                    if (cancel)
                    {
                        break;
                    }
                }
            }

            if (dataWasRead)
            {
                this.lastReadEventTime = maxEventTime;
            }

            bool allFilesRead = !cancel;
            this.allFilesRead = allFilesRead;
            if (allFilesRead)
            {
                this.OnAllFilesRead.SafeInvoke(this);
            }

            return allFilesRead;
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
                        this.logFileList.Dispose();
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
