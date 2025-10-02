// <copyright file="Program.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolRmp
{
    using System;
    using MainstreamData.Monitoring;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            MonitorApplication.Start(new VsatXpolRmp());
        }
    }
}
