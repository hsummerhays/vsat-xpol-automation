// <copyright file="MonitorPoint.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring
{
    using System;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Timers;
    using MainstreamData.ExceptionHandling;
    using MainstreamData.Logging;
    using MainstreamData.Utility;

    // TODO: Write to log if unable to initialize other classes (e.g. EventHandler can't create event log entry in constructor because doesn't have rights to).

    /// <summary>
    /// Abstract MonitorPoint class for monitoring just about anything.
    /// Provides a timer and a logging system, along with dispose functionality.
    /// Is thread safe.
    /// This class and related code are loosely based on the MediasFtpMP written in C++.
    /// </summary>
    public abstract class MonitorPoint : IDisposable
    {
        // TODO: See if threading can be handled more effectively so that this class can be more encapsulated (perhaps using Invoke??).

        /// <summary>
        /// Allows threads to wait for each other to complete.
        /// </summary>
        private static AutoResetEvent waitHandle = new AutoResetEvent(true);

        /// <summary>
        /// Flag for tracking setup status.  Gets cleared by reconfig command.
        /// </summary>
        private bool setupComplete = false;

        /// <summary>
        /// Object for interfacing with the HAL database.
        /// </summary>
        private HalController hal;

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Timer for calling MonitorNow.
        /// </summary>
        private System.Timers.Timer monitorTimer;

        /// <summary>
        /// Timer for calling CheckForCommand.
        /// </summary>
        private System.Timers.Timer commandCheckTimer;

        /// <summary>
        /// Used to prevent multiple monitorNow instances from being queued up.
        /// </summary>
        private bool monitorNowQueued = false;

        /// <summary>
        /// Used to prevent multiple commandCheck instances from being queued up.
        /// </summary>
        private bool commandCheckQueued = false;

        /// <summary>
        /// Object for interfacing with the conehead database.
        /// </summary>
        private ConeheadController conehead = new ConeheadController();

        /// <summary>
        /// Deletes log files after given number of days
        /// </summary>
        private RollingFileDeleter logFileDeleter = null;

        /// <summary>
        /// Initializes a new instance of the MonitorPoint class.
        /// This default constructor sets timer to 1 minute.
        /// </summary>
        protected MonitorPoint()
            : this(1 * 60, 1 * 60, 7)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MonitorPoint class.
        /// </summary>
        /// <param name="monitorIntervalSeconds">Interval in seconds for monitor timer to fire.</param>
        /// <param name="commandIntervalSeconds">Interval in seconds for command check timer to fire.</param>
        /// <param name="logFilesToKeep">Number of log file days to keep before deleting old files.  0 indicates to keep all files.</param>
        protected MonitorPoint(int monitorIntervalSeconds, int commandIntervalSeconds, int logFilesToKeep)
        {
            // Don't overflow timer interval
            if (monitorIntervalSeconds > int.MaxValue / 1000)
            {
                monitorIntervalSeconds = int.MaxValue / 1000;
            }

            if (commandIntervalSeconds > int.MaxValue / 1000)
            {
                commandIntervalSeconds = int.MaxValue / 1000;
            }

            // Setup timers (interval and event handler)
            this.monitorTimer = new System.Timers.Timer(monitorIntervalSeconds * 1000);
            this.monitorTimer.Elapsed += new ElapsedEventHandler(this.MonitorTimer_Elapsed);
            this.commandCheckTimer = new System.Timers.Timer(commandIntervalSeconds * 1000);
            this.commandCheckTimer.Elapsed += new ElapsedEventHandler(this.CommandTimer_Elapsed);

            //// TODO: This is a hack - see RollingFileDeleter TODO for additional info.
            this.logFileDeleter = new RollingFileDeleter(logFilesToKeep);
        }

        /// <summary>
        /// Finalizes an instance of the MonitorPoint class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~MonitorPoint()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Provides a delegate for calling the AtemptMethod method.
        /// </summary>
        private delegate void AttemptMethodDelegate();

        /// <summary>
        /// Gets the smartcode type for a Monitor Point.  See hal database smartcode table.
        /// </summary>
        protected static string SmartcodeType
        {
            get 
            {
                return "MP";
            }
        }

        /// <summary>
        /// Gets or sets object for interfacing with the HAL database.
        /// </summary>
        protected virtual HalController Hal
        {
            get
            {
                return this.hal;
            }

            set
            {
                this.hal = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether setup is complete or not.  Gets cleared by reconfig command.
        /// </summary>
        protected bool SetupComplete
        {
            get
            {
                return this.setupComplete;
            }

            set
            {
                this.setupComplete = value;
            }
        }

        /// <summary>
        /// Gets or sets the running timer interval (how often the MonitorNow method is called).
        /// </summary>
        protected int MonitorTimerIntervalSeconds
        {
            get
            {
                return (int)this.monitorTimer.Interval / 1000;
            }

            set
            {
                if (value > int.MaxValue / 1000)
                {
                    this.monitorTimer.Interval = int.MaxValue;
                }
                else
                {
                    this.monitorTimer.Interval = value * 1000;
                }
            }
        }

        /// <summary>
        /// Gets or sets the paused timer interval (how often the CheckForCommand method is called when paused).
        /// </summary>
        protected int CommandTimerIntervalSeconds
        {
            get
            {
                return (int)this.commandCheckTimer.Interval / 1000;
            }

            set
            {
                if (value > int.MaxValue / 1000)
                {
                    this.commandCheckTimer.Interval = int.MaxValue;
                }
                else
                {
                    this.commandCheckTimer.Interval = value * 1000;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of log file days to keep before deleting old files.  0 indicates to keep all files.
        /// </summary>
        protected int LogFilesToKeep
        {
            get
            {
                return this.logFileDeleter.FilesToKeep;
            }

            set
            {
                this.logFileDeleter.FilesToKeep = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether monitoring is paused or not.
        /// </summary>
        protected virtual bool Paused
        {
            get
            {
                return !this.monitorTimer.Enabled;
            }

            set
            {
                if (value)
                {
                    this.monitorTimer.Stop();
                }
                else
                {
                    this.monitorTimer.Start();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the monitor point stop method has been called.
        /// </summary>
        protected virtual bool Stopped { get; private set; }

        /// <summary>
        /// Logs startup message, starts timers, processes commands, and kicks off monitoring.
        /// </summary>
        public void Start()
        {
            this.Start(null);
        }

        /// <summary>
        /// Logs startup message, starts timers, processes commands, and kicks off monitoring.
        /// </summary>
        /// <param name="windowsForm">Windows form object to refresh when starting up.</param>
        public void Start(System.Windows.Forms.Form windowsForm)
        {
            try
            {
                // Log the startup.
                string message = ApplicationInfo.Name + " version " + ApplicationInfo.Version + " started.";
                message += "  Running as " + (Environment.UserInteractive ? "application" : "service") + ".";
                ExtendedLogger.Write(
                    message,
                    Category.General,
                    Priority.Low);

                // Start debugger if in debug mode.
                MonitorPoint.StartDebugger();

                // Refresh the windows form.
                if (windowsForm != null)
                {
                    windowsForm.Show();
                    windowsForm.Refresh();     // Show will not make the controls draw themselves - force it with refresh.
                }

                // Do remaining startup in separate thread so can be done with service/application startup now.
                Thread monitorNow = new Thread(new ThreadStart(this.StartupCheck));
                monitorNow.Start();
            }
            finally
            {
                // Start timers - this is in finally block so the app can continue even if there are errors elsewhere.
                //  Doesn't actually do any work until the timers elapse.
                this.commandCheckTimer.Start();
                this.Paused = false;
                this.Stopped = false;
            }
        }

        /// <summary>
        /// Stops the timers and logs shutdown message.
        /// </summary>
        public void Stop()
        {
            this.Stopped = true;
            this.Paused = true;
            this.commandCheckTimer.Stop();

            // Wait for commandCheck thread to finish if needed.
            MonitorPoint.waitHandle.WaitOne();

            // Reset the wait handle in case MonitorPoint is started again.
            MonitorPoint.waitHandle.Set();

            // Log the shutdown.
            ExtendedLogger.Write(ApplicationInfo.Name + " shutdown.", Category.General, Priority.Low);
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
        /// Sets up everything needed to monitor websites and log information in HAL.
        /// Should only be called indirectly via AttemptMethod to ensure threading is handled properly.
        /// The this.hal object must be initialized before calling this method - initialize it in the child Setup method.
        /// Must set this.setupComplete in child Setup method - is best to use try/finally so setupComplete is false if everything didn't finish.
        /// </summary>
        /// <returns>True if setup was successful.</returns>
        protected virtual bool Setup()
        {
            if (this.setupComplete)
            {
                return true;
            }

            // Setup conehead.
            try
            {
                this.conehead.RetrieveData();
            }
            catch (ConfigurationException)
            {
                return false;
            }

            // TODO: See if HAL object can be setup here instead of subclass.
            ////    The problem is that HAL wants to create an MP record if
            ////    one doesn't already exist, and we don't have the information
            ////    needed in this base class - only the subclass does.
            ////    This problem can be solved by getting the information from the
            ////    ini file - see TODO notes in MediasWebsiteMP.cs setup method.
            ////    This refers to the [MPDefaults] section of the ini file.
            ////    HalController.alertTypeCodes and CreateMonitorPoint code
            ////    can be used to store the alertTypesCodes.

            // Setup hal.
            try
            {
                // Setup stringbuilder for hal.
                SqlConnectionStringBuilder conStringBuilder =
                    new SqlConnectionStringBuilder();
                conStringBuilder.DataSource = this.conehead.ConfigParams["HalServer"];
                conStringBuilder.InitialCatalog =
                    this.conehead.ConfigParams["HalDatabase"];
                conStringBuilder.UserID = this.conehead.ConfigParams["HalUser"];
                conStringBuilder.Password = this.conehead.ConfigParams["HalPassword"];

                // Setup hal.
                this.hal.ConnectionStringBuilder = conStringBuilder;
                this.hal.ServerKeyId = Convert.ToInt32(this.conehead.ConfigParams["ServerKey"], CultureInfo.InvariantCulture);
                this.hal.GetMonitorPointData();
            }
            catch (ConfigurationException)
            {
                return false;
            }

            if (!this.IsMethodOverridden("Setup"))
            {
                this.SetupComplete = true;
            }

            // Not setting setupComplete here because expecting it to be done in child method.
            return true;
        }

        /// <summary>
        /// Performs the monitoring activity.
        /// </summary>
        protected abstract void MonitorNow();

        /// <summary>
        /// Checks for a command.  Should call ExecuteCommand if one was found.
        /// </summary>
        protected abstract void CheckForCommand();

        /// <summary>
        /// Executes a command - pause, resume, and reconfig are currently supported.
        /// Should only be called indirectly via AttemptMethod-CheckForCommand.
        /// </summary>
        /// <param name="command">The command to be executed.</param>
        /// <returns>Whether or not the command executed successfully.</returns>
        protected bool ExecuteCommand(string command)
        {
            // As long as only called indirectly via AttemptExecuteMethod, will only run one instance at a time.

            // Get on with it...
            if (command.Equals("pause"))
            {
                // Put into pause mode.
                this.Paused = true;
                ExtendedLogger.Write(
                    "Pause command processed.",
                    Category.General,
                    Priority.Low);
                return true;
            }
            else if (command.Equals("resume"))
            {
                ExtendedLogger.Write(
                    "Resume started.",
                    Category.General,
                    Priority.Low);

                this.Paused = false;

                // TODO: Decide if should run this in a separate thread.
                this.MonitorNow();   // Calling from this current running thread, so not using AttemptMonitorNow.

                ExtendedLogger.Write(
                    "Resume command processed.",
                    Category.General,
                    Priority.Low);
                return true;
            }
            else if (command.Equals("none") || string.IsNullOrEmpty(command))
            {
                // Default command, ignore.
            }
            else if (command.Equals("reconfig"))
            {
                this.Paused = true;

                ExtendedLogger.Write(
                    "Reconfig started.",
                    Category.General,
                    Priority.Low);

                // Run setup so any changes will be retrieved and put into effect (e.g. timer interval values updated).
                // TODO: Decide if should call setup in separate thread.
                this.SetupComplete = false;
                this.Setup();   // Calling from this current running thread, so not using AttemptSetup.
                this.Paused = false;

                // TODO: Decide if should call in separate thread.
                this.MonitorNow();   // Calling from this current running thread, so not using AttemptMonitorNow.

                ExtendedLogger.Write(
                    "Reconfig command processed",
                    Category.General,
                    Priority.Low);
                return true;
            }
            else
            {
                ExtendedLogger.Write(
                    "Unknown command was encountered: " + command + ".",
                    Category.General,
                    Priority.High);
            }

            return false;
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
                        this.monitorTimer.Dispose();
                        this.commandCheckTimer.Dispose();
                        if (this.logFileDeleter != null)
                        {
                            this.logFileDeleter.Dispose();
                        }

                        this.conehead.Dispose();
                    }

                    // Clean up unmanaged resources (if any)
                }

                this.disposed = true;
            }
            ////finally
            {
                // Currently there is no base to dispose.
                ////base.dispose(disposing);
            }
        }

        /// <summary>
        /// Starts Visual Studio if compiled in debug mode (even for service).
        /// </summary>
        [Conditional("DEBUG")]
        private static void StartDebugger()
        {
            // Break code if running outside of visual studio.
            string parentName = ApplicationInfo.ParentProcess.ProcessName;
            if (!parentName.Equals("devenv") && !parentName.Equals("VCSExpress"))
            {
                Debugger.Break();
            }
        }

        /// <summary>
        /// Makes sure a pause command isn't pending, then runs MonitorNow.  Is separate from Start so can be in its own thread.
        /// </summary>
        private void StartupCheck()
        {
            // Check for command.
            this.AttemptMethod(new AttemptMethodDelegate(this.CheckForCommand));

            // Run MonitorNow if not paused.
            if (!this.Paused)
            {
                this.AttemptMethod(new AttemptMethodDelegate(this.MonitorNow));
            }
        }

        /// <summary>
        /// Event wired method that calls method to check for command.
        /// </summary>
        /// <param name="sender">Object that called the method.</param>
        /// <param name="e">Arguments passed to the method.</param>
        private void CommandTimer_Elapsed(object sender, EventArgs e)
        {
            //// TODO: See about using System.Threading.Timer instead, but make sure is used in thread safe manner.
            // System.Timers.Timer swallows exceptions - log any that happen here.
            try
            {
                ExtendedLogger.WriteDebug("MonitorPoint.CommandTimer_Elapsed Running");
                this.AttemptMethod(new AttemptMethodDelegate(this.CheckForCommand));
            }
            catch (Exception ex)
            {
                string message = "Unhandled exception detected in Command timer thread.";
                ExtendedLogger.WriteException(message, Category.General, Priority.High, ex);
                throw;
            }
        }

        /// <summary>
        /// Event wired method that calls method to update sofware stats.
        /// </summary>
        /// <param name="sender">Object that called the method.</param>
        /// <param name="e">Arguments passed to the method.</param>
        private void MonitorTimer_Elapsed(object sender, EventArgs e)
        {
            //// TODO: See about using System.Threading.Timer instead, but make sure is used in thread safe manner.
            // System.Timers.Timer swallows exceptions - log any that happen here.
            try
            {
                ExtendedLogger.WriteDebug("MonitorPoint.MonitorTimer_Elapsed Running");
                this.AttemptMethod(new AttemptMethodDelegate(this.MonitorNow));
            }
            catch (Exception ex)
            {
                string message = "Unhandled exception detected in MonitorPoint timer thread.";
                ExtendedLogger.WriteException(message, Category.General, Priority.High, ex);
                throw;
            }
        }

        /// <summary>
        /// Runs method in thread safe manner (for use with CheckForCommand, and MonitorNow).  It exits if another instance is running.
        /// </summary>
        /// <param name="methodToRun">The method to attempt to call.</param>
        private void AttemptMethod(AttemptMethodDelegate methodToRun)
        {
            // Find out which method we are running.
            string methodName = methodToRun.Method.Name;
            bool isMonitor = methodName.Equals("MonitorNow", StringComparison.Ordinal);
            bool isCommand = methodName.Equals("CheckForCommand", StringComparison.Ordinal);

            // Want to run command, block monitorNow, then run monitorNow.
            bool isOkToContinue = true;
            if (isMonitor)
            {
                // Exit on subsequent calls of monitorNow if one is already queued.
                isOkToContinue &= !this.monitorNowQueued;

                // Don't monitor if commandCheckTimer is enabled and MonitorPoint setup isn't complete yet.
                isOkToContinue &= !(this.commandCheckTimer.Enabled && !this.setupComplete);
            }
            else
            {
                // Exit on subsequent calls of command if one is already queued.
                isOkToContinue &= !(isCommand && this.commandCheckQueued);
            }

            // This method is called within its own thread.  Only execute one instance at a time.
            if (isOkToContinue)
            {
                // Mark as queued so won't continue to wait.  If two threads get here at the same time, oh well, so it runs twice sometimes.
                if (isCommand)
                {
                    this.commandCheckQueued = true;
                }
                else if (isMonitor)
                {
                    this.monitorNowQueued = true;
                }

                // Wait for other threads are complete.
                if (waitHandle.WaitOne())
                {
                    try
                    {
                        // TODO: See if can figure out way to avoid monitor right after reconfig (e.g. was blocking monitor, then monitor ran).  Could compare timestamp and interval.
                        // Make sure monitorTimer wasn't disabled in another thread (e.g. ExecuteCommand - Pause).
                        isOkToContinue &= !isMonitor || this.monitorTimer.Enabled;

                        // Check setup then call the method.
                        if (isOkToContinue && this.Setup())
                        {
                            ExtendedLogger.WriteDebug("MonitorPoint." + methodToRun.Method.Name + " Running");
                            methodToRun();
                        }
                    }
                    finally
                    {
                        if (isCommand)
                        {
                            this.commandCheckQueued = false;
                        }
                        else if (isMonitor)
                        {
                            this.monitorNowQueued = false;
                        }

                        // Let another thread run now.
                        waitHandle.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the method is overridden in a derived class.
        /// </summary>
        /// <param name="methodName">Name of method to check.</param>
        /// <returns>True if method is overridden.</returns>
        private bool IsMethodOverridden(string methodName)
        {
            // TODO: See about moving this to the Utils assembly and making it static and public.
            // Adapted from sample by Phil Harding.
            Type type = this.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo == null)
            {
                return false;
            }

            string declaringType = methodInfo.DeclaringType.FullName;
            return declaringType.Equals(type.FullName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
