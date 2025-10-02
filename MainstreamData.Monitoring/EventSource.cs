// <copyright file="EventSource.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;

    /// <summary>
    /// An id to indicate which log file the data came from.
    /// See tEventSource table in the MediasRemoteMon database for more info.
    /// </summary>
    public enum EventSourceId
    {
        /// <summary>
        /// Provide a zero value id.
        /// </summary>
        None,

        /// <summary>
        /// Id for Medias server Input Controller log.
        /// </summary>
        MediasServerInputControllerLog,

        /// <summary>
        /// Id for Medias server FTP Manager log.
        /// </summary>
        MediasServerFtpManagerLog,

        /// <summary>
        /// Id for DVB+ web page stats.
        /// </summary>
        DvbPlusWebPages,

        /// <summary>
        /// Id for hardware utilization stats.
        /// </summary>
        HardwareUtilization,

        /// <summary>
        /// Id for list of running processes.
        /// </summary>
        RunningProcesses,

        /// <summary>
        /// All errors encountered by the remote monitor point.
        /// </summary>
        RemoteMonitorPointErrors
    }
    
    /// <summary>
    /// Provides functions for handling EventSourceId
    /// </summary>
    public static class EventSource
    {
        /// <summary>
        /// Checks to see if an event source id is contained in a bitmask.
        /// </summary>
        /// <param name="bitmask">An integer holding a bitmask of EventSourceIds.</param>
        /// <param name="eventSourceId">The EventSourceId to check for.</param>
        /// <returns>True if the EventSourceID is contained in the bitmask.</returns>
        public static bool BitmaskContainsEventSource(long bitmask, EventSourceId eventSourceId)
        {
            long eventSourceMask = (long)Math.Pow(2, (int)eventSourceId - 1);
            return (bitmask & eventSourceMask) != 0;
        }

        /// <summary>
        /// Calculates bitmask from array of EventSourceIds.
        /// </summary>
        /// <param name="eventSourceIds">Array of EventSourceIds.</param>
        /// <returns>Bitmask based on provided EventSourceIds.</returns>
        public static long CalculateBitmask(EventSourceId[] eventSourceIds)
        {
            long bitmask = 0;
            foreach (EventSourceId eventSourceId in eventSourceIds)
            {
                bitmask += (long)Math.Pow(2, (int)eventSourceId - 1);
            }

            return bitmask;
        }
    }
}