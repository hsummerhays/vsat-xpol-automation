// <copyright file="SpecAnalyzer.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using MainstreamData.Logging;
    using MainstreamData.Utility;
    using NationalInstruments.NI4882;

    /// <summary>
    /// Provides an interface to a spectrum analyzer connected to a GPIB interface.
    /// </summary>
    public class SpecAnalyzer : IDisposable
    {
        /// <summary>
        /// Number of samples retrieved from anaylzer.
        /// </summary>
        public const short SampleCount = 401;

        /// <summary>
        /// Used to lock changes to <see cref="SignalSweepList"/> while sweeping.
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// Provides access to the GPIB interface.
        /// </summary>
        private Device device;

        /// <summary>
        /// GPIB address of analyzer.
        /// </summary>
        private short deviceAddress = 0;

        /// <summary>
        /// List of signals to sweep and update.
        /// </summary>
        private List<Signal> signalSweepList = new List<Signal>();

        /// <summary>
        /// Indicates whether or not signalSweepList includes a beacon signal.
        /// </summary>
        private bool hasBeacon;

        /// <summary>
        /// Index of current signal in <see cref="signalSweepList"/>.
        /// </summary>
        private int signalIndex = 0;

        /// <summary>
        /// Temporary local store for analyzer data.
        /// </summary>
        private List<float> localData = new List<float>();

        /// <summary>
        /// Flag to tell whether or not analyzer is sweeping.
        /// </summary>
        private bool isSweeping = false;

        /// <summary>
        /// Flag to tell whether or not we are doing a single sweep.
        /// </summary>
        private bool isSingleSweep = false;

        /// <summary>
        /// The last command that was sent to the analyzer.
        /// </summary>
        private string lastCommand = string.Empty;

        /// <summary>
        /// Used by the Dispose method to tell the <see cref="SweepHandler"/> thread to stop.
        /// </summary>
        private bool stopThread = false;

        /// <summary>
        /// Used to allow <see cref="SweepHandler"/> thread to complete before disposing.
        /// </summary>
        private ManualResetEvent waitHandle = new ManualResetEvent(true);

        /// <summary>
        /// Used to prevent sweeping until successfully communicated with analyzer.
        /// </summary>
        private bool setupComplete = false;

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecAnalyzer"/> class.
        /// </summary>
        public SpecAnalyzer()
        {
            this.SignalSettings = new SignalSettings();
            this.SignalSettingsPresets = new Dictionary<string, SignalSettings>();

            // Set presets - for Beacon-Widesearch, a 2 MHz span is choosen to account for a large amount of LNB drift.
            this.UpdateOnePreset(SignalType.Beacon, SignalState.WideSearch, new SignalSettings((int)500E3, 0, 0, -35, 0));
            this.UpdateOnePreset(SignalType.Beacon, SignalState.NarrowSearch, new SignalSettings((int)50E3, 0, 0, -35, 0));
            this.UpdateOnePreset(SignalType.CopolClearWave, SignalState.WideSearch, new SignalSettings((int)45E3, 1000, 1000, -35, 1000));
            this.UpdateOnePreset(SignalType.CopolClearWave, SignalState.NarrowSearch, new SignalSettings((int)45E3, 1000, 1000, -35, 1000));
            this.UpdateOnePreset(SignalType.CopolClearWave, SignalState.Found, new SignalSettings((int)10E3, 100, 30, -55, 1000));
            this.UpdateOnePreset(SignalType.XpolClearWave, SignalState.None, new SignalSettings((int)10E3, 100, 30, -55, 1000));

            // Set defaults.
            this.Name = "Unknown";
            Signal signal = new Signal(1377950000, SignalType.CopolClearWave);    // Just an arbitrary default value.
            this.ChangeSettings(signal);
            signal.SignalType = SignalType.Signal;    // Change to ordinary signal now that ChangeSettings has been called.
            this.signalSweepList.Add(signal);

            /* Setup separate thread to handle communications with Spec A.
               Requires a ThreadStart delegate pointing to the method to run. */
            (new Thread(new ThreadStart(this.SweepHandler))).Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SpecAnalyzer"/> class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~SpecAnalyzer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Fires when an error occurs interfacing the analyzer.
        /// This is used instead of standard exception handling since the Sweep method runs in its own thread.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> Error;

        /// <summary>
        /// Fires when fresh data is avaiable from the analyzer.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> FreshData;

        /// <summary>
        /// Gets or sets a name to display in debug messages.
        /// </summary>
        public string Name { get; set; }

        // TODO: Throw ArgumentOutOfRangeException in set methods as appropriate.

        /// <summary>
        /// Gets or sets the board number of the GPIB interface. Must be set before setting address. Default is zero.
        /// </summary>
        public short GpibBoardNumber { get; set; }

        /// <summary>
        /// Gets or sets the GPIB address of the analyzer. Must set after setting GpibBoardNumber. Default is zero, but must call ResetAnalyzer if using default.
        /// </summary>
        public short DeviceAddress
        {
            get
            {
                return this.deviceAddress;
            }

            set
            {
                // TODO: Release previous handle if there is one.
                if (this.deviceAddress != value)
                {
                    this.deviceAddress = value;
                    this.ResetAnalyzer();
                }
            }
        }

        /// <summary>
        /// Gets the identification of the device once the address is specified.
        /// </summary>
        public string Identification { get; private set; }

        /// <summary>
        /// Gets or sets the center frequency of the analyzer in Hz. Does so by setting the frequency of signalSweepList[0].Frequency.
        /// </summary>
        public int Frequency
        {
            get
            {
                return this.signalSweepList[0].Frequency;
            }

            set
            {
                this.signalSweepList[0].Frequency = value;
            }
        }

        /// <summary>
        /// Gets the signal settings (i.e. Span, VBW, RBW, ref level, and sweep time) for the spec analyzer.
        /// </summary>
        public SignalSettings SignalSettings { get; private set; }

        /// <summary>
        /// Gets a list of signal settings that are used for beacon and clear wave searches, etc.
        /// </summary>
        public Dictionary<string, SignalSettings> SignalSettingsPresets { get; private set; }

        /// <summary>
        /// Gets list of signals to sweep and update.
        /// If you include a beacon, the spec A must first find the beacon before it will sweep other signals.
        /// </summary>
        /// <returns></returns>
        public IList<Signal> SignalSweepList
        {
            // TODO: Change this to IEnumerable or ReadOnlyCollection, or use Collection and watch for changes. See also AddSignalSweepList.
            get
            {
                return this.signalSweepList;
            }
        }

        /// <summary>
        /// Converts a string frequency in MHz to an int in Hz.
        /// </summary>
        /// <param name="frequency">The frequency in MHz to convert.</param>
        /// <returns>The original frequency in Hz.</returns>
        public static int ConvertToHertz(string frequency)
        {
            // TODO: See if there is a better place to put this method.
            return (int)(double.Parse(frequency, CultureInfo.InvariantCulture) * 1E6);
        }

        /// <summary>
        /// Replaces the current SweepList with a new one.
        /// </summary>
        /// <param name="signalSweepList">The new list to use.</param>
        public void AddNewSignalSweepList(IList<Signal> signalSweepList)
        {
            // TODO: Allow add/remove/clear instead of exposing the SignalSweepList?
            this.StopSweep();
            lock (this.lockObject)
            {
                // See if a beacon was included.
                int listLength = signalSweepList.Count;
                if (listLength > 0)
                {
                    this.hasBeacon = signalSweepList[0].SignalType == SignalType.Beacon;

                    // Go through all items except the first one.
                    for (int i = 1; i < listLength; i++)
                    {
                        if (signalSweepList[i].SignalType == SignalType.Beacon)
                        {
                            throw new ArgumentException("Only one beacon signal is allowed.", "signalSweepList");
                        }
                    }
                }

                // Reference the new list.
                this.signalIndex = 0;
                this.signalSweepList.Clear();
                this.signalSweepList.AddRange(signalSweepList);
            }
        }

        /// <summary>
        /// Updates the signal settings presets for the spectrum analyzer.
        /// </summary>
        /// <param name="settings">The signal settings for the spectrum analyzer to use. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        public void UpdatePresets(Dictionary<string, SignalSettings> settings)
        {
            foreach (KeyValuePair<string, SignalSettings> item in settings)
            {
                string message = "Invalid key value for SignalSettings: " + item.Key;
                string[] typeStatePair = item.Key.Split('-');
                if (typeStatePair.Length != 2)
                {
                    this.HandleError(message);
                    return;
                }

                string typeString = typeStatePair[0];
                string stateString = typeStatePair[1];

                if (!Enum.IsDefined(typeof(SignalType), typeString) || !Enum.IsDefined(typeof(SignalState), stateString))
                {
                    this.HandleError(message);
                    return;
                }

                SignalType type = (SignalType)Enum.Parse(typeof(SignalType), typeString);
                SignalState state = (SignalState)Enum.Parse(typeof(SignalState), stateString);

                try
                {
                    this.UpdateOnePreset(type, state, item.Value);
                }
                catch (ArgumentException ex)
                {
                    this.HandleError(string.Empty, ex);
                    return;
                }
            }
        }

        /// <summary>
        /// Resets the anaylzer using current values.
        /// </summary>
        public void ResetAnalyzer()
        {
            if (this.device != null)
            {
                this.device.Dispose();
                this.lastCommand = string.Empty;
            }

            this.StopSweep();
            this.setupComplete = false;

            try
            {
                // Setup the device including a callback function for handling reads.
                this.device = new Device(this.GpibBoardNumber, (byte)this.deviceAddress);
                this.device.IOTimeout = TimeoutValue.T10s;
                ////this.device.Notify(GpibStatusFlags.DeviceServiceRequest, new NotifyCallback(this.ReadData_Notify), null);   // Not currently working - see *SRE below.

                /* 
                 * *RST = Reset
                 * *SRE = Enable service request notification when there is new data. Not currently working - further investigation required.
                 * *IDN? = Returns the make and model of the device.
                 * *WAI = Waits for all pending commands to finish before executing others - makes read behave synchronously.
                 */
                this.device.Write("*RST;*IDN?;*WAI\n");    // This will throw InvalidOperationException if no device at specified address.
                this.Identification = this.device.ReadString().Replace("\n", string.Empty);    // This will throw a GpibException if the device doesn't respond to the *IDN? command.
                this.setupComplete = true;
            }
            catch (InvalidOperationException ex)
            {
                this.HandleError("Spectrum Analyzer not found.", ex);
            }
            catch (GpibException ex)
            {
                this.HandleError("Spectrum Analyzer did not respond to the *IDN? GPIB command.", ex);
            }
        }

        /// <summary>
        /// Tells the sweep handler thread to do a single sweep.
        /// </summary>
        public void SingleSweep()
        {
            if (this.setupComplete)
            {
                this.isSingleSweep = true;
                this.isSweeping = true;
            }
            else
            {
                this.ThrowSetupError();
            }
        }

        /// <summary>
        /// Tells the sweep handler thread to start sweeping.
        /// </summary>
        public void StartSweep()
        {
            if (this.setupComplete)
            {
                this.isSweeping = true;
            }
            else
            {
                this.ThrowSetupError();
            }
        }

        /// <summary>
        /// Tells the sweep handler thread to stop sweeping.
        /// </summary>
        public void StopSweep()
        {
            this.isSweeping = false;
            lock (this.lockObject)
            {
                this.lastCommand = string.Empty;
                this.signalIndex = 0;
            }
        }

        /// <summary>
        /// Cleans up resources to avoid having to wait for garbage collection.
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
                        // Clean up managed resources.
                        this.stopThread = true;

                        // Wait up to 15 seconds for SweepHandler thread to stop since GPIB timeout is 10.
                        this.waitHandle.WaitOne(15 * 1000);
                        this.waitHandle.Close();
                        if (this.device != null)
                        {
                            try
                            {
                                this.device.IOTimeout = TimeoutValue.T300ms;
                                try
                                {
                                    this.device.Write("*RST\n");
                                }
                                catch (InvalidOperationException)
                                {
                                    // Swallowing this error since happens when device is unavaliable.
                                }
                                catch (GpibException)
                                {
                                    // Swallowing this error since happens when device is unavaliable.
                                }
                            }
                            finally
                            {
                                this.device.Dispose();
                            }
                        }
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
        /// Gets the <see cref="SignalSettingsPresets"/> key for a specific signal type and state.
        /// </summary>
        /// <param name="type">The signal type to change settings for.</param>
        /// <param name="state">The signal state to change settings for.</param>
        /// <returns>The <see cref="SignalSettingsPresets"/> key for a specific signal type and state.</returns>
        private static string GetSignalSettingsPresetsKey(SignalType type, SignalState state)
        {
            return (SignalType)type + "-" + (SignalState)state;
        }

        /// <summary>
        /// Checks to see if value is zero meaning "AUTO ON".
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>"AUTO ON" if value is zero, otherwise the original value is returned with a space in front of it.</returns>
        private static string CheckAuto(string value)
        {
            if (value == "0")
            {
                return ":AUTO ON";
            }
            else
            {
                return " " + value;
            }
        }

        /// <summary>
        /// Updates a preset identified by signal type and state.
        /// </summary>
        /// <param name="type">The signal type to change settings for.</param>
        /// <param name="state">The signal state to change settings for.</param>
        /// <param name="settings">The new values to change settings to.</param>
        private void UpdateOnePreset(SignalType type, SignalState state, SignalSettings settings)
        {
            // Check for valid presets.
            // Beacon-Found is not needed because no scan is done after it is found.
            // CopolClearWave-NarrowSearch is not needed either because the Signal class doesn't use it.
            if (type == SignalType.Signal || state == SignalState.Inactive ||
                (type == SignalType.XpolClearWave && state != SignalState.None) ||
                (type != SignalType.XpolClearWave && state == SignalState.None) ||
                (type == SignalType.Beacon && state == SignalState.Found) ||
                ////(type == SignalType.CopolClearWave && state == SignalState.NarrowSearch) ||
                settings == null)
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid value or combination of values of type ({0}), state ({1}), and/or settings{2} for presets.",
                    (SignalType)type,
                    (SignalState)state,
                    settings == null ? " (null)" : string.Empty));
            }

            string key = SpecAnalyzer.GetSignalSettingsPresetsKey(type, state);
            SignalSettings settingsPreset = null;
            if (this.SignalSettingsPresets.ContainsKey(key))
            {
                settingsPreset = this.SignalSettingsPresets[key];
            }
            else
            {
                settingsPreset = new SignalSettings();
                this.SignalSettingsPresets.Add(key, settingsPreset);
            }

            settingsPreset.Span = settings.Span;
            settingsPreset.Rbw = settings.Rbw;
            settingsPreset.Vbw = settings.Vbw;
            settingsPreset.RefLevel = settings.RefLevel;
            settingsPreset.SweepTime = settings.SweepTime;
        }

        /// <summary>
        /// Calls <see cref="HandleError(string, Exception)"/> with InvalidOperationException and a setup error message.
        /// </summary>
        private void ThrowSetupError()
        {
            const string Message = "Setup is not complete. Address must be changed or ResetAnalyzer must be called directly.";
            this.HandleError(string.Empty, new InvalidOperationException(Message));
        }

        /// <summary>
        /// Calls <see cref="Error"/> event if there is a delegate attached.
        /// </summary>
        /// <param name="message">Error message to send.</param>
        private void HandleError(string message)
        {
            this.HandleError(message, null);
        }

        /// <summary>
        /// Calls <see cref="Error"/> event if there is a delegate attached.
        /// </summary>
        /// <param name="message">Error message to send.</param>
        /// <param name="ex">The exception that occurred.</param>
        private void HandleError(string message, Exception ex)
        {
            this.HandleError(message, ex, 0);
        }

        /// <summary>
        /// Calls <see cref="Error"/> event if there is a delegate attached.
        /// </summary>
        /// <param name="message">Error message to send.</param>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="waitSeconds">The number of seconds to pause after raising the error event. Allows for backing off a bit when exceptions are encountered.</param>
        private void HandleError(string message, Exception ex, int waitSeconds)
        {
            if (this.Error != null)
            {
                if (ex != null && ex.Message.Length != 0)
                {
                    message += " " + ex.ToString();
                }

                string nameText = this.Name == "Unknown" ? string.Empty : " (" + this.Name + ")";
                message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Spectrum analyzer error on board {0} at address {1}{2}. {3}",
                    this.GpibBoardNumber,
                    this.deviceAddress,
                    nameText,
                    message);
                this.Error(this, new AnalyzerEventArgs(message));
            }

            if (waitSeconds > 0)
            {
                Thread.Sleep(waitSeconds * 1000);
            }
        }

        /// <summary>
        /// Changes span, rbw, vbw, reflevel, and sweep time all at once based on signal type and state.
        /// Only changes if signal type and state match one of the conditions.
        /// </summary>
        /// <param name="signal">The signal to check for type and state.</param>
        private void ChangeSettings(Signal signal)
        {
            string key = SpecAnalyzer.GetSignalSettingsPresetsKey(signal.SignalType, signal.State);
            if (this.SignalSettingsPresets.ContainsKey(key))
            {
                SignalSettings oldSettings = this.SignalSettings;
                SignalSettings newSettings = this.SignalSettingsPresets[key];
                oldSettings.Span = newSettings.Span;
                oldSettings.Rbw = newSettings.Rbw;
                oldSettings.Vbw = newSettings.Vbw;
                oldSettings.RefLevel = newSettings.RefLevel;
                oldSettings.SweepTime = newSettings.SweepTime;
            }
        }

        /// <summary>
        /// Increments the <see cref="signalIndex"/> value to look at next signal in <see cref="signalSweepList"/>. Won't move off beacon until found.
        /// </summary>
        private void IncrementSignalIndex()
        {
            this.WriteDebug("IncrementSignalIndex was called while signalIndex was " + this.signalIndex);
            Monitor.Enter(this.lockObject);

            if (++this.signalIndex >= this.SignalSweepList.Count)
            {
                this.signalIndex = 0;
            }

            try
            {
                while (true)
                {
                    bool isBeacon = this.signalIndex == 0;
                    if (isBeacon)
                    {
                        if (this.signalSweepList[0].State == SignalState.Found)
                        {
                            // Don't bother with beacon anymore now that it has been found.
                            this.signalIndex = 1;
                            if (this.stopThread || !this.isSweeping)
                            {
                                return;
                            }
                        }
                        else
                        {
                            // Continue trying to find beacon.
                            return;
                        }
                    }

                    // Create a flag to watch for all signals being inactive so can sleep before continuing.
                    // If this.hasBeacon is true at this point, then the beacon has been found and should be considered inactive.
                    bool allInactive = this.signalIndex == (this.hasBeacon ? 1 : 0);
                    int listLength = this.signalSweepList.Count;
                    for (int i = this.signalIndex; i < listLength; i++)
                    {
                        Signal signal = this.signalSweepList[i];
                        if (signal.State != SignalState.Inactive)
                        {
                            if (this.signalIndex != i)
                            {
                                this.signalIndex = i;
                                this.WriteDebug("signalIndex changed to " + i.ToString(CultureInfo.InvariantCulture));
                            }

                            return;
                        }
                        else if (this.stopThread || !this.isSweeping)
                        {
                            return;
                        }
                    }

                    if (this.isSingleSweep)
                    {
                        return;
                    }
                    else if (allInactive)
                    {
                        // Slow down a bit since now waiting for one or more signals to change state before doing more work.
                        // Also, unlock during this time so that the SignalSweepList can be changed.
                        this.WriteDebug("Waiting for one or more signals to become active.");
                        Monitor.Exit(this.lockObject);
                        Thread.Sleep(1000);
                        Monitor.Enter(this.lockObject);
                    }

                    // Since we made it this far, we need to start back at zero.
                    this.signalIndex = 0;
                }
            }
            finally
            {
                Monitor.Exit(this.lockObject);
            }
        }

        /// <summary>
        /// Is the method called by the sweep handler thread.
        /// </summary>
        private void SweepHandler()
        {
            this.waitHandle.Reset();
            while (!this.stopThread)
            {
                if (this.isSweeping)
                {
                    if (this.isSingleSweep)
                    {
                        this.isSweeping = false;
                        this.isSingleSweep = false;
                    }

                    lock (this.lockObject)
                    {
                        // Don't sweep any inactive signals.
                        Signal signal = this.signalSweepList[this.signalIndex];
                        this.ChangeSettings(signal);

                        // Use beacon offset if available.
                        // TODO: Use uint or long for frequency since int.MaxValue is 2,147,483,647 and some LNBs go up to 2,150,000,000. This may not be important since currently doing 950 to 1450 MHz.
                        int frequency = signal.Frequency;
                        int beaconOffset = 0;
                        bool isBeacon = this.signalIndex == 0;
                        if (this.hasBeacon && (!isBeacon || signal.State == SignalState.NarrowSearch))
                        {
                            beaconOffset = this.signalSweepList[0].Offset;
                            frequency += beaconOffset;
                            this.WriteDebug("Beacon offset " + beaconOffset.ToString(CultureInfo.InvariantCulture) + " in use.");
                        }

                        // Use copol offset if signal state is NarrowSearch or Found.
                        if (signal.SignalType == SignalType.CopolClearWave &&
                            signal.State.In(SignalState.NarrowSearch, SignalState.Found))
                        {
                            frequency += signal.Offset;
                            this.WriteDebug("Signal offset " + signal.Offset.ToString(CultureInfo.InvariantCulture) + " in use.");
                        }

                        // Use copol offset on xpol signal where applicable.
                        SignalPair parent = signal.ClearWaveSignalParent;
                        bool? copolSignalOffsetInUse = null;
                        if (parent != null && signal.SignalType == SignalType.XpolClearWave)
                        {
                            if (parent.CopolSignal.State == SignalState.Found)
                            {
                                int offset = parent.CopolSignal.Offset;
                                frequency += offset;
                                copolSignalOffsetInUse = true;
                                this.WriteDebug("Copol offset " + offset.ToString(CultureInfo.InvariantCulture) + " in use.");
                            }
                            else if (parent.SignalFound)
                            {
                                copolSignalOffsetInUse = false;
                            }
                        }

                        // TODO: Make sure values are valid before sweeping.

                        // GPIB Write **********************************************
                        // Build command string.
                        StringBuilder command = new StringBuilder();
                        string sweepTime = SpecAnalyzer.CheckAuto(this.SignalSettings.SweepTime.ToString(CultureInfo.InvariantCulture));
                        sweepTime += this.SignalSettings.SweepTime == 0 ? string.Empty : "MS";
                        command.Append("INIT:CONT OFF;");        // SNGLS; Stop continuous sweep. Is first command, so don't need colon in front.
                        command.Append(":FREQ:CENT " + frequency + ";");    // CF xxxxxxxHZ; Set center frequency to where beacon should be.
                        command.Append("SPAN " + this.SignalSettings.Span + ";");   // SP xxxxxxxHZ; Set span to acquisition size. Is under :FREQ, so don't need colon in front.
                        command.Append(":DISP:WIND:TRAC:Y:RLEV " + this.SignalSettings.RefLevel.ToString(CultureInfo.InvariantCulture) + ";");   // RL xxDM; Set reference level.
                        command.Append(":BAND" + SpecAnalyzer.CheckAuto(this.SignalSettings.Rbw.ToString(CultureInfo.InvariantCulture)) + ";");    // RB xxxHZ; Set resolution bandwidth.
                        command.Append(":BAND:VID" + SpecAnalyzer.CheckAuto(this.SignalSettings.Vbw.ToString(CultureInfo.InvariantCulture)) + ";");    // VB xxxHZ; Set video bandwidth.
                        command.Append(":SWE:TIME" + sweepTime + ";");  // ST xxxxMS; Set Sweep Time.
                        command.Append(":FORM ASC;:UNIT:POWER DBM;*WAI;\n");   // TDF P;AUNITS DBM; Real number format, dbm, wait until commands finish

                        // See if last command changed.
                        if (this.lastCommand == command.ToString())
                        {
                            // Command is same - clear it so only do sweep and get data.
                            command = new StringBuilder();
                        }
                        else
                        {
                            this.lastCommand = command.ToString();
                            this.WriteDebug("New analyzer command: " + this.lastCommand);
                        }

                        // Setup sweep command - must include *WAI after INIT:IMM.
                        try
                        {
                            // Send commands to the spectrum analyzer.
                            const string SweepCommand = "INIT:IMM;*WAI;:TRAC? TRACE1;*WAI\n";   // TS; Take sweep, TA; transfer trace A amplitude values. Is new command, so don't need colon in front.
                            this.device.Write(command + SweepCommand);

                            // TODO: Check for errors using SYST:ERR and SYST:ERR:COUN?. Note: *CLS clears errors.
                        }
                        catch (GpibException ex)
                        {
                            this.HandleError("GPIB error while writing data.", ex, 2);
                        }
                        catch (InvalidOperationException ex)
                        {
                            this.HandleError("GPIB error while writing data.", ex, 5);
                        }

                        if (!this.stopThread)
                        {
                            // TODO: See about using *OPC? or *OPC or *SRE to correctly check for good sweep.
                            this.ReadData(frequency, beaconOffset, copolSignalOffsetInUse);
                        }
                    }

                    // This call must be outside lock so that signalSweepList can be changed when all signals are inactive.
                    this.IncrementSignalIndex();
                }
                else
                {
                    // Wait to see if isSweeping becomes true.
                    Thread.Sleep(500);
                }
            }

            this.waitHandle.Set();
            this.WriteDebug("Exiting SweepHandler method.");
        }

        /*
        /// <summary>
        /// Not currently in use - need to investigate *SRE command some more to make it work.
        /// </summary>
        /// <param name="sender">The object that fired this event.</param>
        /// <param name="e">The event args.</param>
        private void ReadData_Notify(object sender, NotifyData e)
        {
            SerialPollFlags test = this.device.SerialPoll();    // Does this populate the NotifyData status with the SerialPollFlags?

            // TODO: Put status and count to better use.
            string status = e.Status.ToString();
            int count = e.Count;

            this.ReadData();
        }*/

        /// <summary>
        /// Reads data from the spectrum analyzer via GPIB.
        /// </summary>
        /// <param name="frequency">The current center frequency of the spectrum analyzer.</param>
        /// <param name="beaconOffset">The current beacon offset.</param>
        /// <param name="copolSignalOffsetInUse">Indicates that an xpol signal is using the copol offset.</param>
        private void ReadData(int frequency, int beaconOffset, bool? copolSignalOffsetInUse)
        {
            string rawData = null;
            const string DataErrorMessage = "Data error while reading data.";
            try
            {
                rawData = this.device.ReadString(401 * 20);     // 401 is a common Spec A sample count.
            }
            catch (GpibException ex)
            {
                this.HandleError("GPIB error while reading data.", ex, 2);
                return;
            }
            catch (FormatException ex)
            {
                this.HandleError(DataErrorMessage, ex, 5);
                return;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                this.HandleError(DataErrorMessage, ex, 5);
                return;
            }

            // Format data and make it available ************************
            if (!string.IsNullOrEmpty(rawData))
            {
                // Update data for the currentSignal.
                Signal signal = this.signalSweepList[this.signalIndex];
                this.WriteDebug("Data for slot " + this.signalIndex + " read at frequency: "
                    + signal.Frequency.ToString(CultureInfo.InvariantCulture) +
                    ", Center: " + frequency.ToString(CultureInfo.InvariantCulture));
                signal.UpdateData(rawData, frequency, beaconOffset, this.SignalSettings.Span, this.SignalSettings.Rbw, this.SignalSettings.Vbw);

                // Update SignalPair.SignalFound as needed.
                if (copolSignalOffsetInUse.HasValue)
                {
                    signal.ClearWaveSignalParent.SignalFound = (bool)copolSignalOffsetInUse;
                }

                // Fire the FreshData event.
                if (this.FreshData != null)
                {
                    this.FreshData(this, new AnalyzerEventArgs(signal));
                }
            }
        }

        /// <summary>
        /// Writes message to the configured trace output (if applicable).
        /// </summary>
        /// <param name="message">The message to output.</param>
        private void WriteDebug(string message)
        {
            // Strip line feeds from message
            message = message.Replace("\r\n", " ").Replace("\n", " ");

            string fullMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})\t{2}",
                "SpecAnalyzer",
                this.Name,
                message);
            ExtendedLogger.WriteDebug(fullMessage);
        }
    }
}