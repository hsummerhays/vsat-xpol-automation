// -----------------------------------------------------------------------
// <copyright file="VsatXpolLmp.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

#define CODE_ANALYSIS
[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using System.Threading;
    using System.Web.UI;
    using MainstreamData.Utility;

    /// <summary>
    /// Encapsulates functionality needed for gathering data from Remote Monitor Nodes (RMN).
    /// </summary>
    public class VsatXpolLmp : IDisposable
    {
        // TODO: See if can handle exceptions more generically throughout this class.

        /// <summary>
        /// Client for making calls to Windows Service that controls the spectrum analyzers.
        /// </summary>
        private VsatXpolRmpClient.VsatXpolRmpHostClient rmpClient;

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsatXpolLmp"/> class.
        /// </summary>
        /// <param name="satelliteName">The name of the satellite.</param>
        /// <param name="networks">A list of the networks available on the satellite.</param>
        /// <param name="wcfAddress">The Windows Communication Foundation address for connecting to the VsatXpolRmp (e.g. http://vsat2rmn:8000/ServiceModel/vsatxpolrmp ).</param>
        /// <exception cref="InvalidOperationException">Is thrown if unable to open connection to RMP.</exception>
        public VsatXpolLmp(string satelliteName, string networks, string wcfAddress)
        {
            this.SatelliteName = satelliteName;
            this.Networks = networks;
            this.WcfAddress = wcfAddress;

            this.rmpClient = new VsatXpolRmpClient.VsatXpolRmpHostClient("WSHttpBinding_IVsatXpolRmpHost", wcfAddress);

            const string Action = "opening connection to";
            try
            {
                this.rmpClient.Open();

                // Since not using security, must call a method before actual connection is made.
                this.rmpClient.get_LastErrorMessage();
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
        }

        /// <summary>
        /// Gets the name of the satellite.
        /// </summary>
        public string SatelliteName { get; private set; }

        /// <summary>
        /// Gets a list of the networks available on the satellite.
        /// </summary>
        public string Networks { get; private set; }

        /// <summary>
        /// Gets the Windows Communication Foundation address for connecting to the VsatXpolRmp.
        /// </summary>
        public string WcfAddress { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the LMP is connected to the RMP or not.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                bool isConnected = false;
                if (this.rmpClient.State == CommunicationState.Opened)
                {
                    try
                    {
                        // Since not using security, must call a method to ensure that an actual connection is made.
                        this.rmpClient.get_UtcTimeNow();
                        isConnected = true;
                    }
                    catch (TimeoutException)
                    {
                        // Nothing to do - just want to return false.
                    }
                    catch (CommunicationObjectFaultedException)
                    {
                        // Nothing to do - just want to return false.
                    }
                    catch (EndpointNotFoundException)
                    {
                        // Nothing to do - just want to return false.
                    }
                    catch (SecurityNegotiationException)
                    {
                        // Nothing to do - just want to return false.
                    }
                    catch (CommunicationException)
                    {
                        // Nothing to do - just want to return false.
                    }
                }

                return isConnected;
            }
        }

        /// <summary>
        /// Sends signal settings (from tServerInfo, tSignalSlot, and tSignalSettings) to the RMPs for all rows in the table, then gets the signal data back and updates the table.
        /// </summary>
        /// <param name="table">A table containing the results of a selServerByStatus stored procedure call. Allows both HAL and VsatXpolBatchMP to utilize this method.</param>
        /// <param name="connection">Connection to VsatXpol DB. Is used for retrieving signal settings.</param>
        /// <returns>The error message for the first satellite (if any).</returns>
        public static string PopulateSignalData(DataTable table, SqlConnection connection)
        {
            // TODO: Integrate this into the class instead of having a static method.
            string lastSatelliteName = string.Empty;
            string lastErrorMessage = string.Empty;
            string firstErrorMessage = string.Empty;
            bool isFirstSatellite = true;
            List<SignalPair> signalPairList = new List<SignalPair>();
            VsatXpolLmp lmp = null;
            int copolBeaconFrequency = 0;
            int xpolBeaconFrequency = 0;
            bool paused = false;
            foreach (DataRow row in table.Rows)
            {
                // Get reference to LMP class for accessing RMP.
                string satelliteName = row["zServerName"].ToString();
                if (satelliteName != lastSatelliteName)
                {
                    VsatXpolLmp.InitializeLastSatellite(lastSatelliteName, lmp, signalPairList, copolBeaconFrequency, xpolBeaconFrequency, connection);

                    // Clear the way for a new satellite to be processed.
                    lastSatelliteName = satelliteName;
                    signalPairList.Clear();
                    signalPairList.Add(null);   // Add an empty slot for beacons.
                    copolBeaconFrequency = 0;
                    xpolBeaconFrequency = 0;
                    lastErrorMessage = string.Empty;
                    lmp = null;

                    // Handle properly if disabled.
                    if (row["bEnabled"].ToString().Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    {
                        paused = true;
                    }
                    else
                    {
                        paused = false;

                        // Get existing LMP reference or create a new one as needed.
                        try
                        {
                            if (VsatXpolLmpList.Contains(satelliteName))
                            {
                                lmp = VsatXpolLmpList.GetLmp(satelliteName);
                            }
                            else
                            {
                                lmp = VsatXpolLmpList.Add(satelliteName, row["zDisplayName"].ToString(), row["zWcfAddress"].ToString());
                            }

                            // See if RMP has any internal errors.
                            lastErrorMessage = lmp.GetLastErrorMessage();
                        }
                        catch (InvalidOperationException ex)
                        {
                            lastErrorMessage = ex.Message;
                        }
                    }
                }

                // Gather signal information from DB table as needed to initialize RMP.
                string slotName = row["zSlotName"].ToString();
                int frequency = SpecAnalyzer.ConvertToHertz(row["fFrequencyMhz"].ToString());
                if (slotName.Equals("CopolBeacon", StringComparison.OrdinalIgnoreCase))
                {
                    copolBeaconFrequency = frequency;
                }
                else if (slotName.Equals("XpolBeacon", StringComparison.OrdinalIgnoreCase))
                {
                    xpolBeaconFrequency = frequency;
                }
                else
                {
                    signalPairList.Add(new SignalPair(frequency));  // Assumes slots are in order in the DB and none are missing.
                }

                // Update status in DB table as needed.
                if (paused)
                {
                    row["zServerStatus"] = "Paused";
                    row["zServerStatusMessage"] = "Server Paused (bEnable is False in DB)";
                }
                else if (!string.IsNullOrEmpty(lastErrorMessage))
                {
                    row["zServerStatus"] = "Down";
                    row["zServerStatusMessage"] = lastErrorMessage;
                }
                else if (!row["zServerStatus"].ToString().Equals("UP", StringComparison.OrdinalIgnoreCase))
                {
                    // No errors now, so change back to "Up".
                    row["zServerStatus"] = "Up";
                    row["zServerStatusMessage"] = "Up";
                }

                if (isFirstSatellite && !row["zServerStatus"].ToString().Equals("UP", StringComparison.OrdinalIgnoreCase))
                {
                    firstErrorMessage = row["zServerStatusMessage"].ToString();
                }

                isFirstSatellite = false;
            }

            VsatXpolLmp.InitializeLastSatellite(lastSatelliteName, lmp, signalPairList, copolBeaconFrequency, xpolBeaconFrequency, connection);

            // Update "up" rows with data from RMP.
            lastSatelliteName = string.Empty;
            lastErrorMessage = string.Empty;
            isFirstSatellite = string.IsNullOrEmpty(firstErrorMessage);
            foreach (DataRow row in table.Rows)
            {
                if (row["zServerStatus"].ToString().Equals("UP", StringComparison.OrdinalIgnoreCase))
                {
                    // Get reference to LMP class for accessing RMP.
                    string satelliteName = row["zServerName"].ToString();
                    if (satelliteName != lastSatelliteName)
                    {
                        lastSatelliteName = satelliteName;
                        lastErrorMessage = string.Empty;
                        signalPairList.Clear();
                        try
                        {
                            signalPairList.AddRange((List<SignalPair>)VsatXpolLmpList.GetLmp(satelliteName).GetSignalPairList());

                            if (signalPairList.Count > 0)
                            {
                                // TODO: Move this code earlier in this method and wait a few seconds to see if they get found.
                                SignalPair beacons = signalPairList[0];
                                if (beacons.CopolSignal.State != SignalState.Found || beacons.XpolSignal.State != SignalState.Found)
                                {
                                    lastErrorMessage = "Beacons not yet found.";
                                }
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            lastErrorMessage = ex.Message;
                        }
                    }

                    if (string.IsNullOrEmpty(lastErrorMessage))
                    {
                        // Gather signal information for updating status time.
                        string slotName = row["zSlotName"].ToString();
                        Signal signal = null;
                        try
                        {
                            if (slotName.Equals("CopolBeacon", StringComparison.OrdinalIgnoreCase))
                            {
                                signal = signalPairList[0].CopolSignal;
                            }
                            else if (slotName.Equals("XpolBeacon", StringComparison.OrdinalIgnoreCase))
                            {
                                signal = signalPairList[0].XpolSignal;
                            }
                            else
                            {
                                // Database check contraint guarantees that slotName will be numeric 1 through 9.
                                signal = signalPairList[int.Parse(slotName, CultureInfo.InvariantCulture)].CopolSignal;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            row["zServerStatus"] = "Down";
                            row["zServerStatusMessage"] = "SlotName " + slotName + " not currently found on RMP";
                        }

                        string frequencyOnRmp = signal.Frequency.ToString(CultureInfo.InvariantCulture);
                        string frequencyInDB = SpecAnalyzer.ConvertToHertz(row["fFrequencyMhz"].ToString()).ToString(CultureInfo.InvariantCulture);
                        if (frequencyOnRmp != frequencyInDB)
                        {
                            row["zServerStatus"] = "Down";
                            row["zServerStatusMessage"] = "Frequency on RMP (" + frequencyOnRmp + ") doesn't match frequency in DB (" + frequencyInDB + ")";
                        }
                        else
                        {
                            DateTime? statusTime = signal.LastUpdateTime == DateTime.MinValue ? (DateTime?)null : signal.LastUpdateTime;
                            if (statusTime == null)
                            {
                                row["oStatusTime"] = DBNull.Value;
                            }
                            else
                            {
                                row["oStatusTime"] = statusTime;
                                row["ElapsedTime"] = (DateTime.UtcNow - (DateTime)statusTime).TotalMinutes;
                            }
                        }
                    }
                    else
                    {
                        row["zServerStatus"] = "Down";
                        row["zServerStatusMessage"] = lastErrorMessage;
                    }

                    string storedError = row["zServerStatusMessage"].ToString();
                    if (isFirstSatellite && !storedError.Equals("UP", StringComparison.OrdinalIgnoreCase))
                    {
                        firstErrorMessage = row["zServerStatusMessage"].ToString();
                    }
                }

                isFirstSatellite = false;
            }

            return firstErrorMessage;
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        public void Initialize(IList<SignalPair> signalPairList)
        {
            this.Initialize(signalPairList, new Dictionary<string, SignalSettings>(), false);
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzer to use.</param>
        public void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings)
        {
            this.Initialize(signalPairList, settings, false);
        }

        /// <summary>
        /// Rescans the beacon frequencies and initializes and the entire list of CWs.
        /// </summary>
        /// <param name="signalPairList">The new list of beacons (slot zero) and CW slots.</param>
        /// <param name="settings">The signal settings for the spectrum analyzer to use as presets. The keys must be in the format "SignalType-SignalState" (e.g. Beacon-WideSearch).</param>
        /// <param name="force">Forces initialization even if there are no changes in the signal frequencies.</param>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        public void Initialize(IList<SignalPair> signalPairList, Dictionary<string, SignalSettings> settings, bool force)
        {
            const string Action = "getting status from";
            try
            {
                this.rmpClient.InitializeWithForceOption(signalPairList.ToArray(), settings, force);
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
        }

        /// <summary>
        /// Gets a list of signals without Data plot detail.
        /// </summary>
        /// <returns>An array containing beacon signal stats (slot zero) and stats for all CWs slots.</returns>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Properties should not throw exceptions in get methods.")]
        public IList<SignalPair> GetSignalPairList()
        {
            const string Action = "getting status from";
            try
            {
                return new List<SignalPair>(this.rmpClient.get_SignalPairList());
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }

            return null;
        }

        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        /// <returns>The current UTC time.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Properties should not throw exceptions in get methods.")]
        public DateTime GetUtcTimeNow()
        {
            const string Action = "getting time from";
            try
            {
                return this.rmpClient.get_UtcTimeNow();
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the status of a specific signal.
        /// </summary>
        /// <param name="clearWaveSlot">The slot of the CW to get the status of. A value of zero will return beacon data.</param>
        /// <returns>Object containing detailed data for both the copol and xpol.</returns>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        public SignalPair GetSignalPair(int clearWaveSlot)
        {
            const string Action = "getting status from";
            try
            {
                return this.rmpClient.GetSignalPair(clearWaveSlot);
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the indicated <see cref="SignalPair"/> and waits for a clear wave signal after a 2 second pause to allow CW time to settle.
        /// </summary>
        /// <param name="clearWaveSlot">The clear wave slot number to use.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait for the CW to appear or go away.</param>
        /// <returns>Data about the copol and xpol signals.</returns>
        public SignalPair CheckClearWave(int clearWaveSlot, int timeoutSeconds)
        {
            return this.CheckClearWave(clearWaveSlot, true, timeoutSeconds, 2);
        }

        /// <summary>
        /// Retrieves the indicated <see cref="SignalPair"/> and checks for a clear wave signal.
        /// </summary>
        /// <param name="clearWaveSlot">The clear wave slot number to use.</param>
        /// <param name="waitForFound">Waits for found if true. Waits for not found if false.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait for the CW to appear or go away.</param>
        /// <param name="settleSeconds">The number of seconds to wait for the CW to settle before taking final measurements.</param>
        /// <returns>Data about the copol and xpol signals.</returns>
        public SignalPair CheckClearWave(int clearWaveSlot, bool waitForFound, int timeoutSeconds, int settleSeconds)
        {
            // Get time of last signal updates
            SignalPair pair = this.GetSignalPair(clearWaveSlot);
            DateTime lastCopolUpdate = pair.CopolSignal.LastUpdateTime;
            DateTime lastXpolUpdate = pair.XpolSignal.LastUpdateTime;
            DateTime oldestSweep = lastCopolUpdate < lastXpolUpdate ? lastCopolUpdate : lastXpolUpdate;

            // Allow CW signal to settle out.
            Thread.Sleep(settleSeconds * 1000);
            pair = this.GetSignalPair(clearWaveSlot);

            // Take sweeps until found/not found, while respecting timeout.
            DateTime endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow <= endTime)
            {
                // Make sure we have fresh data.
                lastCopolUpdate = pair.CopolSignal.LastUpdateTime;
                lastXpolUpdate = pair.XpolSignal.LastUpdateTime;
                if (pair.CopolSignal.State != SignalState.Inactive && lastCopolUpdate >= oldestSweep && lastXpolUpdate >= oldestSweep)
                {
                    oldestSweep = lastCopolUpdate < lastXpolUpdate ? lastCopolUpdate : lastXpolUpdate;

                    // TODO: 4407 spec analyzer currently used on VSAT2 copol has lots of noise with RBW and VBW set to auto causing false "Found". Try some different RBW and VBW values to see if can be improved.
                    // TODO: Find out why was getting weird measurements with RBW set to 1KHz and VBW set to 100 Hz (e.g. -17.26 dB copol and 56.8 isolation). Could have simply been two CWs up at the same time.
                    // Break if the signal is in the requested state.
                    if (waitForFound)
                    {
                        if (pair.SignalFound)
                        {
                                break;
                        }
                    }
                    else if (pair.CopolSignal.State != SignalState.Found)
                    {
                        // We got a new sweep and no signal was there, so we can exit.
                        break;
                    }
                }

                // Give spectrum analyzer some time to sweep.
                System.Threading.Thread.Sleep(700);

                // Get new data.
                pair = this.GetSignalPair(clearWaveSlot);
            }

            return pair;
        }

        /// <summary>
        /// Gets 100 most recent error messages encountered by the RMP.
        /// </summary>
        /// <returns>List of error messages.</returns>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Properties should not throw exceptions in get methods.")]
        public IList<string> GetRecentErrorMessages()
        {
            const string Action = "getting errors from";
            try
            {
                return new List<string>(this.rmpClient.get_RecentErrorMessageList());
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }

            return null;
        }

        /// <summary>
        /// Gets the most recent error message.
        /// </summary>
        /// <returns>The most recent error message. If the last message wasn't an error, the null is returned.</returns>
        /// <exception cref="InvalidOperationException">Is thrown if unable to communicate with RMP.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Properties should not throw exceptions in get methods.")]
        public string GetLastErrorMessage()
        {
            const string Action = "getting last error from";
            try
            {
                return this.rmpClient.get_LastErrorMessage();
            }
            catch (TimeoutException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (EndpointNotFoundException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (SecurityNegotiationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }
            catch (CommunicationException ex)
            {
                VsatXpolLmp.HandleError(Action, ex);
            }

            return null;
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
                        // Clean up managed resources
                        if (!this.rmpClient.State.In(CommunicationState.Faulted, CommunicationState.Closed, CommunicationState.Closing))
                        {
                            this.rmpClient.Close();
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
        /// Provides a common way to pass error details back to the parent application without requiring the use of System.ServiceModel.
        /// </summary>
        /// <param name="action">The value to display in the following message: "ExceptionXyz while {action} RMP"</param>
        /// <param name="ex">The original exception that was thrown.</param>
        private static void HandleError(string action, Exception ex)
        {
            string message = string.Format(CultureInfo.InvariantCulture, "{0} while {1} RMP. {2}", ex.GetType().Name, action, ex.Message);
            throw new InvalidOperationException(message, ex);
        }

        /// <summary>
        /// Initializes the last satellite if all info has been gathered.
        /// </summary>
        /// <param name="satelliteName">The name of the last satellite processed.</param>
        /// <param name="lmp">The LMP reference for the last satellite.</param>
        /// <param name="signalPairList">The signal pair list for the last satellite.</param>
        /// <param name="copolBeaconFrequency">The copolBeaconFrequency in Hz.</param>
        /// <param name="xpolBeaconFrequency">The xpolBeaconFrequency in Hz.</param>
        /// <param name="connection">A connection to the VsatXpol DB to retrieve signal settings.</param>
        private static void InitializeLastSatellite(
            string satelliteName,
            VsatXpolLmp lmp,
            List<SignalPair> signalPairList,
            int copolBeaconFrequency,
            int xpolBeaconFrequency,
            SqlConnection connection)
        {
            // TODO: Integrate this into the class instead of having a static method.
            if (!string.IsNullOrEmpty(satelliteName) && lmp != null && lmp.IsConnected)
            {
                Dictionary<string, SignalSettings> settings = new Dictionary<string, SignalSettings>();
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                using (SqlCommand command = new SqlCommand("selSignalSettings " + satelliteName, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            settings.Add(
                                reader["zKey"].ToString(), 
                                new SignalSettings(
                                int.Parse(reader["iSpanHz"].ToString(), CultureInfo.InvariantCulture),
                                short.Parse(reader["iRbwHz"].ToString(), CultureInfo.InvariantCulture),
                                short.Parse(reader["iVbwHz"].ToString(), CultureInfo.InvariantCulture),
                                float.Parse(reader["fRefLevel"].ToString(), CultureInfo.InvariantCulture),
                                short.Parse(reader["iSweepTimeMS"].ToString(), CultureInfo.InvariantCulture)));
                        }
                    }
                }

                signalPairList[0] = new SignalPair(copolBeaconFrequency, xpolBeaconFrequency);
                lmp.Initialize(signalPairList, settings);
            }
        }
    }
}