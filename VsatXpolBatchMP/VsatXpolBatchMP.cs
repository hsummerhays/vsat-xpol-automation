// <copyright file="VsatXpolBatchMP.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolBatchMP
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using MainstreamData.Data;
    using MainstreamData.Logging;
    using MainstreamData.Monitoring.Linkstar;
    using MainstreamData.Monitoring.VsatXpol;
    using MainstreamData.Utility;

    /// <summary>
    /// List of statuses for batches.
    /// </summary>
    internal enum BatchStatus
    {
        /// <summary>
        /// Waiting for batch start time to pass.
        /// </summary>
        WaitingToStart,

        /// <summary>
        /// The batch is being processed.
        /// </summary>
        InProgress,

        /// <summary>
        /// The batch completed successfully.
        /// </summary>
        CompletedSuccessfully,

        /// <summary>
        /// The batch failed - see zStatusComments field for details.
        /// </summary>
        Failed,

        /// <summary>
        /// The batch was requested to be cancelled.
        /// </summary>
        Cancelling,

        /// <summary>
        /// The batch was cancelled by a user.
        /// </summary>
        Cancelled,
    }

    /// <summary>
    /// List of statuses for batch detail records.
    /// </summary>
    internal enum BatchDetailStatus
    {
        /// <summary>
        /// The measurement or reboot hasn't been performed yet.
        /// </summary>
        Waiting,

        /// <summary>
        /// The measurement or reboot completed successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// The measurement or reboot failed.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Main class to handle monitoring.
    /// </summary>
    public class VsatXpolBatchMP : MonitorPoint
    {
        /// <summary>
        /// Used for thread synchronization.
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// Used to keep all batch processing synchronous for now.
        /// </summary>
        private bool processing = false;

        /// <summary>
        /// Friendly and powerful interface into the database.
        /// </summary>
        private SqlDatabaseExtended vsatXpolDB;

        /// <summary>
        /// Holds connection information for the database.
        /// </summary>
        private SqlConnectionStringBuilder sqlConnBuilder = new SqlConnectionStringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="VsatXpolBatchMP"/> class.
        /// </summary>
        public VsatXpolBatchMP()
            : base()
        {
            Properties.Settings settings = Properties.Settings.Default;
            this.sqlConnBuilder.DataSource = settings.DatabaseServer;
            this.sqlConnBuilder.InitialCatalog = settings.DatabaseName;
            this.sqlConnBuilder.UserID = settings.DatabaseUser;
            this.sqlConnBuilder.Password = settings.DatabasePassword;

            try
            {
                this.vsatXpolDB = new SqlDatabaseExtended(this.sqlConnBuilder);
                this.vsatXpolDB.CreateConnection();
            }
            catch (SqlException ex)
            {
                ExtendedLogger.WriteException("Error while creating connection to database.", Category.General, Priority.High, ex);
            }
        }

        /// <summary>
        /// Sets up the monitor point. Is called by base class.
        /// </summary>
        /// <returns>True if setup completed successfully.</returns>
        protected override bool Setup()
        {
            // Not going to connect directly to HAL, so should not call base.Setup()
            ////return base.Setup();

            this.CommandTimerIntervalSeconds = 60;
            this.SetupComplete = true;
            return true;
        }

        /// <summary>
        /// Performs the main monitoring function.
        /// </summary>
        protected override void MonitorNow()
        {
            this.ProcessBatches();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        protected override void CheckForCommand()
        {
            // Do nothing - not going to implement commands for this MP.
        }

        /// <summary>
        /// Processes all batches that are ready to be processed.
        /// </summary>
        private void ProcessBatches()
        {
            lock (this.lockObject)
            {
                if (this.processing)
                {
                    return;
                }

                this.processing = true;
            }

            // TODO: See about moving exception handling partially into other methods.
            const string Message = "Unexpected error while processing batches.";
            try
            {
                // Check for new batches to process (selBatchesToProcess)
                using (IDataReader reader = this.vsatXpolDB.ExecuteReader(CommandType.StoredProcedure, "selBatchesToProcess"))
                {
                    DateTime? minStartTime = null;
                    int processedCount = 0;
                    while (reader.Read())
                    {
                        // Process batches as needed.
                        // TODO: Make so batches on separate networks can be processed at the same time.
                        int batchId = reader["iBatchId"].ToNullable<int>();
                        DateTime? startTime = reader["oStartTime"].ToNullable<DateTime?>();
                        if (startTime.HasValue)
                        {
                            if (startTime <= DateTime.UtcNow)
                            {
                                processedCount += 1;
                                this.StartBatch(batchId, reader);
                            }
                            else
                            {
                                if (!minStartTime.HasValue || minStartTime > startTime)
                                {
                                    minStartTime = startTime;
                                }
                            }
                        }

                        if (this.Stopped)
                        {
                            break;
                        }
                    }

                    if (minStartTime.HasValue)
                    {
                        ExtendedLogger.Write("Next batch will be processed on " + minStartTime.Value.ToString() + " UTC.", Category.General, Priority.Low);
                    }
                    else if (processedCount == 0)
                    {
                        ExtendedLogger.Write("No batches to process.", Category.General, Priority.Low);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                // Null DB value was used.
                ExtendedLogger.WriteException(Message, Category.General, Priority.High, ex);
            }
            catch (ArgumentNullException ex)
            {
                // Null DB value was used.
                ExtendedLogger.WriteException(Message, Category.General, Priority.High, ex);
            }
            catch (InvalidCastException ex)
            {
                ExtendedLogger.WriteException(Message, Category.General, Priority.High, ex);
            }
            catch (InvalidOperationException ex)
            {
                // Catches WebException from rncc, various exceptions from VsatXpolLmp, and some exceptions from database operations.
                ExtendedLogger.WriteException(Message, Category.General, Priority.High, ex);
            }
            catch (SqlException ex)
            {
                ExtendedLogger.WriteException(Message, Category.General, Priority.High, ex);
            }
            finally
            {
                this.processing = false;
            }
        }

        /// <summary>
        /// Checks a batch to see if it can be started.
        /// </summary>
        /// <param name="batchId">The batchId of the batch being started.</param>
        /// <param name="reader">The reader used to get data about the batch.</param>
        private void StartBatch(int batchId, IDataReader reader)
        {
            // NOTE: Exception handling currently done in the ProcessBatches method that calls this method.

            // Log the start of the batch.
            int retryCount = 0;
            bool batchAttemptFailed = true;
            while (batchAttemptFailed)
            {
                string batchType = reader["zBatchType"].ToNullable<string>();
                string errorMessage = "Unknown error on " + batchType + " batch id " + batchId;
                int serverId = reader["iServerId"].ToNullable<int>();
                BatchStatus currentStatus = (BatchStatus)Enum.Parse(typeof(BatchStatus), reader["zStatus"].ToNullable<string>());
                ExtendedLogger.Write("Starting to process batch id " + batchId, Category.General, Priority.Low);

                // Get CW slot frequency.
                //// TODO: Use enmum for batchType instead of string.
                if (batchType.Equals("xpol", StringComparison.OrdinalIgnoreCase))
                {
                    string frequency = string.Empty;
                    int? clearWaveSlot = null;
                    using (IDataReader slotReader = this.vsatXpolDB.ExecuteReader("selMaxSlot", reader["iNetworkId"].ToNullable<int>()))
                    {
                        while (slotReader.Read())
                        {
                            frequency = slotReader["fFrequencyMhz"].ToString();
                            clearWaveSlot = int.Parse(slotReader["zSlotName"].ToNullable<string>(), CultureInfo.InvariantCulture);
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(frequency))
                    {
                        errorMessage = "Unable to retrieve max CW slot for " + reader["zNetworkName"].ToNullable<string>();
                    }
                    else
                    {
                        // Prepare to use VsatXpolLmp class.
                        string serverName = reader["zServerName"].ToNullable<string>();
                        string lastError = string.Empty;
                        using (DataSet dataset = this.vsatXpolDB.ExecuteDataSet("selServerByStatus", -1, -1, null, null, "=" + serverId, null, null))
                        {
                            using (SqlConnection connection = new SqlConnection(this.vsatXpolDB.ConnectionString))
                            {
                                lastError = VsatXpolLmp.PopulateSignalData(dataset.Tables[0], connection);
                                if (!string.IsNullOrEmpty(lastError))
                                {
                                    // May be waiting for beacons to be found - try once more.
                                    Thread.Sleep(10 * 1000);
                                    lastError = VsatXpolLmp.PopulateSignalData(dataset.Tables[0], connection);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(lastError))
                        {
                            // There was a problem with the RMP or being able to communicate with it.
                            errorMessage = "Error while attempting to process " + batchType + " batch id " + batchId + ": " + lastError;
                        }
                        else
                        {
                            // Check for clear wave already active - wait if is.
                            // TODO: Consider only waiting a few minutes, then abort.
                            VsatXpolLmp lmp = VsatXpolLmpList.Dictionary[serverName];
                            SignalPair pair = lmp.CheckClearWave((int)clearWaveSlot, false, 5, 0);
                            if (pair.CopolSignal.State == SignalState.Found)
                            {
                                errorMessage = "An active clearwave was found on slot " + clearWaveSlot + " while attempting to process batch id " + batchId;
                            }
                            else
                            {
                                SignalPair beacons = lmp.GetSignalPair(0);
                                if (this.UpdateStatus(batchId, BatchStatus.InProgress, beacons.CopolSignal.Offset, beacons.XpolSignal.Offset, pair.CopolSignal.Frequency))
                                {
                                    try
                                    {
                                        this.ProcessBatch(batchId, frequency, (int)clearWaveSlot, reader, lmp);
                                        batchAttemptFailed = false;
                                    }
                                    catch (InvalidOperationException ex)
                                    {
                                        // Catches WebException from rncc, various exceptions from VsatXpolLmp, and some exceptions from database operations.
                                        errorMessage += " " + ex.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // batchType is Reboot.
                    if (this.UpdateStatus(batchId, BatchStatus.InProgress, null))
                    {
                        try
                        {
                            this.ProcessBatch(batchId, reader);
                            batchAttemptFailed = false;
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Catches WebException from rncc, various exceptions from VsatXpolLmp, and some exceptions from database operations.
                            errorMessage += " " + ex.ToString();
                        }
                    }
                }

                if (batchAttemptFailed)
                {
                    // Another failure on the current batch.
                    if (++retryCount >= 10)
                    {
                        // Final failure on the current batch - mark as failed.
                        currentStatus = BatchStatus.Failed;
                        errorMessage = batchType + " batch failed. " + errorMessage;

                        // Clear flag so loop will exit.
                        batchAttemptFailed = false;
                    }

                    // Since the attempt failed, log the status of the attempt.
                    this.UpdateStatus(batchId, currentStatus, errorMessage, Category.General, Priority.Medium);
                }

                if (this.Stopped)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Processes a reboot batch once it has been started - must call <see cref="StartBatch"/> first.
        /// </summary>
        /// <param name="batchId">The batchId of the batch being processed.</param>
        /// <param name="reader">The reader used to get data about the batch.</param>
        private void ProcessBatch(int batchId, IDataReader reader)
        {
            this.ProcessBatch(batchId, string.Empty, 0, reader, null);
        }

        /// <summary>
        /// Processes a batch once it has been started - must call <see cref="StartBatch"/> first.
        /// </summary>
        /// <param name="batchId">The batchId of the batch being processed.</param>
        /// <param name="frequency">The frequency of the clear wave slot being used.</param>
        /// <param name="clearWaveSlot">The clear wave slot being used.</param>
        /// <param name="reader">The reader used to get data about the batch.</param>
        /// <param name="lmp">The <see cref="VsatXpolLmp"/> interface to use for looking at the clear wave.</param>
        private void ProcessBatch(int batchId, string frequency, int clearWaveSlot, IDataReader reader, VsatXpolLmp lmp)
        {
            // NOTE: Exception handling currently done in the ProcessBatches method that indirectly calls this method.

            // Gather linkstar DB connection info.
            var builder = new SqlConnectionStringBuilder();
            builder = new SqlConnectionStringBuilder();
            builder.DataSource = reader["zLinkstarDBHost"].ToNullable<string>();
            builder.InitialCatalog = reader["zLinkstarDBName"].ToNullable<string>();
            builder.UserID = reader["zLinkstarDBUser"].ToNullable<string>();
            builder.Password = reader["zLinkstarDBPassword"].ToNullable<string>();

            // TODO: Get RNCC region from config file or database.
            TelnetRncc rncc = null;
            try
            {
                rncc = new TelnetRncc(builder, "1e");
                rncc.NetworkSupportsReceiveOnly = reader["bSupportsReceiveOnly"].ToNullable<bool>();

                // Retrieve list of RCSTs to process.
                bool cancelled = false;
                using (IDataReader detailReader = this.vsatXpolDB.ExecuteReader("selBatchDetail", batchId))
                {
                    while (detailReader.Read())
                    {
                        if (this.CheckForCancel(batchId, this.vsatXpolDB))
                        {
                            ExtendedLogger.Write("Batch " + batchId + " was cancelled by user", Category.General, Priority.Low);
                            cancelled = true;
                            break;
                        }

                        cancelled = this.ProcessRcst(batchId, frequency, clearWaveSlot, detailReader, lmp, rncc);

                        if (this.Stopped)
                        {
                            // Cancel the batch - must call update status twice since not allowed to go from inprogress to cancelled.
                            string message = "Batch " + batchId + " was cancelled by due to batch processor being shut down.";
                            this.UpdateStatus(batchId, BatchStatus.Cancelling, null, null, null);
                            this.UpdateStatus(batchId, BatchStatus.Cancelled, message);
                            ExtendedLogger.Write(message, Category.General, Priority.Low);
                            cancelled = true;
                        }

                        if (cancelled)
                        {
                            break;
                        }
                    }
                }

                if (!cancelled)
                {
                    if (!this.UpdateStatus(batchId, BatchStatus.CompletedSuccessfully, null))
                    {
                        this.UpdateStatus(
                            batchId,
                            BatchStatus.Failed,
                            "Unable to update status to 'CompletedSuccessfully'. See VsatXpolBatchMP log for details.",
                            Category.General,
                            Priority.High);
                    }

                    ExtendedLogger.Write("Batch id " + batchId + " was processed successfully.", Category.General, Priority.Low);
                }
            }
            finally
            {
                if (rncc != null)
                {
                    rncc.Dispose();
                }
            }
        }

        /// <summary>
        /// Processes a single RCST (or tBatchDetail record) by sending up a clear wave and reading its measurements.
        /// </summary>
        /// <param name="batchId">The batchId of the batch being processed.</param>
        /// <param name="frequency">The frequency of the clear wave slot being used.</param>
        /// <param name="clearWaveSlot">The clear wave slot being used.</param>
        /// <param name="detailReader">The reader used to get data about the RCST.</param>
        /// <param name="lmp">The <see cref="VsatXpolLmp"/> interface to use for looking at the clear wave.</param>
        /// <param name="rncc">The <see cref="TelnetRncc"/> interface to use for sending up the clear wave.</param>
        /// <returns>True if process was cancelled.</returns>
        private bool ProcessRcst(int batchId, string frequency, int clearWaveSlot, IDataReader detailReader, VsatXpolLmp lmp, TelnetRncc rncc)
        {
            //// NOTE: Exception handling currently done in the ProcessBatches method that indirectly calls this method.

            bool cancelled = false;

            // Send up clear wave.
            // TODO: Look at telnet responses to make sure commands are being executed properly. The telnet buffer is written to the DB regardless.
            rncc.ClearBuffer();
            string termId = detailReader["termId"].ToNullable<string>();
            bool batchTypeIsXpol = !string.IsNullOrEmpty(frequency);
            SignalPair pair;
            BatchDetailStatus status = BatchDetailStatus.Complete;
            if (batchTypeIsXpol)
            {
                string power = "-14";
                string seconds = "30";  // This is simply how long the CW will be commanded to be up. It will be taken back down immediately after it is found.
                rncc.ClearWave("icw", termId, power, frequency, seconds);

                // Get the isolation value.
                // TODO: Add code for when measurment fails (e.g. can't contact RMP)? May already be done via exception thrown by lmp and via try/catch of calling method, but better handling may still be desired.
                pair = lmp.CheckClearWave(clearWaveSlot, 10);
                if (!pair.SignalFound)
                {
                    // Try again.
                    rncc.ClearWave("icw", termId, power, frequency, seconds);
                    pair = lmp.CheckClearWave(clearWaveSlot, 10);
                }

                // Stop the clear wave.
                rncc.ClearWave("tcw", termId, power, frequency, string.Empty);
            }
            else
            {
                pair = new SignalPair(0, 0);
                if (rncc.Execute("reboot", termId, true) == 0)
                {
                    status = BatchDetailStatus.Failed;
                }
            }

            // Store the data.
            // TODO: Write something to status comments other than null;
            string statusComments = null;
            Signal copol = pair.CopolSignal;
            Signal xpol = pair.XpolSignal;
            this.vsatXpolDB.ExecuteNonQuery(
                "updBatchDetail",
                batchId,
                termId,
                DateTime.UtcNow,
                Math.Round(copol.PeakLevel, 2),
                Math.Round(xpol.PeakLevel, 2),
                Math.Round(pair.Isolation, 1),
                Math.Round(copol.Amplitude, 2),
                Math.Round(xpol.Amplitude, 2),
                (BatchDetailStatus)status,
                statusComments,
                rncc.Buffer,
                copol.DataString,
                copol.CenterFrequency,
                copol.DataSpan,
                copol.Rbw,
                copol.Vbw,
                xpol.DataString,
                xpol.CenterFrequency,
                xpol.DataSpan,
                xpol.Rbw,
                xpol.Vbw);

            // Make sure the clear wave goes away. Don't bother though if application is shutting down.
            // TODO: Add this back once ViaSat resolves problem with clear waves lingering after stopping them.
            /*
            if (!this.Stopped)
            {
                pair = VsatXpolBatchMP.CheckClearWave(lmp, (int)clearWaveSlot, false, 60);
                if (pair.CopolSignal.State == SignalState.Found)
                {
                    this.UpdateStatus(
                        batchId,
                        BatchStatus.Failed,
                        "Clear wave for RCST " + termId + " did not go away after 60 seconds. Marking batch as failed.",
                        Category.General,
                        Priority.High);
                    cancelled = true;
                }
            }*/

            return cancelled;
        }

        /// <summary>
        /// Attempts to update the specified batch with a new status. Uses sqlConnBuilder.UserID and null completedTime.
        /// </summary>
        /// <param name="batchId">The batch id to update.</param>
        /// <param name="status">The status to change to.</param>
        /// <param name="copolBeaconOffset">The copol beacon offset in Hz. No change if null.</param>
        /// <param name="xpolBeaconOffset">The xpol beacon offset in Hz. No change if null.</param>
        /// <param name="slotFrequency">The CW frequency being used for this batch. No change if null.</param>
        /// <returns>True if update was successful.</returns>
        private bool UpdateStatus(int batchId, BatchStatus status, int? copolBeaconOffset, int? xpolBeaconOffset, int? slotFrequency)
        {
            return this.UpdateStatus(batchId, status, null, this.sqlConnBuilder.UserID, copolBeaconOffset, xpolBeaconOffset, slotFrequency, null, null, null);
        }

        /// <summary>
        /// Attempts to update the specified batch with a new status. Uses sqlConnBuilder.UserID and current time for completedTime.
        /// </summary>
        /// <param name="batchId">The batch id to update.</param>
        /// <param name="status">The status to change to.</param>
        /// <param name="statusComments">Any comments on the status.</param>
        /// <returns>True if update was successful.</returns>
        private bool UpdateStatus(int batchId, BatchStatus status, string statusComments)
        {
            return this.UpdateStatus(batchId, status, statusComments, this.sqlConnBuilder.UserID, null, null, null, DateTime.UtcNow, null, null);
        }

        /// <summary>
        /// Attempts to update the specified batch with a new status and also logs the message. Uses sqlConnBuilder.UserID and current time for completedTime when necessary.
        /// </summary>
        /// <param name="batchId">The batch id to update.</param>
        /// <param name="status">The status to change to.</param>
        /// <param name="statusComments">Any comments on the status.</param>
        /// <param name="category">The category to log the message under.</param>
        /// <param name="priority">The priority to log the message under.</param>
        /// <returns>True if update was successful.</returns>
        private bool UpdateStatus(int batchId, BatchStatus status, string statusComments, Category category, Priority priority)
        {
            DateTime? time = null;
            if (status.In(BatchStatus.Cancelled, BatchStatus.Failed, BatchStatus.CompletedSuccessfully))
            {
                time = DateTime.UtcNow;
            }

            return this.UpdateStatus(batchId, status, statusComments, this.sqlConnBuilder.UserID, null, null, null, time, category, priority);
        }

        /// <summary>
        /// Attempts to update the specified batch with a new status and also logs the message if category and priority are not null.
        /// </summary>
        /// <param name="batchId">The batch id to update.</param>
        /// <param name="status">The status to change to.</param>
        /// <param name="statusComments">Any comments on the status.</param>
        /// <param name="updatedBy">The username to put into the zUpdatedBy field.</param>
        /// <param name="copolBeaconOffset">The copol beacon offset in Hz. No change if null.</param>
        /// <param name="xpolBeaconOffset">The xpol beacon offset in Hz. No change if null.</param>
        /// <param name="slotFrequency">The CW frequency being used for this batch. No change if null.</param>
        /// <param name="completedTime">The new completed time if a applicable. No change if null.</param>
        /// <param name="category">The category to log the message under.</param>
        /// <param name="priority">The priority to log the message under.</param>
        /// <returns>True if update was successful.</returns>
        private bool UpdateStatus(int batchId, BatchStatus status, string statusComments, string updatedBy, int? copolBeaconOffset, int? xpolBeaconOffset, int? slotFrequency, DateTime? completedTime, Category? category, Priority? priority)
        {
            bool isOkToContinue = true;
            try
            {
                double? frequencyMhz = slotFrequency / 1E6;
                this.vsatXpolDB.ExecuteNonQuery("updBatchStatus", batchId, (BatchStatus)status, statusComments, updatedBy, copolBeaconOffset, xpolBeaconOffset, frequencyMhz, completedTime);
                if (category != null && priority != null)
                {
                    ExtendedLogger.Write(statusComments, (Category)category, (Priority)priority);
                }
            }
            catch (SqlException ex)
            {
                string failedComments = string.IsNullOrEmpty(statusComments) ? string.Empty : ". Status Comments: " + statusComments;
                ExtendedLogger.WriteException(
                    "Error while attempting to change status to '" + (BatchStatus)status + "' for batch id " + batchId + failedComments,
                    Category.General,
                    Priority.High,
                    ex);
                isOkToContinue = false;
            }

            return isOkToContinue;
        }

        /// <summary>
        /// Checks to see if a batch has been cancelled.
        /// </summary>
        /// <param name="batchId">The batch id to check.</param>
        /// <param name="vsatXpolDB">The DB object to execute against.</param>
        /// <returns>True if update was cancelled.</returns>
        private bool CheckForCancel(int batchId, SqlDatabaseExtended vsatXpolDB)
        {
            bool isCancelled = false;
            using (DataSet set = this.vsatXpolDB.ExecuteDataSet("selServerInfoForBatch", batchId))
            {
                DataRow row = set.Tables[0].Rows[0];
                BatchStatus currentStatus = (BatchStatus)Enum.Parse(typeof(BatchStatus), row["zStatus"].ToString());
                if (currentStatus == BatchStatus.Cancelling)
                {
                    // Keep zUpdateUser the same so can see who cancelled the batch.
                    this.UpdateStatus(
                        batchId,
                        BatchStatus.Cancelled,
                        null,
                        row["zUpdatedBy"].ToNullable<string>(),
                        null,
                        null,
                        null,
                        DateTime.UtcNow,
                        null,
                        null);
                    isCancelled = true;
                }
                else if (currentStatus == BatchStatus.Cancelled)
                {
                    isCancelled = true;
                }
            }

            return isCancelled;
        }
    }
}
