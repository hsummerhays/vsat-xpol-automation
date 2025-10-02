// -----------------------------------------------------------------------
// <copyright file="VsatXpolRmp.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Resources.NeutralResourcesLanguageAttribute("en")]
[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolRmp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.ServiceModel;
    using System.Timers;
    using MainstreamData.Logging;
    using MainstreamData.Monitoring;
    using MainstreamData.Monitoring.VsatXpol;
    using MainstreamData.Monitoring.VsatXpol.VsatXpolRmp.Properties;

    /// <summary>
    /// Provides core functionality for the application.
    /// </summary>
    internal class VsatXpolRmp : MonitorPoint
    {
        /// <summary>
        /// A ServiceHost object to provide a WCF interface.
        /// </summary>
        [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "WCF ServiceHost")]
        public ServiceHost ServiceHost = null;

        /// <summary>
        /// A list of recent error messages that can be retrieved by <see cref="VsatXpolRmpHost"/>.
        /// </summary>
        private static Queue<string> errorMessageQueue = new Queue<string>();

        /// <summary>
        /// Timer for calling <see cref="InitializeIsolationAnalyzer"/>.
        /// </summary>
        private Timer reinitializeTimer = new Timer(24 * 60 * 60 * 1000);

        /// <summary>
        /// Tracks the number of times that setup has failed, so can back off the timer after a number of failures,
        /// See <see cref="Setup"/> method.
        /// </summary>
        private int beaconFindFailureCount = 0;

        /// <summary>
        /// Track whether or not an instance of this class has been disposed. Need a separate private copy for the child class.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes static members of the <see cref="VsatXpolRmp"/> class. IMPORTANT: Must dispose the <see cref="IsolationAnalyzer"/> reference when closing app.
        /// </summary>
        static VsatXpolRmp()
        {
            // Note: If wanted to, could get rid of FxCop rule CA1810 warning by creating a local variable and private method (e.g. private static IsolationAnalyzer isolationAnalyzer = initIsoAnalyzer();).
            VsatXpolRmp.IsolationAnalyzer = new IsolationAnalyzer();
            VsatXpolRmp.IsolationAnalyzer.Error +=
                new EventHandler<AnalyzerEventArgs>(VsatXpolRmp.IsoAnalyzer_Error);
            VsatXpolRmp.IsolationAnalyzer.BeaconsFoundEvent +=
                new EventHandler<AnalyzerEventArgs>(VsatXpolRmp.IsoAnalyzer_BeaconsFound);
            ExtendedLogger.MessageLogged += new EventHandler<LogEventArgs>(VsatXpolRmp.ExtendedLogger_MessageLogged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsatXpolRmp"/> class.
        /// </summary>
        public VsatXpolRmp()
            : base()
        {
            this.reinitializeTimer.Elapsed += new ElapsedEventHandler(this.ReinitializeTimer_Elapsed);
            this.reinitializeTimer.Start();
        }

        /// <summary>
        /// Gets or sets a reference to the <see cref="IsolationAnalyzer"/> being used to provide data to this class.
        /// </summary>
        public static IsolationAnalyzer IsolationAnalyzer { get; set; }

        /// <summary>
        /// Gets a list of RMP error messages that have been queued up.
        /// </summary>
        public static List<string> RecentErrorMessages
        {
            get
            {
                return new List<string>(VsatXpolRmp.errorMessageQueue);
            }
        }

        /// <summary>
        /// Rescans the beacon frequencies and uses the current list of CWs in app.config.
        /// </summary>
        public static void InitializeIsolationAnalyzer()
        {
            // TODO: Make sure settings are valid. Would be good to have isoAnalyzer and SpecAnalyzer throw ArgumentOutOfRangeException and just catch that.
            // TODO: Implement pause correctly if decide to support commands.

            // Setup analyzer with settings from app.config.
            Settings settings = Settings.Default;
            IsolationAnalyzer isoAnalyzer = VsatXpolRmp.IsolationAnalyzer;
            isoAnalyzer.CopolAnalyzerGpibBoardNumber = settings.SpectrumAnalyzerGpibBoardNumCopol;
            isoAnalyzer.CopolAnalyzerAddress = settings.SpectrumAnalyzerAddressCopol;
            isoAnalyzer.XpolAnalyzerGpibBoardNumber = settings.SpectrumAnalyzerGpibBoardNumXpol;
            isoAnalyzer.XpolAnalyzerAddress = settings.SpectrumAnalyzerAddressXpol;

            // TODO: Decide if want to remove this commented out code completely.
            /*Signal copolBeacon = new Signal(SpecAnalyzer.ConvertToHertz(settings.BeaconLbandFreqMhzCopol), SignalType.Beacon);
            Signal xpolBeacon = new Signal(SpecAnalyzer.ConvertToHertz(settings.BeaconLbandFreqMhzXpol), SignalType.Beacon);

            StringCollection stringList = settings.CWLbandFreqMhzList;
            List<SignalPair> clearWaveList = new List<SignalPair>();
            foreach (string frequency in stringList)
            {
                clearWaveList.Add(new SignalPair(SpecAnalyzer.ConvertToHertz(frequency)));
            }*/

            if (isoAnalyzer.CopolBeacon.Frequency == 0)
            {
                ExtendedLogger.Write("Waiting for someone to call the VsatXpolRmpHost.Initialize method.", Category.Config, Priority.Low);
            }
            else
            {
                /*isoAnalyzer.Stop();
                isoAnalyzer.CopolBeacon = copolBeacon;
                isoAnalyzer.XpolBeacon = xpolBeacon;
                isoAnalyzer.AddNewClearWaveList(clearWaveList);*/
                ExtendedLogger.Write("IsolationAnalyzer Init: Calling IsolationAnalyzer.Reset() to start beacon find", Category.Config, Priority.Medium);
                isoAnalyzer.Reset();  // Also stops and starts it.
            }
        }

        /// <summary>
        /// Sets up the monitor point.
        /// </summary>
        /// <returns>True if setup succeeded.</returns>
        protected override bool Setup()
        {
            // Not going to call base.Setup since don't want to connect to conehead or hal directly.
            // Don't setup again if already complete - allows us to keep retrying stuff if it isn't setup when the application first starts
            if (this.SetupComplete)
            {
                return true;
            }

            // Don't need CommandTimer since VsatXpolRmpHost can be programmed to receive commands.
            this.CommandTimerIntervalSeconds = 86400 * 7;   // Setting to 7 days since don't have access to disable.

            this.beaconFindFailureCount = 0;
            VsatXpolRmp.InitializeIsolationAnalyzer();

            // Configure the WCF interface.
            if (this.ServiceHost != null)
            {
                this.ServiceHost.Close();
            }

            // Create a ServiceHost for the VsatXpolRmpHost type and provide the base address (via app.config).
            this.ServiceHost = new ServiceHost(typeof(VsatXpolRmpHost));

            // Open the ServiceHostBase to create listeners and start listening for messages.
            this.ServiceHost.Open();
            this.SetupComplete = true;

            return this.SetupComplete;
        }

        /// <summary>
        /// Normally performs the main monitoring function. In this MP though, <see cref="VsatXpolRmpHost"/> handles most of it.
        /// </summary>
        protected override void MonitorNow()
        {
            // Retry beacon find 10 times, then wait for command to reconfig.
            // TODO: Implement command to reconfig or add MonitorApplication.Stop method and just stop after so many tries.
            // TODO: Review the initialize methods here and in VsatXpolRmpHost to see if improvements and simplifications can be made.
            // TODO: Come up with a better way to see if IsolationAnalyzer has been initialized other than checking for CopolBeacon.Frequency != 0.
            IsolationAnalyzer isoAnalyzer = VsatXpolRmp.IsolationAnalyzer;
            if (!isoAnalyzer.BeaconsFound && isoAnalyzer.CopolBeacon.Frequency != 0 && this.beaconFindFailureCount < 10)
            {
                this.beaconFindFailureCount++;

                // Attempt another find.
                ExtendedLogger.Write("Calling IsolationAnalyzer.Reset() to restart beacon find.", Category.Config, Priority.Medium);
                isoAnalyzer.Reset();
            }
        }

        /// <summary>
        /// Checks for new commands.
        /// </summary>
        protected override void CheckForCommand()
        {
            // TODO: Add code to check for commands (e.g. pause, reconfig, etc.). See MediasRmp for example.
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Check disposed field so Dispose does not get done more than once
            try
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        // Clean up managed resources - closing the service host should dispose the VsatXpolRmpHost instance properly since the destuctor calls dispose.
                        if (this.ServiceHost != null)
                        {
                            if (this.ServiceHost.State == CommunicationState.Faulted)
                            {
                                this.ServiceHost.Abort();
                            }
                            else
                            {
                                this.ServiceHost.Close();
                            }
                        }

                        VsatXpolRmp.IsolationAnalyzer.Dispose();
                        this.reinitializeTimer.Dispose();
                    }

                    // Clean up unmanaged resources (if any)
                }

                this.disposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Logs any errors that the IsoAnalyzer class may have encountered.
        /// </summary>
        /// <param name="sender">The object that called the method.</param>
        /// <param name="e">The arguments for the method.</param>
        private static void IsoAnalyzer_Error(object sender, AnalyzerEventArgs e)
        {
            ExtendedLogger.Write(e.Message, Category.General, Priority.High);
        }

        /// <summary>
        /// Logs a message when beacons have been found.
        /// </summary>
        /// <param name="sender">The object that called the method.</param>
        /// <param name="e">The arguments for the method.</param>
        private static void IsoAnalyzer_BeaconsFound(object sender, AnalyzerEventArgs e)
        {
            ExtendedLogger.Write("Beacons were successfully found.", Category.Config, Priority.Low);
        }

        /// <summary>
        /// Queues up the last 100 error messages to send back to anyone that requests them.
        /// </summary>
        /// <param name="sender">The object that call this method.</param>
        /// <param name="e">The method arguments.</param>
        private static void ExtendedLogger_MessageLogged(object sender, LogEventArgs e)
        {
            Queue<string> queue = VsatXpolRmp.errorMessageQueue;
            queue.Enqueue(string.Format(
                CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2}",
                (Priority)e.Priority,
                e.Category,
                e.Message));

            if (queue.Count > 100)
            {
                // Discard oldest value.
                queue.Dequeue();
            }
        }

        /// <summary>
        /// Reinitializes the IsolationAnalyzer if MP is not paused.
        /// </summary>
        /// <param name="sender">The object that called this method.</param>
        /// <param name="e">The method arguments.</param>
        private void ReinitializeTimer_Elapsed(object sender, EventArgs e)
        {
            // TODO: Check more often if paused?
            if (!this.Paused)
            {
                VsatXpolRmp.InitializeIsolationAnalyzer();
            }
        }
    }
}