// <copyright file="Signal.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text;
    using MainstreamData.Logging;
    using MainstreamData.Utility;

    /// <summary>
    /// Information about the state of a signal.
    /// </summary>
    public enum SignalState
    {
        /// <summary>
        /// <see cref="SignalType"/> "Signal" and "XpolClearWave" will be None when not inactive since they don't get "searched".
        /// </summary>
        None,

        /// <summary>
        /// Resets the signal to start searching.
        /// </summary>
        WideSearch,

        /// <summary>
        /// The signal is being sought.
        /// </summary>
        NarrowSearch,

        /// <summary>
        /// The signal has been found.
        /// </summary>
        Found,

        /// <summary>
        /// The signal is neither being sought nor has it been found.
        /// </summary>
        Inactive
    }

    /// <summary>
    /// Information about the type of signal.
    /// </summary>
    public enum SignalType
    {
        /// <summary>
        ///  Any signal the SpecAnalyzer can search for.
        /// </summary>
        Signal,

        /// <summary>
        /// A copol clear wave signal used to align VSAT dishes.
        /// </summary>
        CopolClearWave,

        /// <summary>
        /// An xpol clear wave signal used to align VSAT dishes - see SignalState.None
        /// </summary>
        XpolClearWave,

        /// <summary>
        /// A beacon signal used to determine signal drift.
        /// </summary>
        Beacon
    }

    /// <summary>
    /// Used to determine if state of a signal should automatically switch to inactive after a certain period of time.
    /// </summary>
    public enum SignalMode
    {
        /// <summary>
        /// Signal state will automatically change to inactive after a certain period of time.
        /// </summary>
        ActivelySweeping,

        /// <summary>
        /// Signal state will remain constant regardless of how long ago the signal data was updated.
        /// </summary>
        DataReview
    }

    /// <summary>
    /// Holds data about a given signal (e.g. clear wave, copol beacon, xpol beacon).
    /// </summary>
    [DataContract(IsReference = true)]
    public class Signal : IExtensibleDataObject
    {
        /// <summary>
        /// The minimum amplitude a signal can be, and still be considered found.
        /// </summary>
        private const int MinSignalAmplitude = 12;

        /// <summary>
        /// Used to get the next id available to assign to the next instance. Warning this could silently overflow if program runs for a long time.
        /// </summary>
        private static int nextUniqueId = 0;

        /// <summary>
        /// Used with nextUniqueId to make sure only one thread updates it at a time.
        /// </summary>
        private static object lockNextUniqueId = new object();

        /// <summary>
        /// Allows blocking certain actions (e.g. Clone) while the object is being updated. Must be a DataMember since is used once sent back to client.
        /// </summary>
        [DataMember]
        private readonly object lockObject = new object();

        /// <summary>
        /// Used to display a unique id for the current instance in trace data for debugging.
        /// </summary>
        [DataMember]
        private int uniqueId;

        /// <summary>
        /// Stores data from spectrum analyzer about the signal.
        /// </summary>
        [DataMember]
        private string rawData = string.Empty;

        /// <summary>
        /// L-band frequency of the signal in Hz.
        /// </summary>
        [DataMember]
        private int frequency;

        /// <summary>
        /// Holds the current state of the signal anaylzer.
        /// </summary>
        [DataMember]
        private SignalState state = SignalState.None;

        /// <summary>
        /// Stores last time UpdateRetrievalTime was called.
        /// </summary>
        [DataMember]
        private DateTime lastRetrievalTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the Signal class.
        /// </summary>
        public Signal()
            : this(0, SignalType.Signal, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Signal class.
        /// </summary>
        /// <param name="frequency">L-band frequency of the signal in Hz.</param>
        public Signal(int frequency)
            : this(frequency, SignalType.Signal, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Signal class.
        /// </summary>
        /// <param name="frequency">L-band frequency of the signal in Hz.</param>
        /// <param name="signalType">The type of signal to store.</param>
        public Signal(int frequency, SignalType signalType)
            : this(frequency, signalType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Signal class.
        /// </summary>
        /// <param name="signalType">The type of signal to store.</param>
        public Signal(SignalType signalType)
            : this(0, signalType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Signal class.
        /// </summary>
        /// <param name="frequency">L-band frequency of the signal in Hz.</param>
        /// <param name="signalType">The type of signal to store.</param>
        /// <param name="parent">A reference to the object that contains this instance.</param>
        public Signal(int frequency, SignalType signalType, SignalPair parent)
        {
            this.frequency = frequency;
            this.SignalType = signalType;
            this.ClearWaveSignalParent = parent;

            this.Mode = SignalMode.ActivelySweeping;

            lock (Signal.lockNextUniqueId)
            {
                this.uniqueId = Signal.nextUniqueId++;
                this.WriteDebug("UniqueId assigned: " + this.uniqueId.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets or sets a value used to support serialization.
        /// </summary>
        public ExtensionDataObject ExtensionData { get; set; }

        /// <summary>
        /// Gets or sets a comma separated representation of the data. Normally the UpdateData method is used to update this value.
        /// </summary>
        public string RawData
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.rawData;
                }
            }

            set
            {
                lock (this.lockObject)
                {
                    // See if FormatExcetion is thrown, then store the data.
                    this.CheckRawData(value);
                    this.rawData = value;
                }
            }
        }

        /// <summary>
        /// Gets the spectrum analyzer data about the signal from <see cref="RawData"/>.
        /// It is best to assign this to a local variable to prevent parsing the raw data over and over.
        /// </summary>
        public IList<float> Data
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.CheckRawData(this.rawData);
                }
            }
        }

        /// <summary>
        /// Gets a comma delimted list of values in a more compact format than <see cref="RawData"/>.
        /// </summary>
        public string DataString
        {
            get
            {
                // Lock happens when calling this.Data.
                return string.Join(",", Array.ConvertAll<float, string>(((List<float>)this.Data).ToArray(), Convert.ToString));
            }
        }

        /// <summary>
        /// Gets the span in Hz used when data was retrieve. Use the UpdateData method to update this property.
        /// </summary>
        [DataMember]
        public int DataSpan { get; private set; }

        /// <summary>
        /// Gets or sets L-band frequency of the signal in Hz.
        /// </summary>
        public int Frequency
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.frequency;
                }
            }

            set
            {
                lock (this.lockObject)
                {
                    this.frequency = value;
                    this.ActualFrequency = value;
                    this.ResetState();
                }
            }
        }

        /// <summary>
        /// Gets the center frequency in Hz used by the <see cref="SpecAnalyzer"/> when the signal was scanned.
        /// </summary>
        [DataMember]
        public int CenterFrequency { get; private set; }

        /// <summary>
        /// Gets the frequency in Hz of the peak signal (when first found) with the beacon offset removed.
        /// </summary>
        [DataMember]
        public int ActualFrequency { get; private set; }

        /// <summary>
        /// Gets the offset at the time the signal was found: ActualFrequency - Frequency.
        /// </summary>
        public int Offset
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.ActualFrequency - this.Frequency;
                }
            }
        }

        /// <summary>
        /// Gets amplitude of signal within Data in dBm.
        /// </summary>
        [DataMember]
        public float Amplitude { get; private set; }

        /// <summary>
        /// Gets peak level of signal within Data in dBm.
        /// </summary>
        [DataMember]
        public float PeakLevel { get; private set; }

        /// <summary>
        /// Gets the resolution bandwidth used by the <see cref="SpecAnalyzer"/> to sweep the signal.
        /// </summary>
        [DataMember]
        public int Rbw { get; private set; }

        /// <summary>
        /// Gets the video bandwidth used by the <see cref="SpecAnalyzer"/> to sweep the signal.
        /// </summary>
        [DataMember]
        public int Vbw { get; private set; }

        /// <summary>
        /// Gets or sets a value to determine if signal state should automatically switch to inactive after a certain period of time.
        /// </summary>
        [DataMember]
        public SignalMode Mode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the current state of the signal.
        /// Will automatically switch to Inactive if <see cref="UpdateRetrievalTime"/> isn't called at least every 20 seconds.
        /// Will automatically switch to Active if Inactive and <see cref="UpdateRetrievalTime"/> is called.
        /// For <see cref="SignalType"/> "XpolClearWave" and "Signal", only 'Inactive' and 'None' are allowed.
        /// For all other <see cref="SignalType"/>, only *Search and Inactive states are allowed to be set externally.
        /// </summary>
        public SignalState State
        {
            get
            {
                lock (this.lockObject)
                {
                    /*this.WriteDebug("Signal state read start - current state: " + ((SignalState)this.state));
                    this.WriteDebug(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}: Now ({1}) Retrieval ({2})",
                            "Signal state read times",
                            DateTime.UtcNow.ToString(),
                            this.lastRetrievalTime.ToString()));*/

                    // For clear wave signals, check last retrieval time and update state to reflect whether or not we are actively watching this signal.
                    if (this.Mode == SignalMode.ActivelySweeping &&
                        this.SignalType.In(SignalType.CopolClearWave, SignalType.XpolClearWave) &&
                        (DateTime.UtcNow - this.lastRetrievalTime).TotalSeconds > 20)
                    {
                        this.state = SignalState.Inactive;
                    }

                    ////this.WriteDebug("Signal state read end - current state: " + ((SignalState)this.state));

                    return this.state;
                }
            }

            set
            {
                lock (this.lockObject)
                {
                    if (this.SignalType.In(SignalType.Signal, SignalType.XpolClearWave) &&
                        !value.In(SignalState.Inactive, SignalState.None))
                    {
                        throw new ArgumentOutOfRangeException("value", "Type is Signal or XpolClearWave - only 'Inactive' and 'None' are allowed.");
                    }
                    else if (!value.In(SignalState.NarrowSearch, SignalState.WideSearch, SignalState.Inactive))
                    {
                        throw new ArgumentOutOfRangeException("value", "Only *Search and Inactive states are allowed to be set externally.");
                    }
                    else
                    {
                        this.state = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the type of signal.
        /// </summary>
        [DataMember]
        public SignalType SignalType { get; set; }

        /// <summary>
        /// Gets the last UTC time the Data was updated.
        /// </summary>
        [DataMember]
        public DateTime LastUpdateTime { get; private set; }

        /// <summary>
        /// Gets or sets a reference to the object that contains this instance.
        /// </summary>
        public SignalPair ClearWaveSignalParent { get; set; }

        /// <summary>
        /// Blocks other threads, then clones data to this.Data and updates DataSpan.
        /// </summary>
        /// <param name="rawData">The data to store.</param>
        /// <param name="centerFrequency">The center frequency of the spec a in Hz.</param>
        /// <param name="beaconOffset">The offset frequency of the beacon, if applicable (otherwise pass zero).</param>
        /// <param name="span">The span of frequencies scanned in Hz.</param>
        /// <param name="rbw">The resolution bandwidth used by the <see cref="SpecAnalyzer"/> to sweep the signal.</param>
        /// <param name="vbw">The video bandwidth used by the <see cref="SpecAnalyzer"/> to sweep the signal.</param>
        public void UpdateData(string rawData, int centerFrequency, int beaconOffset, int span, int rbw, int vbw)
        {
            int sampleCount = rawData.Split(',').GetUpperBound(0) + 1;
            this.WriteDebug(string.Format(
                CultureInfo.InvariantCulture,
                "UpdateData was called with data count ({0}), centerFrequency ({1}), beaconOffset ({2}), span ({3}), rbw ({4}), and vbw ({5})",
                sampleCount,
                centerFrequency,
                beaconOffset,
                span,
                rbw,
                vbw));
            lock (this.lockObject)
            {
                // Check the raw data by setting the RawData property and then get the modified count.
                this.RawData = rawData;
                IList<float> data = this.Data;
                sampleCount = data.Count;

                // Store other new values
                this.DataSpan = span;
                this.CenterFrequency = centerFrequency;
                this.Rbw = rbw;
                this.Vbw = vbw;

                /*****************
                 * Store Amplitude
                 *****************/
                const int Separation = 50;

                // Get peak position and value.
                int peakPos;
                float peakValue;
                if (this.SignalType == SignalType.Signal)
                {
                    peakPos = sampleCount / 2;
                    peakValue = data[peakPos];
                }
                else
                {
                    peakPos = this.GetPeakPos();
                    peakValue = data[peakPos];
                }

                // Get noise outside max.
                int noisePosLow = peakPos < Separation ? 0 : peakPos - Separation;
                int noisePosHigh = peakPos > sampleCount - Separation - 1 ? sampleCount : peakPos + Separation;
                float noiseAvg = this.GetAvgOutside(noisePosLow, noisePosHigh);

                // Store difference.
                this.Amplitude = peakValue - noiseAvg;
                this.PeakLevel = peakValue;

                /************************
                 * Store ActualFrequency - the frequency of the position within the data that has the highest amplitude (for beacon and copol CW only).
                 ************************/
                if (this.SignalType.In(SignalType.Beacon, SignalType.CopolClearWave))
                {
                    bool isBeacon = this.SignalType == SignalType.Beacon;
                    bool foundStrongSignal = this.Amplitude >= Signal.MinSignalAmplitude;
                    bool isFound = this.State == SignalState.Found;
                    if (foundStrongSignal && !isFound)
                    {
                        double spanPerSample = span / (double)sampleCount;
                        double peakSamplePos = this.GetPeakPos();
                        double centerSamplePos = sampleCount / 2.0;
                        double sampleDistFromCenter = peakSamplePos - centerSamplePos;
                        double freqDistFromCenter = sampleDistFromCenter * spanPerSample;

                        // Calc the frequency.
                        this.ActualFrequency = (int)(centerFrequency - beaconOffset + freqDistFromCenter);
                        this.WriteDebug("Peak found at " + this.ActualFrequency.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (!isFound)
                    {
                        // No strong signal and wasn't in SignalState.Found, so get rid of offset.
                        this.ActualFrequency = centerFrequency;
                    }
                    else
                    {
                        // Just leave the center where it was - don't follow a signal while in SignalState.Found.
                    }

                    /*************************
                    * Update state as needed for beacon and copol CW.
                    *************************/
                    switch (this.state)
                    {
                        case SignalState.WideSearch:
                            // Change state - SpecAnalyzer class will know to narrow span for beacons and copols.
                            if (foundStrongSignal)
                            {
                                // TODO: Determine if using NarrowSearch for copol provides better results for clear wave checking.
                                ////if (isBeacon)
                                {
                                    this.state = SignalState.NarrowSearch;
                                }

                                /*else
                                {
                                    this.state = SignalState.Found;
                                }*/
                            }
                            else if (isBeacon)
                            {
                                // Force beacon to inactive since search attempt failed.
                                this.state = SignalState.Inactive;
                            }

                            break;
                        case SignalState.NarrowSearch:
                            if (foundStrongSignal)
                            {
                                this.state = SignalState.Found;
                            }
                            else if (isBeacon)
                            {
                                // Force beacon to inactive since search attempt failed.
                                this.state = SignalState.Inactive;
                            }
                            else
                            {
                                // Force wide scan of signal since wasn't found on narrow search.
                                this.state = SignalState.WideSearch;
                            }

                            break;
                        case SignalState.Found:
                            // Beacon should never get here since SpecAnalyzer stops looking at it once it is found.
                            if (!foundStrongSignal)
                            {
                                // Not found anymore - go back to WideSearch.
                                this.state = SignalState.WideSearch;
                            }

                            break;
                    }
                }
                else
                {
                    // Never want an offset for xpol or SignalType.Signal.
                    this.ActualFrequency = this.Frequency;
                }

                this.LastUpdateTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Clears current sample data.
        /// </summary>
        public void ClearData()
        {
            lock (this.lockObject)
            {
                this.rawData = string.Empty;
            }
        }

        /// <summary>
        /// Clones the current instance, but sets Data to "new float[0]".
        /// </summary>
        /// <returns>A clone of the current instance with Data unpopulated</returns>
        public Signal CloneWithoutData()
        {
            lock (this.lockObject)
            {
                Signal clone = (Signal)this.MemberwiseClone();

                // Clone still has reference to original raw data - clear it.
                clone.rawData = string.Empty;

                this.WriteDebug("CloneWithoutData was called.");
                return clone;
            }
        }

        /// <summary>
        /// Clones the data held in the current instance.
        /// </summary>
        /// <returns>A clone of the current instance.</returns>
        public Signal Clone()
        {
            // Pause all other updates
            lock (this.lockObject)
            {
                // Since strings are immutable, don't need to make a deep copy of rawData - just do memberwiseclone.
                Signal clone = (Signal)this.MemberwiseClone();
                return clone;
            }
        }

        /// <summary>
        /// Must call to prevent clear wave signal states from switching to inactive. See State property for more info.
        /// </summary>
        public void UpdateRetrievalTime()
        {
            this.lastRetrievalTime = DateTime.UtcNow;
            if (this.SignalType == SignalType.CopolClearWave & this.State.In(SignalState.Inactive, SignalState.None))
            {
                this.state = SignalState.WideSearch;
            }
            else if (this.SignalType == SignalType.XpolClearWave && this.state == SignalState.Inactive)
            {
                this.state = SignalState.None;
            }

            this.WriteDebug("UpdateRetrievalTime called - retrieval time is now: " + this.lastRetrievalTime.ToString());
        }

        /// <summary>
        /// Checks the <see cref="SignalType"/> and resets <see cref="State"/> to the default starting point (e.g. WideSearch).
        /// </summary>
        private void ResetState()
        {
            if (this.SignalType.In(SignalType.Beacon, SignalType.CopolClearWave))
            {
                this.state = SignalState.WideSearch;
            }
            else
            {
                this.state = SignalState.None;
            }
        }

        /// <summary>
        /// Checks the raw data by throwing a format exception if it isn't correct, then returns the data as a List.
        /// </summary>
        /// <param name="rawData">The raw data to check and parse.</param>
        /// <returns>A list of values parsed from the raw data.</returns>
        /// <exception cref="FormatException">Thrown if float data isn't in a comma delimited format.</exception>
        private List<float> CheckRawData(string rawData)
        {
            List<float> dataList = new List<float>();
            if (!string.IsNullOrEmpty(rawData))
            {
                List<string> stringList = new List<string>(rawData.Split(",".ToCharArray()));
                foreach (string value in stringList)
                {
                    // TODO: Consider if should/could remove reference to SpecAnalyzer.SampleCount.
                    if (dataList.Count >= SpecAnalyzer.SampleCount)
                    {
                        // Get shorter value for raw data.
                        StringBuilder newRawData = new StringBuilder();
                        foreach (float item in dataList)
                        {
                            newRawData.Append(item.ToString("F", CultureInfo.InvariantCulture) + ",");
                        }

                        // Remove the trailing comma.
                        this.rawData = newRawData.ToString().Substring(0, newRawData.Length - 1);
                        break;
                    }
                    else
                    {
                        // This section of code may throw a FormatException.
                        float parsedValue = float.Parse(value, CultureInfo.InvariantCulture);
                        float dataValue = (float)Math.Round(parsedValue, 2);
                        dataList.Add(dataValue);
                    }
                }
            }

            return dataList;
        }

        /// <summary>
        /// Gets the position of the sample point with the highest amplitude.
        /// </summary>
        /// <returns>Position of point with highest amplitude.</returns>
        private int GetPeakPos()
        {
            IList<float> data = this.Data;
            float max = data[0]; // Get first sample.
            int maxPos = 0;

            // Look through entire range.
            int length = data.Count;
            float lastValue = max - 1;  // Make sure is less than max.
            int maxOffset = 0;
            for (int i = 0; i < length; i++)
            {
                // See if current is highest so far.
                if (max < data[i] || lastValue == max)
                {
                    // Calc center of max if applicable
                    if (lastValue == max)
                    {
                        maxOffset++;
                    }
                    else
                    {
                        max = data[i]; // Remember highest value.
                        maxOffset = 0;
                    }

                    maxPos = i + (maxOffset / 2);        // Remember center position of highest.
                }
            }

            return maxPos;
        }

        /// <summary>
        /// Calculates the average noise outside the excluded range.
        /// </summary>
        /// <param name="excludeStart">The position to start excluding.</param>
        /// <param name="excludeEnd">The position to stop excluding.</param>
        /// <returns>Average amplitude (in this case noise) outside of the specified range.</returns>
        private float GetAvgOutside(int excludeStart, int excludeEnd)
        {
            IList<float> data = this.Data;
            double total = 0;
            for (int i = 0; i < excludeStart; i++)
            {
                total += data[i];
            }

            int sampleCount = data.Count;
            for (int i = excludeEnd + 1; i < sampleCount; i++)
            {
                total += data[i];
            }

            // Note: there is always at least one data point even if the start and end are the same values.
            return (float)(total / (sampleCount - (excludeEnd - excludeStart + 1.0)));
        }

        /// <summary>
        /// Writes message to the configured trace output (if applicable).
        /// </summary>
        /// <param name="message">The message to output.</param>
        private void WriteDebug(string message)
        {
            // TODO: Decide if can add this back in - removed so HAL doesn't need logging config in web.config.

            // Strip line feeds from message
            /*message = message.Replace("\r\n", " ").Replace("\n", " ");

            string fullMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1}-{2})\t{3}\t{4}\t{5}",
                "Signal",
                (SignalType)this.SignalType,
                (SignalState)this.state,
                this.uniqueId,
                this.frequency,
                message);
            ExtendedLogger.WriteDebug(fullMessage);*/
        }
    }
}