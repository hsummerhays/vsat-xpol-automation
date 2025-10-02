// -----------------------------------------------------------------------
// <copyright file="SignalPair.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

namespace MainstreamData.Monitoring.VsatXpol
{
    using System.Runtime.Serialization;
    using MainstreamData.Utility;

    /// <summary>
    /// Stores information for calculating xpol isolation.
    /// </summary>
    [DataContract(IsReference = true)]
    public class SignalPair : IExtensibleDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalPair"/> class for a clear wave signal pair.
        /// </summary>
        /// <param name="frequency">L-band frequency of the signal in Hz.</param>
        public SignalPair(int frequency)
        {
            this.CopolSignal = new Signal(frequency, SignalType.CopolClearWave, this);
            this.XpolSignal = new Signal(frequency, SignalType.XpolClearWave, this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalPair"/> class for a beacon signal pair.
        /// </summary>
        /// <param name="copolBeaconFrequency">L-band frequency of the copol beacon in Hz.</param>
        /// <param name="xpolBeaconFrequency">L-band frequency of the xpol beacon in Hz.</param>
        public SignalPair(int copolBeaconFrequency, int xpolBeaconFrequency)
        {
            this.CopolSignal = new Signal(copolBeaconFrequency, SignalType.Beacon, this);
            this.XpolSignal = new Signal(xpolBeaconFrequency, SignalType.Beacon, this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalPair"/> class for any signal pair.
        /// </summary>
        /// <param name="copolSignal">Signal to store as the copol.</param>
        /// <param name="xpolSignal">Signal to store as the xpol.</param>
        public SignalPair(Signal copolSignal, Signal xpolSignal)
        {
            this.CopolSignal = copolSignal;
            this.XpolSignal = xpolSignal;
        }

        /// <summary>
        /// Gets or sets a value used to support serialization.
        /// </summary>
        public ExtensionDataObject ExtensionData { get; set; }

        /// <summary>
        /// Gets the copol <see cref="Signal"/> object for analyzing the clearwave signal.
        /// </summary>
        [DataMember]
        public Signal CopolSignal { get; private set; }

        /// <summary>
        /// Gets the xpol <see cref="Signal"/> object for analyzing the clearwave signal.
        /// </summary>
        [DataMember]
        public Signal XpolSignal { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="XpolSignal"/> is using the <see cref="CopolSignal"/> offset
        /// (which means the <see cref="CopolSignal"/> has been found and both signals have correct information about the clearwave signal).
        /// </summary>
        [DataMember]
        public bool SignalFound { get; set; }

        /// <summary>
        /// Gets the isolation value of the between the two signals. Not very useful for beacons.
        /// </summary>
        public float Isolation
        {
            get
            {
                // Return zero unless cw has been found and copol signal offset is in use.
                if (!this.SignalFound || this.CopolSignal == null || this.XpolSignal == null ||
                    this.CopolSignal.SignalType == SignalType.Beacon || this.CopolSignal.PeakLevel < this.XpolSignal.PeakLevel)
                {
                    return 0;
                }
                else
                {
                    return this.CopolSignal.PeakLevel - this.XpolSignal.PeakLevel;
                }
            }
        }

        /// <summary>
        /// Sets the mode of both signals at one time.
        /// </summary>
        /// <param name="mode">The mode to change to.</param>
        public void SetMode(SignalMode mode)
        {
            this.CopolSignal.Mode = mode;
            this.XpolSignal.Mode = mode;
        }

        /// <summary>
        /// Must call to prevent clear wave signal states from switching to inactive. See Signal.State property for more info.
        /// </summary>
        public void UpdateRetrievalTime()
        {
            this.CopolSignal.UpdateRetrievalTime();
            this.XpolSignal.UpdateRetrievalTime();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is a <see cref="SignalPair"/>, and whether its frequencies and <see cref="SignalType"/>s are equal to the current <see cref="SignalPair"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="SignalPair"/>.</param>
        /// <returns>True if they have the same frequencies and signal types.</returns>
        public override bool Equals(object obj)
        {
            // If parameter cannot be cast then return false.
            SignalPair pair = obj as SignalPair;
            if (pair == null)
            {
                return false;
            }

            return this.Equals(pair);
        }

        /// <summary>
        /// Determines whether the specified <see cref="SignalPair"/> frequencies and <see cref="SignalType"/>s are equal to the current <see cref="SignalPair"/>.
        /// </summary>
        /// <param name="pair">The <see cref="SignalPair"/> to compare with the current <see cref="SignalPair"/>.</param>
        /// <returns>True if they have the same frequencies and signal types.</returns>
        public bool Equals(SignalPair pair)
        {
            bool isEqual = this.CopolSignal.Frequency == pair.CopolSignal.Frequency;
            isEqual &= this.CopolSignal.SignalType == pair.CopolSignal.SignalType;
            isEqual &= this.XpolSignal.Frequency == pair.XpolSignal.Frequency;
            isEqual &= this.XpolSignal.SignalType == pair.XpolSignal.SignalType;
            return isEqual;
        }

        /// <summary>
        /// Serves as a hash function for <see cref="SignalPair"/>.
        /// </summary>
        /// <returns>The hash code value.</returns>
        public override int GetHashCode()
        {
            // Must override this when overriding equals, but simply returning the base hash code.
            return base.GetHashCode();
        }
    }
}