// <copyright file="IVsatXpolRmpHost.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolRmp
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using MainstreamData.Monitoring;

    /// <summary>
    /// Provides a WCF interface for accessing VSAT Xpol information to assist with satellite dish alignment.
    /// </summary>
    [ServiceContract(Namespace = "http://MainstreamData.Monitoring")]
    public interface IVsatXpolRmpHost
    {
        /// <summary>
        /// Gets a list of signals containing beacon signal stats (slot zero) and stats for all CWs slots. Data plot detail is excluded.
        /// </summary>
        IEnumerable<SignalPair> SignalPairList
        {
            [OperationContract]
            get;
        }

        /// <summary>
        /// Gets 100 most recent error messages encountered by the RMP.
        /// </summary>
        IEnumerable<string> RecentErrorMessageList
        {
            [OperationContract]
            get;
        }

        /// <summary>
        /// Gets the most recent error message. If the last message wasn't an error, the null is returned.
        /// </summary>
        string LastErrorMessage
        {
            [OperationContract]
            get;
        }

        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        DateTime UtcTimeNow
        {
            [OperationContract]
            get;
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        [OperationContract]
        void Initialize(IList<SignalPair> signalPairList);

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzers to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        [OperationContract(Name = "InitializeWithSettings")]
        void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings);

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzers to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        /// <param name="force">Forces initialization even if there are no changes in the signal frequencies.</param>
        [OperationContract(Name = "InitializeWithForceOption")]
        void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings, bool force);

        /// <summary>
        /// Gets the status of a specific signal.
        /// </summary>
        /// <param name="clearWaveSlot">The slot of the CW to get the status of. A value of zero will return beacon data.</param>
        /// <returns>Object containing detailed data for both the copol and xpol.</returns>
        [OperationContract]
        SignalPair GetSignalPair(int clearWaveSlot);
    }
}