// <copyright file="IsolationAnalyzer.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Uses beacons locate clear wave signal and gathers data about the cw.
    /// </summary>
    public class IsolationAnalyzer : IDisposable
    {
        /// <summary>
        /// Used to prevent multiple threads from triggering events unnecessarily.
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// The SpecAnalyzer for the copol signal.
        /// </summary>
        private SpecAnalyzer copolSpecAnalyzer = new SpecAnalyzer();

        /// <summary>
        /// The SpecAnalyzer for the xpol signal.
        /// </summary>
        private SpecAnalyzer xpolSpecAnalyzer = new SpecAnalyzer();

        /// <summary>
        /// The copol beacon signal object.
        /// </summary>
        private Signal copolBeacon = new Signal(SignalType.Beacon);

        /// <summary>
        /// The xpol beacon signal object.
        /// </summary>
        private Signal xpolBeacon = new Signal(SignalType.Beacon);

        /// <summary>
        /// Indicates whether or not to call the BeaconsFound event when the state of both beacons switches to found.
        /// </summary>
        private bool beaconsFoundEventFired = false;

        /// <summary>
        /// Indicates whether or not an alert was sent for the copol beacon not being found.
        /// </summary>
        private bool copolBeaconAlertSent = false;

        /// <summary>
        /// Indicates whether or not an alert was sent for the copol beacon not being found.
        /// </summary>
        private bool xpolBeaconAlertSent = false;

        /// <summary>
        /// The list of clear wave signals to sweep on the SpecAnalyzers.
        /// </summary>
        private List<SignalPair> clearWaveList = new List<SignalPair>();

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the IsolationAnalyzer class.
        /// </summary>
        public IsolationAnalyzer()
        {
            SpecAnalyzer analyzer = this.copolSpecAnalyzer;
            analyzer.Name = "Copol";
            analyzer.Error += new EventHandler<AnalyzerEventArgs>(this.HandleError);
            analyzer.FreshData += new EventHandler<AnalyzerEventArgs>(this.FreshData);
            analyzer.GpibBoardNumber = 0; // This is a default in case user doesn't specify.

            analyzer = this.xpolSpecAnalyzer;
            analyzer.Name = "Xpol";
            analyzer.Error += new EventHandler<AnalyzerEventArgs>(this.HandleError);
            analyzer.FreshData += new EventHandler<AnalyzerEventArgs>(this.FreshData);
            analyzer.GpibBoardNumber = 1;  // Ditto.

            this.ClearWaveList.Add(new SignalPair(1377950000));  // Frequency is just an arbitrary default value.
        }

        /// <summary>
        /// Finalizes an instance of the IsolationAnalyzer class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~IsolationAnalyzer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// An event for dealing with <see cref="IsolationAnalyzer"/> errors.
        /// This is used instead of standard exception handling since the SpecAnalyzer.Sweep methods run in their own threads.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> Error;

        /// <summary>
        /// Fires when both beacons have been found.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> BeaconsFoundEvent;

        /// <summary>
        /// Fires when fresh data is avaiable from the copol analyzer.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> FreshCopolData;

        /// <summary>
        /// Fires when fresh data is avaiable from the xpol analyzer.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> FreshXpolData;

        // TODO: Throw ArgumentOutOfRangeException in set methods as appropriate.

        /// <summary>
        /// Gets or sets the copol analyzer GPIB board number. This must be set before the address or you must call FindBeacon.
        /// </summary>
        public short CopolAnalyzerGpibBoardNumber
        {
            get
            {
                return this.copolSpecAnalyzer.GpibBoardNumber;
            }

            set
            {
                this.Stop();
                this.copolSpecAnalyzer.GpibBoardNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets the xpol analyzer GPIB board number. This must be set before the address or you must call FindBeacon.
        /// </summary>
        public short XpolAnalyzerGpibBoardNumber
        {
            get
            {
                return this.xpolSpecAnalyzer.GpibBoardNumber;
            }

            set
            {
                this.Stop();
                this.xpolSpecAnalyzer.GpibBoardNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets the copol analyzer address.
        /// </summary>
        public short CopolAnalyzerAddress
        {
            get
            {
                return this.copolSpecAnalyzer.DeviceAddress;
            }

            set
            {
                this.Stop();
                this.copolSpecAnalyzer.DeviceAddress = value;
            }
        }

        /// <summary>
        /// Gets or sets the xpol anaylzer address.
        /// </summary>
        public short XpolAnalyzerAddress
        {
            get
            {
                return this.xpolSpecAnalyzer.DeviceAddress;
            }

            set
            {
                this.Stop();
                this.xpolSpecAnalyzer.DeviceAddress = value;
            }
        }

        /// <summary>
        /// Gets or sets the copol beacon object.
        /// </summary>
        public Signal CopolBeacon
        {
            get
            {
                return this.copolBeacon;
            }

            set
            {
                this.Stop();
                this.copolBeacon = value;
                this.UpdateSpecAnalyzerSignalLists();
            }
        }

        /// <summary>
        /// Gets or sets the xpol beacon object.
        /// </summary>
        public Signal XpolBeacon
        {
            get
            {
                return this.xpolBeacon;
            }

            set
            {
                this.Stop();
                this.xpolBeacon = value;
                this.UpdateSpecAnalyzerSignalLists();
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not both beacons have been found.
        /// </summary>
        public bool BeaconsFound
        {
            get
            {
                return this.copolBeacon.State == SignalState.Found && this.xpolBeacon.State == SignalState.Found;
            }
        }

        /// <summary>
        /// Gets list of ClearWaveSignal objects to be scanned.
        /// IMPORTANT: Do not change references within the ClearWaveList directly - replace the ClearWaveList instead.
        /// Otherwise, the SpecAnalyzers won't get updated with the new signals.
        /// </summary>
        public IList<SignalPair> ClearWaveList
        {
            // TODO: Change this to IEnumerable or ReadOnlyCollection, or use Collection and watch for changes.
            get
            {
                return this.clearWaveList;
            }
        }

        /// <summary>
        /// Replaces the current <see cref="ClearWaveList"/> with a new one.
        /// </summary>
        /// <param name="clearWaveList">The new list to use.</param>
        public void AddNewClearWaveList(IList<SignalPair> clearWaveList)
        {
            this.Stop();
            this.clearWaveList.Clear();
            this.clearWaveList.AddRange(clearWaveList);
            this.UpdateSpecAnalyzerSignalLists();
        }

        /// <summary>
        /// Updates the presets for both spectrum analyzers.
        /// </summary>
        /// <param name="settings">The signal settings for the spectrum analyzers to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        public void UpdateSpecAnalyzerPresets(Dictionary<string, SignalSettings> settings)
        {
            this.copolSpecAnalyzer.UpdatePresets(settings);
            this.xpolSpecAnalyzer.UpdatePresets(settings);
        }

        /// <summary>
        /// Stops the isolation analyzer from doing its work.
        /// </summary>
        public void Stop()
        {
            this.copolSpecAnalyzer.StopSweep();
            this.xpolSpecAnalyzer.StopSweep();
        }

        /// <summary>
        /// Causes the analyzers to start sweeping (retrieving the signal data) for xpol isolation.
        /// </summary>
        public void Start()
        {
            // Simply make the SpecAnalyzers start sweeping again. Their current signal lists will take care of the rest.
            this.copolSpecAnalyzer.StartSweep();
            this.xpolSpecAnalyzer.StartSweep();
        }

        /// <summary>
        /// Stops sweeping and finds the beacons again.
        /// </summary>
        public void Reset()
        {
            this.Stop();
            Thread.Sleep(1 * 1000); // TODO: Make so this is not necessary - make so waits until current scans are finished.
            this.copolBeacon.State = SignalState.WideSearch;
            this.xpolBeacon.State = SignalState.WideSearch;
            this.beaconsFoundEventFired = false;
            this.copolBeaconAlertSent = false;
            this.xpolBeaconAlertSent = false;
            this.Start();
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
        /// <param name="disposing">True if resources should be disposed.</param>
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
                        this.copolSpecAnalyzer.Dispose();
                        this.xpolSpecAnalyzer.Dispose();
                    }

                    // Clean up unmanaged resources (if any)
                }

                this.disposed = true;
            }
            ////finally
            {
                ////base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Updates the SignalSweepLists of both SpecAnalyzers.
        /// </summary>
        private void UpdateSpecAnalyzerSignalLists()
        {
            // Build list for copol spec A.
            List<Signal> newCopolList = new List<Signal>();
            List<Signal> newXpolList = new List<Signal>();
            newCopolList.Add(this.copolBeacon);
            newXpolList.Add(this.xpolBeacon);
            foreach (SignalPair signal in this.clearWaveList)
            {
                newCopolList.Add(signal.CopolSignal);

                // Link xpol signal up to copol so can use offset.
                signal.XpolSignal.ClearWaveSignalParent = signal;
                newXpolList.Add(signal.XpolSignal);
            }

            this.copolSpecAnalyzer.AddNewSignalSweepList(newCopolList);
            this.xpolSpecAnalyzer.AddNewSignalSweepList(newXpolList);
        }

        /// <summary>
        /// Provides a way for passing errors up to the class that is using this class.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">The event arguments.</param>
        private void HandleError(object sender, AnalyzerEventArgs e)
        {
            if (this.Error != null)
            {
                this.Error(sender, e);
            }
        }

        /// <summary>
        /// Handle new data provided by the spectrum analyzer.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">Event arguments.</param>
        private void FreshData(object sender, AnalyzerEventArgs e)
        {
            // Check the current state of the beacons and act accordingly.
            if (e.Signal.SignalType == SignalType.Beacon)
            {
                if (!this.copolBeaconAlertSent && this.CopolBeacon.State == SignalState.Inactive)
                {
                    this.copolBeaconAlertSent = true;
                    this.SendBeaconAlert(this.copolBeacon);
                }

                if (!this.xpolBeaconAlertSent && this.XpolBeacon.State == SignalState.Inactive)
                {
                    this.xpolBeaconAlertSent = true;
                    this.SendBeaconAlert(this.xpolBeacon);
                }

                lock (this.lockObject)
                {
                    if (!this.beaconsFoundEventFired && this.BeaconsFound && this.BeaconsFoundEvent != null)
                    {
                        this.beaconsFoundEventFired = true;
                        this.BeaconsFoundEvent(this, e);
                    }
                }
            }

            // Pass the fresh data on to any delegates attached to the event.
            if ((e.Signal.SignalType == SignalType.CopolClearWave || e.Signal.Equals(this.copolBeacon)) && this.FreshXpolData != null)
            {
                this.FreshCopolData(this, e);
            }

            if ((e.Signal.SignalType == SignalType.XpolClearWave || e.Signal.Equals(this.xpolBeacon)) && this.FreshXpolData != null)
            {
                this.FreshXpolData(this, e);
            }
        }

        /// <summary>
        /// Fires the error event to indicate that a beacon was not found.
        /// </summary>
        /// <param name="beacon">The beacon signal that wasn't found.</param>
        private void SendBeaconAlert(Signal beacon)
        {
            bool isCopol = beacon.Equals(this.copolBeacon);
            string beaconName = isCopol ? "Copol" : "Crosspol";
            this.HandleError(this, new AnalyzerEventArgs(beaconName + " beacon frequency not found at " + beacon.Frequency + "Hz."));
        }
    }
}