// <copyright file="VsatXpolRmpHost.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolRmp
{
    using System;
    using System.Collections.Generic;
    using MainstreamData.Logging;
    using MainstreamData.Monitoring.VsatXpol;

    /// <summary>
    /// Provides a WCF interface for accessing VSAT Xpol information to assist with satellite dish alignment. IMPORTANT: Must dispose the <see cref="IsolationAnalyzer"/> reference when closing app.
    /// </summary>
    public class VsatXpolRmpHost : IVsatXpolRmpHost
    {
        /// <summary>
        /// Stores the last signal settings that were passed in via the initialize method.
        /// </summary>
        private static Dictionary<string, SignalSettings> lastSignalSettings = new Dictionary<string, SignalSettings>();

        /// <summary>
        /// Gets a list of signals containing beacon signal stats (slot zero) and stats for all CWs slots. Data plot detail is excluded.
        /// </summary>
        public IEnumerable<SignalPair> SignalPairList
        {
            get
            {
                IsolationAnalyzer isoAnalyzer = VsatXpolRmp.IsolationAnalyzer;
                List<SignalPair> list = new List<SignalPair>();
                list.Add(new SignalPair(
                    isoAnalyzer.CopolBeacon.CloneWithoutData(),
                    isoAnalyzer.XpolBeacon.CloneWithoutData()));
                foreach (SignalPair pair in isoAnalyzer.ClearWaveList)
                {
                    list.Add(new SignalPair(
                        pair.CopolSignal.CloneWithoutData(),
                        pair.XpolSignal.CloneWithoutData()));
                }

                return list;
            }
        }

        /// <summary>
        /// Gets 100 most recent error messages encountered by the RMP.
        /// </summary>
        public IEnumerable<string> RecentErrorMessageList
        {
            get
            {
                return VsatXpolRmp.RecentErrorMessages;
            }
        }

        /// <summary>
        /// Gets the most recent error message. If the last message wasn't an error, the null is returned.
        /// </summary>
        public string LastErrorMessage
        {
            get
            {
                List<string> messages = VsatXpolRmp.RecentErrorMessages;
                int count = messages.Count;
                string errorMessage = null;
                if (count > 0)
                {
                    string message = messages[count - 1];
                    if (message.Substring(0, 4).ToUpperInvariant() == "HIGH")
                    {
                        errorMessage = message;
                    }
                }

                return errorMessage;
            }
        }

        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        public DateTime UtcTimeNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        public void Initialize(IList<SignalPair> signalPairList)
        {
            this.Initialize(signalPairList, new Dictionary<string, SignalSettings>(), false);
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzers to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        public void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings)
        {
            this.Initialize(signalPairList, settings, false);
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzers to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        /// <param name="force">Forces initialization even if there are no changes in the signal frequencies.</param>
        public void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings, bool force)
        {
            IsolationAnalyzer isoAnalyzer = VsatXpolRmp.IsolationAnalyzer;

            // See if there are any changes.
            // TODO: Consider forcing signalPairList[0] SignalTypes to be Beacon.
            bool isEqual = false;
            if (!force)
            {
                if (VsatXpolRmpHost.CompareSignalSettings(ref settings))
                {
                    SignalPair oldBeaconPair = new SignalPair(isoAnalyzer.CopolBeacon, isoAnalyzer.XpolBeacon);
                    isEqual = signalPairList[0].Equals(oldBeaconPair);
                    if (isEqual)
                    {
                        int count = signalPairList.Count;
                        isEqual &= count == isoAnalyzer.ClearWaveList.Count + 1;
                        for (int i = 1; isEqual && i < count; i++)
                        {
                            isEqual &= signalPairList[i].Equals(isoAnalyzer.ClearWaveList[i - 1]);
                        }
                    }
                }
            }

            // Initialize the isolation analyzer.
            if (force || !isEqual)
            {
                isoAnalyzer.Stop();
                isoAnalyzer.CopolBeacon = signalPairList[0].CopolSignal;
                isoAnalyzer.XpolBeacon = signalPairList[0].XpolSignal;
                IList<SignalPair> clearWaveList = new List<SignalPair>(signalPairList);
                clearWaveList.RemoveAt(0);
                isoAnalyzer.AddNewClearWaveList(clearWaveList);
                isoAnalyzer.UpdateSpecAnalyzerPresets(settings);
                VsatXpolRmp.InitializeIsolationAnalyzer();

                // Remember current settings for next time initialize is called.
                VsatXpolRmpHost.lastSignalSettings = settings;
            }
        }

        /// <summary>
        /// Gets the status of a specific signal.
        /// </summary>
        /// <param name="clearWaveSlot">The slot of the CW to get the status of. A value of zero will return beacon data.</param>
        /// <returns>Object containing detailed data for both the copol and xpol.</returns>
        public SignalPair GetSignalPair(int clearWaveSlot)
        {
            ExtendedLogger.WriteDebug("GetSignalPair call for clear wave slot " + clearWaveSlot);

            IsolationAnalyzer isoAnalyzer = VsatXpolRmp.IsolationAnalyzer;
            SignalPair pair;
            if (clearWaveSlot == 0)
            {
                pair = new SignalPair(isoAnalyzer.CopolBeacon, isoAnalyzer.XpolBeacon);
            }
            else
            {
                int index = clearWaveSlot - 1;
                IList<SignalPair> list = isoAnalyzer.ClearWaveList;
                if (list.Count > index)
                {
                    pair = isoAnalyzer.ClearWaveList[index];
                    pair.UpdateRetrievalTime();
                }
                else
                {
                    return null;
                }
            }

            return pair;
        }

        /// <summary>
        /// Compares signal settings to the <see cref="lastSignalSettings"/>.
        /// </summary>
        /// <param name="settings">The new settings to compare against.</param>
        /// <returns>True if settings match.</returns>
        private static bool CompareSignalSettings(ref Dictionary<string, SignalSettings> settings)
        {
            Dictionary<string, SignalSettings> oldSettings = VsatXpolRmpHost.lastSignalSettings;
            bool isEqual = true;
            if (settings == null)
            {
                settings = oldSettings;
            }
            else
            {
                isEqual = oldSettings.Count == settings.Count;
                if (isEqual)
                {
                    foreach (KeyValuePair<string, SignalSettings> item in settings)
                    {
                        if (oldSettings.ContainsKey(item.Key))
                        {
                            if (!oldSettings[item.Key].Equals(item.Value))
                            {
                                isEqual = false;
                                break;
                            }
                        }
                        else
                        {
                            isEqual = false;
                            break;
                        }
                    }

                    if (isEqual)
                    {
                        foreach (KeyValuePair<string, SignalSettings> item in oldSettings)
                        {
                            if (settings.ContainsKey(item.Key))
                            {
                                if (!settings[item.Key].Equals(item.Value))
                                {
                                    isEqual = false;
                                    break;
                                }
                            }
                            else
                            {
                                isEqual = false;
                                break;
                            }
                        }
                    }
                }
            }

            return isEqual;
        }
    }
}