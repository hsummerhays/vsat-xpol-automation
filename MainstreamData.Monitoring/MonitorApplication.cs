// <copyright file="MonitorApplication.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using MainstreamData.Logging;
    using MainstreamData.Utility;

    /// <summary>
    /// Class to provide main entry point for the monitor application and/or service.
    /// </summary>
    public static class MonitorApplication
    {
        /// <summary>
        /// Initializes static members of the MonitorApplication class.
        /// </summary>
        static MonitorApplication()
        {
            // Attach global exception handlers.
            if (Environment.UserInteractive)
            {
                Application.ThreadException +=
                    new ThreadExceptionEventHandler(MonitorApplication.Application_ThreadException);
            }

            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(MonitorApplication.CurrentDomain_UnhandledException);
        }

        /// <summary>
        /// The main entry point for the application.
        /// When running in application mode, uses title, product, or assembly name (whichever is not blank).
        /// </summary>
        /// <param name="monitorPoint">The monitorPoint to use.</param>
        public static void Start(MonitorPoint monitorPoint)
        {
            try
            {
                try
                {
                    // Setup security for mutex so don't get access denied error in application mode.
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    MutexSecurity mutexsecurity = new MutexSecurity();
                    mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
                    mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
                    mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

                    // Create a mutex to check if this is the first instance of the application.
                    // firstInstance is updated by the mutex.
                    bool firstInstance;
                    using (new Mutex(false, "Global\\" + ApplicationInfo.Name + ":" + ApplicationInfo.Guid, out firstInstance, mutexsecurity))
                    {
                        if (Environment.UserInteractive)
                        {
                            // Start as form.
                            MonitorApplication.RunForm(monitorPoint, firstInstance);
                        }
                        else
                        {
                            // Start as service.
                            ServiceBase[] servicesToRun;
                            servicesToRun = new ServiceBase[] { new MonitorService(monitorPoint) };
                            ServiceBase.Run(servicesToRun);
                        }
                    }
                }
                finally
                {
                    monitorPoint.Dispose();
                }
            }
            catch (Exception ex)
            {
                MonitorApplication.HandleException("An unhandled exception was encountered in program's Main method.  Shutdown in progress.", ex);
            }
        }

        /// <summary>
        /// Starts the windows form if not already running.
        /// </summary>
        /// <param name="monitorPoint">The monitor point to use.</param>
        /// <param name="firstInstance">Indicates whether the program is already running or not.</param>
        private static void RunForm(MonitorPoint monitorPoint, bool firstInstance)
        {
            if (firstInstance)
            {
                string name = string.IsNullOrEmpty(ApplicationInfo.Title) ? ApplicationInfo.Product : ApplicationInfo.Title;
                name = string.IsNullOrEmpty(name) ? ApplicationInfo.Name : name;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MPForm(name, monitorPoint));
            }
            else
            {
                MessageBox.Show(
                    "Another instance of this application is already running.",
                    ApplicationInfo.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        /// <summary>
        /// Last chance exception handler for the application.
        /// </summary>
        /// <param name="message">An additional message to include when logging the exception.</param>
        /// <param name="ex">The object that got the exception.</param>
        private static void HandleException(string message, Exception ex)
        {
            string errorTitle = ApplicationInfo.Name + " Error";
            try
            {
                ExtendedLogger.WriteException(message, Category.Exception, Priority.High, ex);
                string errorMessage = "An unhandled exception occurred and has " +
                    "been logged. Please contact support.";
                MonitorApplication.ShowMessageInNewThread(errorMessage, errorTitle);
            }
            catch (Exception innerEx)
            {
                string errorMessage = "An unexpected exception occured while " +
                    "attempting to log an exception. ";
                errorMessage += Environment.NewLine + innerEx.ToString();
                MonitorApplication.ShowMessageInNewThread(errorMessage, errorTitle);
            }
        }

        /// <summary>
        /// Calls MessageBox.Show within a new thread and returns immediately.
        /// </summary>
        /// <param name="text">The text to display in the message box.</param>
        /// <param name="caption">The text to display in the title bar of the message box.</param>
        private static void ShowMessageInNewThread(string text, string caption)
        {
            MessageBoxOptions option = Environment.UserInteractive ? MessageBoxOptions.DefaultDesktopOnly : MessageBoxOptions.ServiceNotification;
            var thread = new Thread(() => MessageBox.Show(
                text,
                caption,
                MessageBoxButtons.OK,
                MessageBoxIcon.Stop,
                MessageBoxDefaultButton.Button1,
                option));
            thread.Start();
        }

        /// <summary>
        /// Handles exceptions raised during Application_ThreadException or ones raised in a separate thread.
        /// Note: System.Timers.Timer swallows exceptions, so wrap those handlers in try/catch and log separately.
        /// </summary>
        /// <param name="sender">The object that got the exception.</param>
        /// <param name="ex">The exception details.</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            if (ex.ExceptionObject is Exception)
            {
                MonitorApplication.HandleException("CurrentDomain_UnhandledException encountered.  Application shutting down.", (Exception)ex.ExceptionObject);

                // Prevent the OS from showing its own message since we have logged the error and can't abort the exit.
                Environment.Exit((int)Priority.High);
            }
        }

        /// <summary>
        /// Handles exceptions that occur on this application's main thread (non-service only).
        /// </summary>
        /// <param name="sender">The object that got the exception.</param>
        /// <param name="ex">The execption details.</param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs ex)
        {
            MonitorApplication.HandleException("Application_ThreadException occurred.  Recovery is possible.", ex.Exception);

            // If code gets here, the application will actually continue.
        }
    }
}