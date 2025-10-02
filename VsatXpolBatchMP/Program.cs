// <copyright file="Program.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolBatchMP
{
    using System;

    /// <summary>
    /// Class to provide main entry point for the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.  Add STAThread to properly support application mode (in addition to service mode).
        /// </summary>
        [STAThread]
        public static void Main()
        {
            MonitorApplication.Start(new VsatXpolBatchMP());
        }
    }
}