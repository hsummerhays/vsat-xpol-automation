// <copyright file="RollingFileDeleter.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Logging
{
    using System;
    using System.IO;
    using System.Timers;
    using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
    using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;

    // TODO: Create Rolling Flat File Trace Listener with purging capability and dump this class - see examples on Internet (especially by derekwilson).

    /// <summary>
    /// Deletes rolling log files from a given folder.
    /// </summary>
    public class RollingFileDeleter : IDisposable
    {
        /// <summary>
        /// The begining for the fileName used for the files.
        /// </summary>
        private FileInfo rollingFile;

        /// <summary>
        /// The number of files to keep in the folder.
        /// </summary>
        private int filesToKeep;

        /// <summary>
        /// The timer to delete the files periodically.
        /// </summary>
        private Timer deleteLogFileTimer = new Timer(60 * 60 * 24 * 1000);

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the RollingFileDeleter class.
        /// This class assumes that files are sorted in date order and will delete in ascending order.
        /// Warning: It will delete anything named fileNameStart*.ext.  Make sure your fileNameStart 
        /// is unique enough to avoid deleting files that shouldn't be deleted.
        /// </summary>
        /// <param name="filesToKeep">Number of files to keep after deletion.  0 indicates to keep all.</param>
        public RollingFileDeleter(int filesToKeep)
            : this(filesToKeep, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RollingFileDeleter class.
        /// This class assumes that files are sorted in date order and will delete in ascending order.
        /// Warning: It will delete anything named fileNameStart*.ext.  Make sure your fileNameStart 
        /// is unique enough to avoid deleting files that shouldn't be deleted.
        /// </summary>
        /// <param name="filesToKeep">Number of files to keep after deletion.  0 indicates to keep all.</param>
        /// <param name="fullFilePath">Full path to log files (e.g. c:\msd\log0\MediasRmpAlerter\MediasRmpAlerter.log).
        /// Note: This will normally come from the TraceListener configured in app.config.</param>
        public RollingFileDeleter(int filesToKeep, string fullFilePath)
        {
            this.deleteLogFileTimer.Elapsed += new ElapsedEventHandler(this.DeleteLogFileTimer_Tick);

            // Read app.config to get RollingFlatFileTraceListener fileName.
            if (string.IsNullOrEmpty(fullFilePath))
            {
                IConfigurationSource configSource = ConfigurationSourceFactory.Create();
                LoggingSettings settings = LoggingSettings.GetLoggingSettings(configSource);
                foreach (TraceListenerData listener in settings.TraceListeners)
                {
                    if (listener.Type.Equals(
                        typeof(Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners.RollingFlatFileTraceListener)))
                    {
                        fullFilePath = listener.ElementInformation.Properties["fileName"].Value.ToString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(fullFilePath))
                {
                    throw new MainstreamData.ExceptionHandling.ConfigurationException(
                        "No RollingFlatFileTraceListener was found.");
                }
            }

            this.rollingFile = new FileInfo(fullFilePath);
            this.filesToKeep = filesToKeep;

            this.PurgeExcessFiles();
            this.deleteLogFileTimer.Start();
        }

        /// <summary>
        /// Finalizes an instance of the RollingFileDeleter class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~RollingFileDeleter()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the number of files to keep.  0 indicates to keep all.
        /// </summary>
        public int FilesToKeep
        {
            get
            {
                return this.filesToKeep;
            }

            set
            {
                this.filesToKeep = value;
            }
        }

        /// <summary>
        /// Purges the older log files.
        /// </summary>
        public void PurgeExcessFiles()
        {
            // TODO: Make it so FilesToKeep goes by dates, not actual number of files.
            if (this.filesToKeep == 0)
            {
                return;
            }

            // TODO: Throw exception if directory doesn't exist?
            DirectoryInfo dir = this.rollingFile.Directory;
            if (!dir.Exists)
            {
                ExtendedLogger.Write("Folder \"" + dir.FullName + "\" does not exist.", Category.Config, Priority.High);
                return;
            }

            string pattern = this.rollingFile.Name.Replace(".", "*.");

            FileInfo[] files = dir.GetFiles(pattern);
            int filesToPurge = files.Length - this.filesToKeep;
            if (filesToPurge <= 0)
            {
                return;
            }

            Array.Sort(files, new CompareFileInfoEntries(CompareByOptions.FileName));

            int filesPurged = filesToPurge;
            foreach (FileInfo file in files)
            {
                file.Delete();
                ExtendedLogger.Write("The following old log file was deleted: " + file.FullName, Category.General, Priority.Low);
                filesToPurge--;
                if (filesToPurge < 1)
                {
                    break;
                }
            }
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
                        this.deleteLogFileTimer.Dispose();
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

        /// <summary>
        /// The method for handling the timer event.
        /// </summary>
        /// <param name="sender">The object that call the method.</param>
        /// <param name="e">The details about the event.</param>
        private void DeleteLogFileTimer_Tick(object sender, EventArgs e)
        {
            //// TODO: See about using System.Threading.Timer instead, but make sure is used in thread safe manner.
            // System.Timers.Timer swallows exceptions - log any that happen here.
            try
            {
                this.PurgeExcessFiles();
            }
            catch (Exception ex)
            {
                string message = "Unhandled exception detected in DeleteLogFileTimer thread.";
                ExtendedLogger.WriteException(message, Category.General, Priority.High, ex);
                throw;
            }
        }
    }
}