// <copyright file="MonitorService.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System.ServiceProcess;

    /// <summary>
    /// The class needed to run monitor as a Windows service.  Is used by MonitorApplication.
    /// </summary>
    internal partial class MonitorService : ServiceBase
    {
        /// <summary>
        /// The monitor point class to use with the service.
        /// </summary>
        private MonitorPoint monitorPoint;

        /// <summary>
        /// Initializes a new instance of the MonitorService class.
        /// </summary>
        /// <param name="monitorPoint">The monitor point to use.</param>
        public MonitorService(MonitorPoint monitorPoint)
        {
            this.monitorPoint = monitorPoint;
            this.InitializeComponent();
        }

        /// <summary>
        /// Method that gets called when the service starts up.
        /// </summary>
        /// <param name="args">Arguments passed from the command line.</param>
        protected override void OnStart(string[] args)
        {
            this.monitorPoint.Start();
        }

        /// <summary>
        /// Method that gets called when service is stopped.
        /// </summary>
        protected override void OnStop()
        {
            this.monitorPoint.Stop();
            this.monitorPoint.Dispose();
        }
    }
}